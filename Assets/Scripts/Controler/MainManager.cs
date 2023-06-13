using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainManager : MonoBehaviour
{
    public MapOptions options;
    public BiomeSO biome;
    public Material material;

    public Mesh mesh;

    public InputActionAsset inputConfig;

#if UNITY_EDITOR
    [HideInInspector] private bool initialized;
#endif
    
    public void OnEnable()
    {
        Application.targetFrameRate = -1;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.vSyncCount = 0;

        try
        {
            InputManager.Init(inputConfig);
            InputManager.gameplay.Enable();
        }
        catch { }

        //SpriteRenderer.Setup();
        Renderer.Init(mesh, material);

#if UNITY_EDITOR
        biome.manager = this;
#endif

        options.biome = biome;
        MapManager.CreateMap(options, material, ref biome.atlases);

#if UNITY_EDITOR
        initialized = true;
#endif
    }

    public void LateUpdate()
    {
        //SpriteRenderer.RenderStaticChunks();
        Renderer.Render();
    }

    public void OnDisable()
    {
#if UNITY_EDITOR
        initialized = false;
#endif
        try
        {
            options.biome.noisers.Dispose();
            options.biome.diffusionMap.Dispose();
            options.biome.tiles.Dispose();
        } catch(System.Exception e) { Debug.LogException(e); }

        //SpriteRenderer.Dispose();
        Renderer.Dispose();
        MapGenerator.ClearMap();
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
