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

    public static NativeArray<Hex> hices;
    public static NativeArray<Sprite> sprites;


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

        CheckReachJob reachJob = new CheckReachJob
        {
            size = options.size,
            hices = hexJob.hices,
        };
        reachJob.ScheduleByRef(handle).Complete();
        hices = reachJob.hices;
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

        /*ChunkJob chunkJob = new ChunkJob
        {
            sprites = spriteJob.sprites,
            bounds = new NativeArray<AABB>(count / CHUNK_MAX + 1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory),
        };*/

        //chunkJob.ScheduleByRef(count / CHUNK_MAX + 1, handle).Complete();

        /*for(int i = 0; i < chunkJob.bounds.Length; i++)
        {
            SpriteRenderer.AddChunk(ref spritesheet, chunkJob.sprites.Slice(i, math.clamp(CHUNK_MAX, 0, count - i * CHUNK_MAX)), chunkJob.bounds[i]);
        }*/
        handle.Complete();
        spritesheet.SetSprites(spriteJob.sprites);
        Renderer.AddSpritesheet(spritesheet, count);

        //chunkJob.bounds.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetPosFromCoord(int q, int r, float h, int resolution)
    {
        return new float2((q + (r / 2f)) * ((resolution -1f) / resolution), (r) * (1f / 2 * (3f / 4)) + (h * 7.5f) - 1.5f) * ((resolution -1f) / resolution);
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
            sprite.pos = pos; //float2: 8 bytes (8)
            if (hex.reachable)
                sprite.index = layer == Layer.DECO ? hex.tile.decoSprite : hex.tile.baseSprite; //float: 4 bytes (12)
            else
                sprite.index = -1;
            //sprite.opacity = 1; //4 bytes (16)
                                //Could add new properties to the sprite struct ?
            //sprite.scale.x = 1; //4 bytes (20)
            //sprite.scale.y = 1; //4 bytes (24)
            sprite.rotation = 0; //4 bytes (28)
                                 //4 bytes remaining for 32 bytes component optimization on GPU
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
            NativeHashSet<int> visited = new NativeHashSet<int>(hices.Length, Allocator.Temp);
            int start = Hex.GetIndexFromCoord(0, 0, size);
            queue.Enqueue(start);
            visited.Add(start);

            do
            {
                int index = queue.Dequeue();
                Hex hex = hices[index];

                hex.reachable = true;
                for (int i = 0; i < 6; i++)
                {
                    int neighbor = Hex.GetNeighborOffset(index, i, size);
                    if (neighbor != -1 && !visited.Contains(neighbor))
                    {
                        Hex neighborHex = hices[neighbor];

                        if (neighborHex.initialized)
                        {
                            queue.Enqueue(neighbor);
                            visited.Add(neighbor);
                        }
                    }
                }
                hices[index] = hex;
            } while (queue.Count > 0);

            queue.Dispose();
            visited.Dispose();
        }
    }
    public static void ClearMap()
    {
        hices.Dispose();
        sprites.Dispose();
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
                    explored = false,
                    visible = false,
                    tile = options.biome.tiles[options.biome.diffusionMap[h * options.biome.width + m]],
                    reachable = false,
                };
            }
            else
            {
                hices[index] = new Hex
                {
                    initialized = false,
                    q = q, //Coordinate Q
                    r = r, //Coordinate R
                    explored = false,
                    visible = false,
                    tile = Tile.Null,
                    reachable = false,
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
                if (options.biome.noisers[i].active != 0)
                {
                    Noiser noiser = options.biome.noisers[i];
                    height += compute_noise(ref noiser, new float3(q, r, randomness.x));
                    moisture += compute_noise(ref noiser, new float3(q, r, randomness.y));
                    count++;
                }
            }
            return new float2(math.clamp(height / count, 0, 0.9999999f), math.clamp(moisture / count, 0, 0.9999999f));
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
            float amplitude = 1;
            float frequency = noiser.frequency;
            switch (noiser.noise_shape)
            {
                case Noiser.NoiseShape.CLASSIC:
                    for (uint i = 0; i < noiser.octaves; ++i)
                    {
                        value += (classic_noise(ref noiser, frequency * pos) + 1) * 0.5f * amplitude;

                        frequency *= noiser.roughness;
                        amplitude *= noiser.amplitude;
                    }
                    break;
                case Noiser.NoiseShape.RIDGID:
                    float weight = 1;
                    for (uint i = 0; i < noiser.octaves; ++i)
                    {
                        float v = ridgid_noise(ref noiser, frequency * pos);
                        v *= v;
                        v *= weight;
                        weight = math.clamp(v * noiser.weight, 0, 1);
                        value += v * amplitude;

                        frequency *= noiser.roughness;
                        amplitude *= noiser.amplitude;
                    }
                    break;
            }
            return value * noiser.multiply + noiser.addition;
        }
    }
}
