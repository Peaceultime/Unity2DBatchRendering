using Unity.Collections;
using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Biome
{
	public NativeArray<Noiser> noisers;
	public NativeArray<Tile> tiles;
	[HideInInspector] public int width;
	[HideInInspector] public int height;
	[HideInInspector] public NativeArray<byte> diffusionMap; //TODO: Change the heightmap to an index map, allowing the tile Color property to be removed.
}