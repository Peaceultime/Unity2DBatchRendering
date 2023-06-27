using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

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

public static class MapManager
{
    public static NativeArray<Sprite> sprites;
    public static NativeList<Village> villages;

    public static List<Spritesheet> spritesheets = new List<Spritesheet>();

    public static void CreateMap(MapOptions options, ref Texture2DArray[] atlases)
    {
#if UNITY_EDITOR
        Stopwatch sw = Stopwatch.StartNew();
        HexSystem.Init(options.size);
        HexSystem.Generate(options);

        RenderMap(options, ref atlases);
        sw.Stop();
        Debug.Log(string.Format("Generating the map in {0}ms", sw.ElapsedMilliseconds));
#else
        HexSystem.Init(options.size);
        HexSystem.Generate(options);

        RenderMap(options, ref atlases);
#endif
    }

    private static void RenderMap(MapOptions options, ref Texture2DArray[] atlases)
    {
        sprites = new NativeArray<Sprite>(options.capacity * 2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        Spritesheet spritesheetBase = new Spritesheet(atlases[2], Layer.BASE, new float2(0, 0), new float4(1));
        Spritesheet spritesheetDeco = new Spritesheet(atlases[1], Layer.DECO, new float2(0, 0.5f), new float4(1));

        GenerateSpriteLayer(spritesheetBase, 0, options.capacity);
        GenerateSpriteLayer(spritesheetDeco, options.capacity, options.capacity);
    }

    private static unsafe void GenerateSpriteLayer(Spritesheet spritesheet, int start, int count)
    {
        SpriteJob spriteJob = new SpriteJob
        {
            hices = (Hex*) HexSystem.hices.GetUnsafePtr(),
            reach = (HexReach*) HexSystem.reach.GetUnsafePtr(),
            layer = (Layer)spritesheet.layer,
            sprites = (Sprite*) sprites.GetUnsafePtr() + start,
            resolution = spritesheet.resolution,
        };

        spriteJob.ScheduleByRef(count, Constants.GRANULARITY).Complete();

        spritesheet.SetSprites(sprites.Slice(start, count));
        Renderer.AddSpritesheet(spritesheet, count);

        spritesheets.Add(spritesheet);
    }

    public static void Dispose()
    {
        HexSystem.Dispose();

        if(sprites.IsCreated) sprites.Dispose();
        if(villages.IsCreated) villages.Dispose();

        spritesheets.Clear();
    }
}