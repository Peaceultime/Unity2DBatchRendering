using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class MapManager
{
    public static void CreateMap(MapOptions options, Material mat, ref Texture2DArray[] atlases)
    {
#if UNITY_EDITOR
        Stopwatch watch = new Stopwatch();
        watch.Start();
        MapGenerator.GenerateMap(options);
        watch.Stop();
        Debug.Log(string.Format("Generating {0} hexes in {1}ms", options.capacity, watch.ElapsedMilliseconds));
        watch.Reset();
        watch.Start();
        MapGenerator.RenderMap(options, mat, atlases);
        watch.Stop();
        Debug.Log(string.Format("Generating {0} sprites in {1}ms", options.capacity * 2, watch.ElapsedMilliseconds));
#else
        MapGenerator.GenerateMap(options);
        MapGenerator.RenderMap(options, mat, atlases);
#endif
    }
    public static void UpdateMap(MapOptions options)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        MapGenerator.RegenerateMap(options);
        watch.Stop();
        Debug.Log(string.Format("Regenerating {0} hexes in {1}ms", options.capacity, watch.ElapsedMilliseconds));
    }
}