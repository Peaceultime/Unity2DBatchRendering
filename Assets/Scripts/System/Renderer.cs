using UnityEngine;
using Unity.Mathematics;

public static class Renderer
{
    private const int MAX_SPRITESHEET_AMOUNT = 32;

    public static readonly int _SpriteBlockShaderID = Shader.PropertyToID("_SpriteBlock");
    public static readonly int _PosBlockShaderID = Shader.PropertyToID("_PosBlock");

    private static readonly int[] QUAD_INDICES = { 0, 3, 1, 3, 0, 2 };
    private static readonly float2[] uvs = { new float2(0, 0), new float2(1, 0), new float2(0, 1), new float2(1, 1), };

    private static RenderParams rp;

    private static GraphicsBuffer commandBuffer, indexBuffer, uvBuffer;

    private static Spritesheet[] spritesheets;
    private static int spritesheetCount;

    public static void Init(Material mat = null, Camera camera = null)
    {
        if(mat == null)
        {
            var shader = Shader.Find("Custom/Sprite/Indirect Instanced");
            mat = new Material(shader);
        }
        mat.enableInstancing = true;

        uvBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, uvs.Length, sizeof(float) * 2);
        uvBuffer.SetData(uvs);
        mat.SetBuffer(_PosBlockShaderID, uvBuffer);

        rp = new RenderParams(mat);

        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 6, sizeof(int));
        indexBuffer.SetData(QUAD_INDICES);

        spritesheets = new Spritesheet[MAX_SPRITESHEET_AMOUNT];
        commandBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, MAX_SPRITESHEET_AMOUNT, GraphicsBuffer.IndirectDrawIndexedArgs.size);
    }
    public static void AddSpritesheet(Spritesheet spritesheet, int count)
    {
        spritesheets[spritesheetCount] = spritesheet;
        GraphicsBuffer.IndirectDrawIndexedArgs[] args = { new GraphicsBuffer.IndirectDrawIndexedArgs { instanceCount = (uint) count, baseVertexIndex = 0, indexCountPerInstance = 6, startIndex = 0 } };
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

        for(int i = 0; i < spritesheetCount; i++)
            spritesheets[i].Clear();

        spritesheetCount = 0;

        commandBuffer = null;
        indexBuffer = null;
        uvBuffer = null;
    }
}