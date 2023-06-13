using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Runtime.CompilerServices;
using System;

public enum Layer : int { BASE = 2, DECO = 1, OUTLINE = 0 };

/*[StructLayout(LayoutKind.Sequential)]
public struct Sprite
{
    public float index;
    public float2 pos;
    public float opacity;
    public float rotation;
    public float2 scale;
    public float dummy;
}

[StructLayout(LayoutKind.Sequential)]
public struct Spritesheet
{
    private static int _MainTexShaderID = Shader.PropertyToID("_MainTex");
    private static int _MainTintShaderID = Shader.PropertyToID("_MainTint");
    private static int _MainPosShaderID = Shader.PropertyToID("_MainPos");

    public Material material;
    public Layer layer;
    public float3 pos;
    public float4 tint;

    public int index;
    public int resolution;

    public Spritesheet(Material mat, Texture2DArray array, Layer layer)
    {
        material = Object.Instantiate(mat);
        material.SetVector(_MainTintShaderID, new float4(1));
        material.SetVector(_MainPosShaderID, new float4(0));

        material.enableInstancing = true;
        material.SetTexture(_MainTexShaderID, array);

        this.layer = layer;
        this.pos = new float3();
        this.tint = new float4(1);

        this.index = -1;
        this.resolution = array.width;
    }
    public Spritesheet(Material mat, Texture2DArray array, Layer layer, float2 pos)
    {
        material = Object.Instantiate(mat);
        material.SetVector(_MainTintShaderID, new float4(1));
        material.SetVector(_MainPosShaderID, new float4(pos, 0));

        material.enableInstancing = true;
        material.SetTexture(_MainTexShaderID, array);

        this.layer = layer;
        this.pos = new float3(pos.x, pos.y, (float)layer);
        this.tint = new float4(1);

        this.index = -1;
        this.resolution = array.width;
    }
    public Spritesheet(Material mat, Texture2DArray array, Layer layer, float2 pos, float4 tint)
    {
        material = Object.Instantiate(mat);
        material.SetVector(_MainTintShaderID, tint);
        material.SetVector(_MainPosShaderID, new float4(pos, 0));

        material.enableInstancing = true;
        material.SetTexture(_MainTexShaderID, array);

        this.layer = layer;
        this.pos = new float3(pos.x, pos.y, (float)layer);
        this.tint = tint;

        this.index = -1;
        this.resolution = array.width;
    }
}

*/
[StructLayout(LayoutKind.Sequential)]
[Serializable]
public struct Sprite
{
    public float2 pos; //X = X, Y = Y, Z = -Y / 1000 + layer for sorting purpose
    public float rotation;
    public int index;

#if UNITY_EDITOR
    public override string ToString()
    {
        return string.Format("{0}", index);
    }
#endif
}
[StructLayout(LayoutKind.Sequential)]
public struct Spritesheet
{
    private static readonly int _MainTexShaderID = Shader.PropertyToID("_MainTex");
    private static readonly int _MainTintShaderID = Shader.PropertyToID("_MainTint");
    private static readonly int _MainPosShaderID = Shader.PropertyToID("_MainPos");

    public float4 tint;
    public float3 offset;
    public int layer;
    public MaterialPropertyBlock mat; //Contains the Spritesheet atlas

    public int resolution;

    private GraphicsBuffer buffer;

    public Bounds bounds => new Bounds(new float3(0, 0, layer), new float3(2048, 2048, 0));

    public Spritesheet(Texture2DArray array, Layer layer, float2 pos, float4 tint)
    {
        mat = new MaterialPropertyBlock();
        mat.SetVector(_MainTintShaderID, tint);
        mat.SetVector(_MainPosShaderID, new float4(pos, (float)layer, 0));

        mat.SetTexture(_MainTexShaderID, array);

        this.layer = (int)layer;
        this.offset = new float3(pos.x, pos.y, (float)layer);
        this.tint = tint;

        this.resolution = array.width;

        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, 1, sizeof(float) * 4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSprites(NativeSlice<Sprite> sprites)
    {
        if(sprites.Length != buffer.count)
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GraphicsBuffer.UsageFlags.LockBufferForWrite, sprites.Length, sizeof(float) * 4);

        var buff = buffer.LockBufferForWrite<Sprite>(0, sprites.Length);
        sprites.CopyTo(buff);
        buffer.UnlockBufferAfterWrite<Sprite>(sprites.Length);

        mat.SetBuffer(Renderer._SpriteBlockShaderID, buffer);
    }

    public void Clear()
    {
        buffer?.Dispose();
        buffer = null;
    }
}