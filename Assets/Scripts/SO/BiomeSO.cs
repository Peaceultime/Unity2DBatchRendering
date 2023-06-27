using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[CreateAssetMenu(fileName = "Biome", menuName = "Custom/Biome", order = 1)]
public class BiomeSO : ScriptableObject
{
    public Noiser[] noisers;
    public TileSO[] tiles;
    public Texture2D diffusionMap;

    public Texture2DArray[] atlases;

    public static implicit operator Biome(BiomeSO so)
    {
        var native_tiles = new NativeArray<Tile>(so.tiles.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        for (int i = 0; i < so.tiles.Length; ++i)
            native_tiles[i] = so.tiles[i];
        return new Biome
        {
            sampler = new NoiseSampler { noisers = new NativeArray<Noiser>(so.noisers, Allocator.Persistent) },
            tiles = native_tiles,
            diffusionMap = TextureToInt(so.diffusionMap),
            width = so.diffusionMap.width,
            height = so.diffusionMap.height,
        };
    }
    private static NativeArray<byte> TextureToInt(Texture2D texture)
    {
        TextureConverter converter = new TextureConverter { input = texture.GetRawTextureData<Color32>(), output = new NativeArray<byte>(texture.width * texture.height, Allocator.Persistent, NativeArrayOptions.UninitializedMemory) };
        converter.Schedule(converter.output.Length, 32).Complete();
        return converter.output;
    }
    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct TextureConverter : IJobParallelFor
    {
        public NativeArray<byte> output;
        public NativeArray<Color32> input;

        public void Execute(int index)
        {
            output[index] = input[index].r;
        }
    }

#if UNITY_EDITOR
    [HideInInspector] public MainManager manager;
    public void OnValidate()
    {
        manager?.OnValidate();
    }
#endif
}