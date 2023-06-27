using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct AnimatedSprite
{
	public int start, range;
	public float interval, progress;
}

public static class AnimatingSystem
{
	public static JobHandle Update(ref NativeArray<Sprite> sprites, ref NativeArray<AnimatedSprite> animations)
	{
        return (new AnimatorJob
        {
            sprites = sprites,
            animations = animations,
            time = UnityEngine.Time.deltaTime,
        }).ScheduleParallel(sprites.Length, Constants.GRANULARITY, default);
	}

    [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, OptimizeFor = OptimizeFor.Performance)]
    private struct AnimatorJob : IJobFor
	{
		public NativeArray<Sprite> sprites;
		public NativeArray<AnimatedSprite> animations;

        [ReadOnly] public float time;

        public void Execute(int i)
        {
            AnimatedSprite animatedSprite = animations[i];
            Sprite sprite = sprites[i];

            if (animatedSprite.range == 0 || sprite.index == -1)
                return;

            animatedSprite.progress += time;
            if (animations[i].progress > animations[i].interval)
            {
                sprite.index += (int) math.floor(animations[i].progress / animations[i].interval);
                animatedSprite.progress %= animations[i].interval;
                if (sprites[i].index >= animations[i].start + animations[i].range - 1)
                    sprite.index = animations[i].start;
            }
            animations[i] = animatedSprite;
            sprites[i] = sprite;
        }
    }
}