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

    public bool reached, reachable, visible, explored;
    public int village;

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
        return Distance(q, r, 0, 0);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Distance(int q, int r, int q2, int r2)
    {
        int tmp_q = q - q2, tmp_r = r - r2;
        return (math.abs(tmp_q) + math.abs(tmp_q + tmp_r) + math.abs(tmp_r)) / 2;
    }
}

/*
 
function axial_subtract(a, b):
    return Hex(a.q - b.q, a.r - b.r)

function axial_distance(a, b):
    var vec = axial_subtract(a, b)
    return (abs(vec.q)
          + abs(vec.q + vec.r)
          + abs(vec.r)) / 2
 
 */