using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public unsafe struct SpriteJob : IJobParallelFor
{
    [ReadOnly] public Layer layer;
    [ReadOnly] public int resolution; //Atlas resolution in pixel

    [NativeDisableUnsafePtrRestriction]
    public Hex* hices;
    [NativeDisableUnsafePtrRestriction]
    public HexReach* reach;

    [NativeDisableUnsafePtrRestriction]
    public Sprite* sprites;

    public void Execute(int i)
    {
        float2 pos = HexSystem.GetPosFromCoord(hices[i].q, hices[i].r, hices[i].h, resolution);

        sprites[i].pos = pos;
        if (reach[i].reachable)
            sprites[i].index = layer == Layer.DECO ? hices[i].tile.decoSprite : hices[i].tile.baseSprite;
        else
            sprites[i].index = -1;

        sprites[i].distance = HexSystem.DistanceFromCenter(hices[i].q, hices[i].r);
    }
}