using UnityEngine;

public static class MeshBuilder
{
    public static Mesh CreateQuad()
    {
        Vector3[] vertices =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0)
        };
        Vector2[] uv =
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        int[] indices =
        {
            0, 2, 1, 2, 3, 1
        };
        return new Mesh
        {
            vertices = vertices,
            uv = uv,
            triangles = indices,
        };
    }
}