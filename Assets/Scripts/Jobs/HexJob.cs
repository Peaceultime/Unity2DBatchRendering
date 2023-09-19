using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public unsafe struct HexJob : IJobParallelFor
{
    [ReadOnly] public float2 randomness;
    [ReadOnly] public Random random;
    [ReadOnly] public MapOptions options;

    [NativeDisableUnsafePtrRestriction]
    public Hex* hices;
    [NativeDisableUnsafePtrRestriction]
    public HexReach* reach;

    public void Execute(int index)
    {
        int q, r;
        HexSystem.GetCoordFromIndex(out q, out r, index, options.size);
        r = -r;
        q = -q;
        int distance = HexSystem.DistanceFromCenter(q, r);

        var f = random.NextFloat();
        float dispersionRate = options.dispersion > 0 ? ((float)(options.size - distance) / options.dispersion) : 2;
        bool reached = distance < (options.size - options.dispersion) || f < dispersionRate;
        float2 noise = options.biome.sampler.get_noise(q, r, randomness);

        int h = (int)math.floor(noise.y * options.biome.height),
            m = (int)math.floor(noise.x * options.biome.width);

        hices[index] = new Hex
        {
            idx = index,
            q = q,
            r = r,
            h = options.terraces == 0 ? noise.y : math.round(noise.y * options.terraces) / options.terraces,
            m = noise.x,
            tile = reached ? options.biome.tiles[options.biome.diffusionMap[h * options.biome.width + m]] : Tile.Null,
        };
        reach[index].reached = !reached;
    }
}