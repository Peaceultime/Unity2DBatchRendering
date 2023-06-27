using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public unsafe struct CheckReachJob : IJob
{
    [ReadOnly] public int size;

    [NativeDisableUnsafePtrRestriction]
    public Hex* hices;
    [NativeDisableUnsafePtrRestriction]
    public HexReach* reach;

    public void Execute()
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        int start = HexSystem.GetIndexFromCoord(0, 0, size);
        queue.Enqueue(start);

        do
        {
            int index = queue.Dequeue();

            reach[index].reachable = true;
            for (int i = 0; i < 6; i++)
            {
                int neighbor = HexSystem.GetNeighborOffset(index, i, size);
                if (neighbor != -1 && !reach[neighbor].reached)
                {
                    reach[neighbor].reached = true;

                    queue.Enqueue(neighbor);
                }
            }
        } while (queue.Count > 0);

        queue.Dispose();
    }
}