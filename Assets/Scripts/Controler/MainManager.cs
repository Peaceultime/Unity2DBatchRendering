using System.Collections;
using TMPro;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainManager : MonoBehaviour
{
    public MapOptions options;
    public BiomeSO biome;
    public Material material;

    public Mesh mesh;

    public InputActionAsset inputConfig;

    private int oldSize;

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

        //SpriteRenderer.Setup();
        Renderer.Init(mesh, material);

#if UNITY_EDITOR
        biome.manager = this;
#endif

        oldSize = options.size;
        options.biome = biome;
        MapManager.CreateMap(options, material, ref biome.atlases);

#if UNITY_EDITOR
        initialized = true;
#endif

        //StartCoroutine(RegenTest());
    }

    public IEnumerator RegenTest()
    {
        while (true)
        {
            var noiser = options.biome.noisers[0];
            noiser.frequency += 0.0000005f;
            options.biome.noisers[0] = noiser;
            MapManager.UpdateMap(options);

            yield return new WaitForSecondsRealtime(0.05f);
        }
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
            if(oldSize != options.size)
            {
                oldSize = options.size;
                OnDisable();
                OnEnable();
            }
            else
            {
                options.biome = biome;
                MapManager.UpdateMap(options);
            }
        }
    }
#endif
}