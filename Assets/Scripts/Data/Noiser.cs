using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
[System.Serializable]
public struct Noiser
{
	public enum NoiseType { SIMPLEX, PERLIN };
	public enum NoiseShape { CLASSIC, RIDGID };

	public NoiseType type;
	public NoiseShape shape;

	public bool active;

	public float amplitude;
	public float frequency;
	public float addition;

	[MinMaxSlider(-1, 1)]
	public float2 clamp;
}

[System.Serializable]
public struct NoiseSampler
{
	public NativeArray<Noiser> noisers;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float2 get_noise(int q, int r, float2 randomness)
    {
        float height = 0, moisture = 0;
        int count = 0;
        for (int i = 0; i < noisers.Length; ++i)
        {
            if (noisers[i].active)
            {
                Noiser noiser = noisers[i];
                height += compute_noise(ref noiser, new float3(q, r, randomness.x));
                moisture += compute_noise(ref noiser, new float3(q, r, randomness.y));
                count++;
            }
        }
        return new float2(math.clamp(height, 0, 1), math.clamp(moisture, 0, 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float classic_noise(ref Noiser noiser, float3 pos)
    {
        return noiser.type == Noiser.NoiseType.PERLIN ? noise.cnoise(pos) : noise.snoise(pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ridgid_noise(ref Noiser noiser, float3 pos)
    {
        return 1f - math.abs(classic_noise(ref noiser, pos));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float compute_noise(ref Noiser noiser, float3 pos)
    {
        float value = 0;
        switch (noiser.shape)
        {
            case Noiser.NoiseShape.CLASSIC:
                value = (classic_noise(ref noiser, noiser.frequency * pos) + 1) * 0.5f * noiser.amplitude;
                break;
            case Noiser.NoiseShape.RIDGID:
                float v = ridgid_noise(ref noiser, noiser.frequency * pos);
                v *= v;
                value = v * noiser.amplitude;
                break;
        }
        value += noiser.addition;

        if (value < noiser.clamp.x)
            value = 0;

        if (value > noiser.clamp.y)
            value = 0;

        return value;
    }

    public void Dispose()
    {
        noisers.Dispose();
    }
}