using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
public unsafe struct StructureJob : IJob
{
    [ReadOnly] public Random random;
    [ReadOnly] public int capacity;
    [ReadOnly] public int size;
    [ReadOnly] public int villageAmount;

    public NativeList<Village> villages;
    [NativeDisableUnsafePtrRestriction]
    public Hex* hices;
    [NativeDisableUnsafePtrRestriction]
    public HexVillage* hexVillages;

    private int currentVillageAmount;

    public void Execute()
    {
        for (currentVillageAmount = 0; currentVillageAmount < villageAmount; currentVillageAmount++)
        {
            villages.Add(SpawnVillage());
        }
    }

    private Village SpawnVillage()
    {
        NativeQueue<int> queue = new NativeQueue<int>(Allocator.Temp);
        NativeHashMap<int, int> visited = new NativeHashMap<int, int>(200, Allocator.Temp); //First int is the index, second int is the propagation score
        int start;

        do
        {
            queue.Clear();
            visited.Clear();
            start = PickRandomHex();

            if (start == -1)
                return default;

            int budget = random.NextInt(36, 69);
            queue.Enqueue(start);
            visited.Add(start, budget);

            do
            {
                int index = queue.Dequeue();

                for (int i = 0; i < 6; i++)
                {
                    int neighbor = HexSystem.GetNeighborOffset(index, i, size);

                    if (neighbor != -1 && !visited.ContainsKey(neighbor) && hices[neighbor].tile.acceptStructures)
                    {
                        float heightDifference = 1 + (hices[neighbor].h - hices[index].h);
                        int propagation = (int)math.round(heightDifference * visited[index] - random.NextInt(4, 19) * hices[neighbor].tile.propagationCostMultiplier);

                        if (propagation > 0)
                        {
                            queue.Enqueue(neighbor);
                            visited.Add(neighbor, propagation);
                        }
                    }
                }
            } while (queue.Count > 0);
        } while (visited.Count() < 24);

        NativeArray<int> villageHices = visited.GetKeyArray(Allocator.Temp);
        Village village = new Village
        {
            origin = hices[start],
        };

        foreach (int index in villageHices)
        {
            hices[index].tile = Tile.Null; //Tile.Village 
            hexVillages[index].village = currentVillageAmount;
        }

        queue.Dispose();
        visited.Dispose();
        villageHices.Dispose();

        return village;
    }
    private int PickRandomHex()
    {
        Hex hex = default;
        int pos = 0, distance = 0, villageDistance = size, tries = 0;
        do
        {
            pos = random.NextInt(capacity);
            hex = hices[pos];
            distance = HexSystem.DistanceFromCenter(hex.q, hex.r);
            for (int i = 0; i < currentVillageAmount; i++)
            {
                villageDistance = math.min(villageDistance, HexSystem.Distance(hex.q, hex.r, villages[i].origin.q, villages[i].origin.r));
            }
            tries++;
        } while ((distance > (size * 0.85) || (hex.h < 0.25 || hex.h > 0.75) && (hex.m < 0.25 || hex.m > 0.75) || villageDistance < 80) && tries < 50000);
        return tries >= 50000 ? -1 : pos;
    }
}