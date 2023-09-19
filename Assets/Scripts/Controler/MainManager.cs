using UnityEngine;
using UnityEngine.InputSystem;

public class MainManager : MonoBehaviour
{
    public MapOptions options;
    public BiomeSO biome;
    public Material mat;

    public InputActionAsset inputConfig;

#if UNITY_EDITOR
    [HideInInspector] private bool initialized;
#endif
    
    public void OnEnable()
    {
        try
        {
            InputManager.Init(inputConfig);
            InputManager.gameplay.Enable();
        }
        catch { }

        Renderer.Init(mat);

#if UNITY_EDITOR
        biome.manager = this;
#endif

        options.biome = biome;
        MapManager.CreateMap(options, ref biome.atlases);

        //GraphicsBuffer buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, HexSystem.capacity, sizeof(bool) * 2);

#if UNITY_EDITOR
        initialized = true;
#endif
    }

    public void LateUpdate()
    {
        Renderer.Render();
    }

    public void OnDisable()
    {
#if UNITY_EDITOR
        initialized = false;
#endif
        try
        {
            options.biome.sampler.Dispose();
            options.biome.diffusionMap.Dispose();
            options.biome.tiles.Dispose();
        } catch(System.Exception e) { Debug.LogException(e); }

        MapManager.Dispose();
        Renderer.Dispose();
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (Application.isPlaying && initialized)
        {
            OnDisable();
            OnEnable();
        }
    }
#endif
}