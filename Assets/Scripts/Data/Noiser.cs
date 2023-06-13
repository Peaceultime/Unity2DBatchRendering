using Unity.Mathematics;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
[System.Serializable]
public struct Noiser
{
	public enum NoiseType { SIMPLEX, PERLIN };
	public enum NoiseShape { CLASSIC, RIDGID };

	public NoiseType noise_type;
	public NoiseShape noise_shape;

	public byte active;

	public uint octaves;
	public float amplitude;
	public float frequency;
	public float roughness;
	public float weight;

	public float addition;
	public float multiply;
}