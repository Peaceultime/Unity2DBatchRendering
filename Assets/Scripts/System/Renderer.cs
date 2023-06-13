using Unity.Collections;
using UnityEngine.Rendering;
using UnityEngine;
using Unity.Mathematics;

public static class Renderer
{
    private const int MAX_SPRITESHEET_AMOUNT = 32;

    public static readonly int _SpriteBlockShaderID = Shader.PropertyToID("_SpriteBlock");
    public static readonly int _PosBlockShaderID = Shader.PropertyToID("_PosBlock");

    //private static readonly int[] QUAD_INDICES = { 0, 2, 1, 2, 3, 1 };
    //private static readonly float2[] uvs = { new float2(0, 0), new float2(0, 1), new float2(1, 0), new float2(1, 1), };

    private static Material mat;
    private static RenderParams rp;

    private static GraphicsBuffer commandBuffer, indexBuffer, uvBuffer;

    private static Spritesheet[] spritesheets;
    private static int spritesheetCount;

    private static Mesh mesh;

    public static void Init(Mesh mesh, Material mat = null, Camera camera = null)
    {
        Renderer.mesh = mesh;

        mat ??= new Material(Shader.Find("Custom/Sprite/Instanced"));
        mat.enableInstancing = true;

        uvBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.uv.Length, sizeof(float) * 2);
        uvBuffer.SetData(mesh.uv);
        mat.SetBuffer(_PosBlockShaderID, uvBuffer);

        rp = new RenderParams(mat);

        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 6, sizeof(int));
        indexBuffer.SetData(mesh.triangles);

        spritesheets = new Spritesheet[MAX_SPRITESHEET_AMOUNT];
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, MAX_SPRITESHEET_AMOUNT, GraphicsBuffer.IndirectDrawIndexedArgs.size);
    }
    public static void AddSpritesheet(Spritesheet spritesheet, int count)
    {
        spritesheets[spritesheetCount] = spritesheet;
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = { new GraphicsBuffer.IndirectDrawIndexedArgs { instanceCount = (uint) count, baseVertexIndex = mesh.GetBaseVertex(0), indexCountPerInstance = mesh.GetIndexCount(0), startIndex = mesh.GetIndexStart(0) } };
        commandBuffer.SetData(args, 0, spritesheetCount, 1);
        spritesheetCount++;
    }

    public static void Render()
    {
        for (int i = 0; i < spritesheetCount; i++)
        {
            Spritesheet spritesheet = spritesheets[i];
            rp.matProps = spritesheet.mat;
            rp.rendererPriority = spritesheet.layer;
            rp.worldBounds = spritesheet.bounds;

            Graphics.RenderPrimitivesIndexedIndirect(rp, MeshTopology.Triangles, indexBuffer, commandBuffer, 1, i);
        }
    }
    public static void Dispose()
    {
        commandBuffer?.Dispose();
        indexBuffer?.Dispose();
        uvBuffer?.Dispose();

        spritesheetCount = 0;

        commandBuffer = null;
        indexBuffer = null;
        uvBuffer = null;
    }
}