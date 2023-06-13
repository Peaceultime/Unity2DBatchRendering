using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public struct AABB //Chunk component
{
	public float xmin;
	public float xmax;
	public float ymin;
	public float ymax;

    public void Add(float x, float y)
    {
        this.xmin = math.min(this.xmin, x);
        this.ymin = math.min(this.ymin, y);
        this.xmax = math.max(this.xmax, x);
        this.ymax = math.max(this.ymax, y);
    }
    public void Add(float2 pos)
	{
		this.xmin = math.min(this.xmin, pos.x);
		this.ymin = math.min(this.ymin, pos.y);
		this.xmax = math.max(this.xmax, pos.x);
		this.ymax = math.max(this.ymax, pos.y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Intersect(float xmin, float xmax, float ymin, float ymax)
	{
		return xmin <= this.xmax && xmax >= this.xmin && ymin <= this.ymax && ymax >= this.ymin;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Intersect(Bounds bounds)
	{
		return Intersect(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y, bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Intersect(AABB aabb)
	{
		return Intersect(aabb.xmin, aabb.xmax, aabb.ymin, aabb.ymax);
	}

	public static implicit operator Bounds(AABB a) { return new Bounds((new float3(a.xmax, a.ymax, 0) + new float3(a.xmin, a.ymin, 0)) / 2, new float3(a.xmax, a.ymax, 0) - new float3(a.xmin, a.ymin, 0)); }
	public static implicit operator AABB(Bounds b) { return new AABB { xmin = b.min.x, ymin = b.min.y, xmax = b.max.x, ymax = b.max.y }; }
}