using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public static class SpriteRenderer
{
    /*private static readonly int _SpriteBlockShaderID = Shader.PropertyToID("_SpriteBlock");
    private static Mesh mesh;
    private static List<SpriteChunk> chunks;

    public struct SpriteChunk
    {
        public RenderParams rp;
        public GraphicsBuffer buffer;
    }
    public static void AddChunk(ref Spritesheet spritesheet, NativeSlice<Sprite> sprites, AABB bounds) //Creates a new static sheet struct and populate the buffer with the sprites data
    {
        SpriteChunk sheet = new SpriteChunk
        {
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, sprites.Length, sizeof(float) * 8),
            rp = new RenderParams
            {
                camera = null,
                lightProbeProxyVolume = null,
                lightProbeUsage = LightProbeUsage.Off,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
                receiveShadows = false,
                reflectionProbeUsage = ReflectionProbeUsage.Off,
                rendererPriority = 10,
                renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = bounds,
                material = spritesheet.material,
                matProps = new MaterialPropertyBlock(),
            },
        };

        sprites.CopyTo(sheet.buffer.LockBufferForWrite<Sprite>(0, sprites.Length));
        sheet.buffer.UnlockBufferAfterWrite<Sprite>(sprites.Length);
        sheet.rp.matProps.SetBuffer(_SpriteBlockShaderID, sheet.buffer);

        chunks.Add(sheet);
    }
    public static void UpdateChunk(int idx, NativeSlice<Sprite> sprites) //Need to test if when the nativearray is updated, the nativeslice is also updated.
    {
        SpriteChunk chunk = chunks[idx];

        sprites.CopyTo(chunk.buffer.LockBufferForWrite<Sprite>(0, sprites.Length));
        chunk.buffer.UnlockBufferAfterWrite<Sprite>(sprites.Length);
        chunk.rp.matProps.SetBuffer(_SpriteBlockShaderID, chunk.buffer);
    }
    public static void Setup()
    {
        mesh = MeshBuilder.CreateQuad();

        chunks = new List<SpriteChunk>();
    }
    public static void RenderStaticChunks()
    {
        if (chunks == null)
            return;

        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            SpriteChunk staticSheet = chunks[i];

            Graphics.RenderMeshPrimitives(staticSheet.rp, mesh, 0, staticSheet.buffer.count);
        }
    }
    public static void Dispose()
    {
        if (chunks == null)
            return;
        for (int i = 0; i < chunks.Count; i++)
        {
            SpriteChunk staticSheet = chunks[i];
            staticSheet.buffer?.Dispose();
            staticSheet.buffer = null;
        }
        chunks.Clear();
        chunks = null;
    }*/
}