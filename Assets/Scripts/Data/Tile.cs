[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Tile
{
	public static readonly Tile Null = new Tile { baseSprite = -1, decoSprite = -1 };

	public int baseSprite; //Index in texture array.
	public int decoSprite; //If there is no deco, this index must be = -1.
}