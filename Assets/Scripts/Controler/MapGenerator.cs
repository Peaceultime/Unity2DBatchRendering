using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[System.Serializable]
public struct MapOptions
{
    [Range(1, 1024)]
    public int size;
    public Biome biome;
    public uint seed;
    [Range(0, 1024)]
    public int terraces;
    [Range(0, 1024)]
    public int dispersion;
    
    public int capacity => (size * (size + 1)) * 3 + 1;
};

public static class MapGenerator
{
    public const int GRANULARITY = 32;
    public const int CHUNK_MAX = 1024;

    public const float TERRACES_HEIGHT = 5f;

    public static NativeArray<Hex> hices;
    public static NativeArray<Sprite> sprites;
    public static NativeList<Village> villages;

    public static List<Spritesheet> spritesheets = new List<Spritesheet>();

    public static void GenerateMap(MapOptions options)
    {
        Random random = new Random(options.seed);

        HexJob hexJob = new HexJob
        {
            options = options,
            random = random,
            randomness = random.NextFloat2(1 << 20),
            hices = new NativeArray<Hex>(options.capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
        };
        JobHandle handle = hexJob.ScheduleByRef(options.capacity, GRANULARITY);

        StructureJob structureJob = new StructureJob
        {
            random = random,
            size = options.size,
            hices = hexJob.hices,
            capacity = options.capacity,
            villages = new NativeList<Village>(Allocator.Persistent),
            villageAmount = options.size / 100,
        };
        handle = structureJob.ScheduleByRef(handle);

        CheckReachJob reachJob = new CheckReachJob
        {
            size = options.size,
            hices = structureJob.hices,
        };

        reachJob.ScheduleByRef(handle).Complete();
        hices = reachJob.hices;
        villages = structureJob.villages;
    }
    public static void RegenerateMap(MapOptions options)
    {
        Random random = new Random(options.seed);

        UpdateHexJob hexJob = new UpdateHexJob
        {
            options = options,
            random = random,
            randomness = random.NextFloat2(1 << 20),
            hices = hices,
        };
        JobHandle handle = hexJob.ScheduleByRef(options.capacity, GRANULARITY);

        StructureJob structureJob = new StructureJob
        {
            random = random,
            size = options.size,
            hices = hexJob.hices,
            capacity = options.capacity,
            villages = villages,
            villageAmount = options.size / 64,
        };
        handle = structureJob.ScheduleByRef(handle);

        for (int i = 0; i < spritesheets.Count; i++)
        {
            SpriteJob spriteJob = new SpriteJob
            {
                hices = structureJob.hices,
                layer = (Layer)spritesheets[i].layer,
                sprites = sprites.Slice(i * options.capacity, options.capacity),
                resolution = spritesheets[i].resolution,
            };

            handle = spriteJob.ScheduleByRef(options.capacity, GRANULARITY, handle);
        }

        handle.Complete();

        hices = hexJob.hices;
        spritesheets[0].SetSprites(sprites.Slice(0, options.capacity));
        spritesheets[1].SetSprites(sprites.Slice(options.capacity, options.capacity));
    }
    public static void RenderMap(MapOptions options, Material material, Texture2DArray[] atlases)
    {
        sprites = new NativeArray<Sprite>(options.capacity * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        Spritesheet spritesheetBase = new Spritesheet(atlases[(int)Layer.BASE], Layer.BASE, new float2(0, 0), new float4(1));
        Spritesheet spritesheetDeco = new Spritesheet(atlases[(int)Layer.DECO], Layer.DECO, new float2(0, 0.5f), new float4(1));

        GenerateSpriteLayer(spritesheetBase, 0, options.capacity);
        GenerateSpriteLayer(spritesheetDeco, options.capacity, options.capacity);
    }

    private static void GenerateSpriteLayer(Spritesheet spritesheet, int start, int count)
    {
        SpriteJob spriteJob = new SpriteJob
        {
            hices = hices,
            layer = (Layer) spritesheet.layer,
            sprites = sprites.Slice(start, count),
            resolution = spritesheet.resolution,
        };

        JobHandle handle = spriteJob.ScheduleByRef(count, GRANULARITY);

        handle.Complete();
        spritesheet.SetSprites(spriteJob.sprites);
        Renderer.AddSpritesheet(spritesheet, count);

        spritesheets.Add(spritesheet);
    }
    public static void ClearMap()
    {
        hices.Dispose();
        sprites.Dispose();
        villages.Dispose();

        for (int i = 0; i < spritesheets.Count; i++)
            spritesheets[i].Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetPosFromCoord(int q, int r, float h, int resolution)
    {
        return new float2((q + (r / 2f)) * ((resolution -1f) / resolution), (r) * (1f / 2 * (3f / 4)) + (h * TERRACES_HEIGHT) - 1.5f) * ((resolution -1f) / resolution);
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct ChunkJob : IJobFor
    {
        [NativeDisableParallelForRestriction] [ReadOnly] public NativeSlice<Sprite> sprites;
        [NativeDisableParallelForRestriction] public NativeArray<AABB> bounds;

        public void Execute(int i)
        {
            AABB bound = new AABB
            {
                xmin = float.MaxValue,
                xmax = float.MinValue,
                ymin = float.MaxValue,
                ymax = float.MinValue,
            };
            for (int j = 0; j < CHUNK_MAX && i * CHUNK_MAX + j < sprites.Length; j++)
            {
                bound.Add(sprites[j].pos);
            }
            bounds[i] = bound;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct SpriteJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Hex> hices;
        [ReadOnly] public Layer layer;
        [ReadOnly] public int resolution; //Atlas resolution in pixel

        public NativeSlice<Sprite> sprites;

        public void Execute(int i)
        {
            Hex hex = hices[i];

            float2 pos = GetPosFromCoord(hex.q, hex.r, hex.h, resolution);

            Sprite sprite = sprites[i];
            sprite.pos = pos;
            if (hex.reachable)
                sprite.index = layer == Layer.DECO ? hex.tile.decoSprite : hex.tile.baseSprite;
            else
                sprite.index = -1;

            sprite.opacity = 1;
            sprites[i] = sprite;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    public struct CheckReachJob : IJob
    {
        [ReadOnly] public int size;

        public NativeArray<Hex> hices;

        public void Execute()
        {
            NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
            int start = Hex.GetIndexFromCoord(0, 0, size);
            queue.Enqueue(start);

            do
            {
                int index = queue.Dequeue();
                Hex hex = hices[index];

                hex.reachable = true;
                for (int i = 0; i < 6; i++)
                {
                    int neighbor = Hex.GetNeighborOffset(index, i, size);
                    if (neighbor != -1)
                    {
                        Hex neighborHex = hices[neighbor];
                        if (!neighborHex.reached && neighborHex.initialized)
                        {
                            neighborHex.reached = true;
                            hices[neighbor] = neighborHex;

                            queue.Enqueue(neighbor);
                        }
                    }
                }
                hices[index] = hex;
            } while (queue.Count > 0);

            queue.Dispose();
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct HexJob : IJobParallelFor
    {
        [ReadOnly] public float2 randomness;
        [ReadOnly] public Random random;
        [ReadOnly] public MapOptions options;

        [WriteOnly] public NativeArray<Hex> hices;

        public void Execute(int index)
        {
            int q, r;
            Hex.GetCoordFromIndex(out q, out r, index, options.size);
            r = -r;
            q = -q;
            int distance = Hex.DistanceFromCenter(q, r);

            var f = random.NextFloat();
            float dispersionRate = options.dispersion > 0 ? ((float)(options.size - distance) / options.dispersion) : 2;
            if (distance < (options.size - options.dispersion) || f < dispersionRate)
            {
                float2 noise = get_noise(q, r);

                int h = (int)math.floor(noise.y * options.biome.height),
                    m = (int)math.floor(noise.x * options.biome.width);

                hices[index] = new Hex
                {
                    initialized = true,
                    q = q, //Coordinate Q
                    r = r, //Coordinate R
                    h = options.terraces == 0 ? noise.y : math.round(noise.y * options.terraces) / options.terraces, //Height
                    m = noise.x, //Moisture
                    tile = options.biome.tiles[options.biome.diffusionMap[h * options.biome.width + m]],
                };
            }
            else
            {
                hices[index] = new Hex
                {
                    q = q, //Coordinate Q
                    r = r, //Coordinate R
                    tile = Tile.Null,
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float2 get_noise(int q, int r)
        {
            float height = 0, moisture = 0;
            int count = 0;
            for (int i = 0; i < options.biome.noisers.Length; ++i)
            {
                if (options.biome.noisers[i].active)
                {
                    Noiser noiser = options.biome.noisers[i];
                    height += compute_noise(ref noiser, new float3(q, r, randomness.x));
                    moisture += compute_noise(ref noiser, new float3(q, r, randomness.y));
                    count++;
                }
            }
            return new float2(math.clamp(height, 0, 1), math.clamp(moisture, 0, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float classic_noise(ref Noiser noiser, float3 pos)
        {
            return noiser.noise_type == Noiser.NoiseType.PERLIN ? noise.cnoise(pos) : noise.snoise(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ridgid_noise(ref Noiser noiser, float3 pos)
        {
            return 1f - math.abs(classic_noise(ref noiser, pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float compute_noise(ref Noiser noiser, float3 pos)
        {
            float value = 0;
            switch (noiser.noise_shape)
            {
                case Noiser.NoiseShape.CLASSIC:
                    value = (classic_noise(ref noiser, noiser.frequency * pos) + 1) * 0.5f * noiser.amplitude;
                    break;
                case Noiser.NoiseShape.RIDGID:
                    float v = ridgid_noise(ref noiser, noiser.frequency * pos);
                    v *= v;
                    value = v * noiser.amplitude;
                    break;
            }
            return value + noiser.addition;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct UpdateHexJob : IJobParallelFor
    {
        [ReadOnly] public float2 randomness;
        [ReadOnly] public Random random;
        [ReadOnly] public MapOptions options;

        [WriteOnly] public NativeArray<Hex> hices;

        public void Execute(int index)
        {
            Hex hex = hices[index];

            var dummy = random.NextFloat();

            if (!hex.reachable)
                return;

            float2 noise = get_noise(hex.q, hex.r);

            int h = (int)math.floor(noise.y * options.biome.height),
                m = (int)math.floor(noise.x * options.biome.width);


            hex.tile = options.biome.tiles[options.biome.diffusionMap[h * options.biome.width + m]];
            hex.h = options.terraces == 0 ? noise.y : math.round(noise.y * options.terraces) / options.terraces;
            hex.m = noise.x;

            hices[index] = hex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float2 get_noise(int q, int r)
        {
            float height = 0, moisture = 0;
            int count = 0;
            for (int i = 0; i < options.biome.noisers.Length; ++i)
            {
                if (options.biome.noisers[i].active)
                {
                    Noiser noiser = options.biome.noisers[i];
                    height += compute_noise(ref noiser, new float3(q, r, randomness.x));
                    moisture += compute_noise(ref noiser, new float3(q, r, randomness.y));
                    count++;
                }
            }
            return new float2(math.clamp(height, 0, 1), math.clamp(moisture, 0, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float classic_noise(ref Noiser noiser, float3 pos)
        {
            return noiser.noise_type == Noiser.NoiseType.PERLIN ? Unity.Mathematics.noise.cnoise(pos) : Unity.Mathematics.noise.snoise(pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ridgid_noise(ref Noiser noiser, float3 pos)
        {
            return 1f - math.abs(classic_noise(ref noiser, pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float compute_noise(ref Noiser noiser, float3 pos)
        {
            float value = 0;
            switch (noiser.noise_shape)
            {
                case Noiser.NoiseShape.CLASSIC:
                    value = (classic_noise(ref noiser, noiser.frequency * pos) + 1) * 0.5f * noiser.amplitude;
                    break;
                case Noiser.NoiseShape.RIDGID:
                    float v = ridgid_noise(ref noiser, noiser.frequency * pos);
                    v *= v;
                    value = v * noiser.amplitude;
                    break;
            }
            return value + noiser.addition;
        }
    }

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct StructureJob : IJob
    {
        [ReadOnly] public Random random;
        [ReadOnly] public int capacity;
        [ReadOnly] public int size;
        [ReadOnly] public int villageAmount;

        public NativeList<Village> villages;
        public NativeArray<Hex> hices;

        private int currentVillageAmount;

        public void Execute()
        {
            for(currentVillageAmount = 0; currentVillageAmount < villageAmount; currentVillageAmount++)
            {
                villages.Add(SpawnVillage());
            }
        }

        private Village SpawnVillage()
        {
            NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
            NativeHashMap<int, int> visited = new NativeHashMap<int, int>(200, Allocator.Temp); //First int is the index, second int is the propagation score
            int start;

            do
            {
                queue.Clear();
                visited.Clear();
                start = PickRandomHex();

                if (start == -1)
                    return default;

                int budget = random.NextInt(27, 56);
                queue.Enqueue(start);
                visited.Add(start, budget);

                do
                {
                    int index = queue.Dequeue();
                    Hex hex = hices[index];

                    for (int i = 0; i < 6; i++)
                    {
                        int neighbor = Hex.GetNeighborOffset(index, i, size);
                        Hex neighborHex = hices[neighbor];

                        if (neighbor != -1 && !visited.ContainsKey(neighbor) && neighborHex.initialized && neighborHex.tile.acceptStructures)
                        {
                            float heightDifference = 1 + (neighborHex.h - hex.h);
                            int propagation = (int) math.round(heightDifference * visited[index] - random.NextInt(3, 11) * neighborHex.tile.propagationCostMultiplier);

                            if(propagation > 0)
                            {
                                queue.Enqueue(neighbor);
                                visited.Add(neighbor, propagation);
                            }
                        }
                    }
                    hices[index] = hex;
                } while (queue.Count > 0);
            } while (visited.Count() < 12);

            NativeArray<int> villageHices = visited.GetKeyArray(Allocator.Temp);
            Village village = new Village
            {
                origin = hices[start],
            };

            foreach (int index in villageHices)
            {
                Hex hex = hices[index];
                hex.tile = Tile.Null; //Tile.Village
                hex.village = currentVillageAmount;
                hices[index] = hex;
            }

            queue.Dispose();
            visited.Dispose();
            villageHices.Dispose();

            return village;
        }
        private int PickRandomHex()
        {
            Hex hex = default;
            int pos = 0, distance = 0, villageDistance = size, tries = 0;
            do { 
                pos = random.NextInt(capacity); 
                hex = hices[pos];
                distance = Hex.DistanceFromCenter(hex.q, hex.r);
                for(int i = 0; i < currentVillageAmount; i++)
                {
                    villageDistance = math.min(villageDistance, Hex.Distance(hex.q, hex.r, villages[i].origin.q, villages[i].origin.r));
                }
                tries++;
            } while ((distance > (size * 0.85) || (hex.h < 0.25 || hex.h > 0.75) && (hex.m < 0.25 || hex.m > 0.75) || villageDistance < 80) && tries < 50000);
            return tries >= 50000 ? -1 : pos;
        }
    }
}