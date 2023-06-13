using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

[StructLayout(LayoutKind.Sequential)]
public struct Hex
{
    private static readonly int2[] NeighborDirections = { new int2(0, 1), new int2(-1, 1), new int2(1, 0), new int2(-1, 0), new int2(1, -1), new int2(0, -1) };

    public bool initialized;
    public int q;
    public int r;
    public float h, m;
    public Tile tile;

    public bool reachable, visible, explored;

#if UNITY_EDITOR
    public override string ToString()
    {
        return string.Format("{0}/{1}", q, r);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndexFromCoord(int q, int r, int size)
    {
        int startIndex = startIndexOfRow(r, size);
        return startIndex + q - firstColumn(r, size);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float rowFromIndex(int index, int size)
    {
        float crossoverIndex = (3 / 2f * size + 1) * (1 + size);
        if (index < crossoverIndex)
        {
            // Upper trapezoid
            float a = 1 / 2f;
            float b = 2 * size + 1 / 2f;
            float c = 3 / 2f * size * size + 1 / 2f * size - index;
            float r = (-b + math.sqrt(b * b - 4 * a * c)) / (2f * a);
            return r;
        }
        else
        {
            // Lower trapezoid
            float a = -1 / 2f;
            float b = 2 * size + 3 / 2f;
            float c = 3 / 2f * size * size + 1 / 2f * size - index;
            float r = (-b + math.sqrt(b * b - 4 * a * c)) / (2f * a);
            return r;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int firstColumn(int r, int size)
    {
        return -size - math.min(r, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int startIndexOfRow(int r, int size)
    {
        if (r <= 0)
        { // upper trapezoid
            return (int) ((3 * size + 1 + r) / 2f * (r + size));
        }
        else
        { // lower trapezoid
            return (int) ((3 * size + 2) / 2f * (size + 1)
                + (4 * size + 2 - r) / 2f * (r - 1));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCoordFromIndex(out int q, out int r, int index, int size)
    {
        r = (int) math.floor(rowFromIndex(index, size));
        q = (index - startIndexOfRow(r, size)) + firstColumn(r, size);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNeighborOffset(int index, int neighbor, int size)
    {
        int2 pos = NeighborDirections[neighbor];
        int q, r;
        GetCoordFromIndex(out q, out r, index, size);

        if (DistanceFromCenter(q + pos.x, r + pos.y) > size)
            return -1;

        return GetIndexFromCoord(q + pos.x, r + pos.y, size);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNeighborOffset(int q, int r, int neighbor, int size)
    {
        int2 pos = NeighborDirections[neighbor];

        if (DistanceFromCenter(q + pos.x, r + pos.y) > size)
            return -1;

        return GetIndexFromCoord(q + pos.x, r + pos.y, size);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DistanceFromCenter(int q, int r)
    {
        if (q == 0 && r == 0) return 0;
        if (q > 0 && r >= 0) return q + r;
        if (q <= 0 && r > 0) return (-q < r) ? r : -q;
        if (q < 0) return -q - r;
        return (-r > q) ? -r : q;
    }
}