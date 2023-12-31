using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public static class HexSystem
{
    private static readonly int2[] NeighborDirections = { new int2(0, 1), new int2(-1, 1), new int2(1, 0), new int2(-1, 0), new int2(1, -1), new int2(0, -1) };

    public static NativeArray<Hex> hices;

    //Some properties won't be stored in the hex directly, so we won't have to create a temp array to extract them and we can directly send them to the GPU.
    public static NativeArray<HexVisibility> visibility;
    public static NativeArray<HexReach> reach;
    public static NativeArray<HexVillage> village;

    public static NativeList<Village> villageList;

    public static int size;
    public static int capacity => (size * (size + 1)) * 3 + 1;

    public static void Init(int size)
    {
        HexSystem.size = size;
        hices = new NativeArray<Hex>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        visibility = new NativeArray<HexVisibility>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        reach = new NativeArray<HexReach>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        village = new NativeArray<HexVillage>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }
    public static unsafe void Generate(MapOptions options)
    {
        Random random = new Random(options.seed);

        HexJob hexJob = new HexJob
        {
            options = options,
            random = random,
            randomness = random.NextFloat2(1 << 20),
            hices = (Hex*)hices.GetUnsafePtr(),
            reach = (HexReach*)reach.GetUnsafePtr(),
        };
        JobHandle handle = hexJob.ScheduleByRef(options.capacity, Constants.GRANULARITY);

        StructureJob structureJob = new StructureJob
        {
            random = random,
            size = options.size,
            hices = (Hex*)hices.GetUnsafePtr(),
            hexVillages = (HexVillage*)village.GetUnsafePtr(),
            capacity = options.capacity,
            villages = new NativeList<Village>(Allocator.Persistent),
            villageAmount = options.size / 100,
        };
        handle = structureJob.ScheduleByRef(handle);

        CheckReachJob reachJob = new CheckReachJob
        {
            size = options.size,
            hices = (Hex*)hices.GetUnsafePtr(),
            reach = (HexReach*)reach.GetUnsafePtr(),
        };

        reachJob.ScheduleByRef(handle).Complete();
        villageList = structureJob.villages;
    }
    public static void Dispose()
    {
        if (hices.IsCreated) hices.Dispose();
        if (visibility.IsCreated) visibility.Dispose();
        if (reach.IsCreated) reach.Dispose();
        if (village.IsCreated) village.Dispose();

        if (villageList.IsCreated) villageList.Dispose();
    }

    #region Utils methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetPosFromCoord(int q, int r, float h, int resolution)
    {
        return new float2((q + (r / 2f)) * ((resolution - 1f) / resolution), (r) * (1f / 2 * (3f / 4)) + (h * Constants.TERRACES_HEIGHT) - 1.5f) * ((resolution - 1f) / resolution);
    }

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
            return (int)((3 * size + 1 + r) / 2f * (r + size));
        }
        else
        { // lower trapezoid
            return (int)((3 * size + 2) / 2f * (size + 1)
                + (4 * size + 2 - r) / 2f * (r - 1));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetCoordFromIndex(out int q, out int r, int index, int size)
    {
        r = (int)math.floor(rowFromIndex(index, size));
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
    #endregion
}

[StructLayout(LayoutKind.Sequential)]
public struct Hex
{
    public int idx;
    public int q;
    public int r;
    public float h, m;
    public Tile tile;

#if UNITY_EDITOR
    public override string ToString()
    {
        return string.Format("{0}/{1}", q, r);
    }
#endif
}
[StructLayout(LayoutKind.Sequential)]
public struct HexVisibility
{
    public bool visible, explored;
}
[StructLayout(LayoutKind.Sequential)]
public struct HexReach
{
    public bool reached, reachable;
}
[StructLayout(LayoutKind.Sequential)]
public struct HexVillage
{
    public int village;
}