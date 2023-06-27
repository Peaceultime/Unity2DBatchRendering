using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static unsafe class Extensions
{
    public static ref T Ref<T>(this NativeArray<T> arr, int index) where T : struct
    {
        return ref UnsafeUtility.ArrayElementAsRef<T>(arr.GetUnsafePtr(), index);
    }
    public static ref T Ref<T>(this NativeList<T> lst, int index) where T : unmanaged
    {
        return ref UnsafeUtility.ArrayElementAsRef<T>(lst.GetUnsafePtr(), index);
    }
}