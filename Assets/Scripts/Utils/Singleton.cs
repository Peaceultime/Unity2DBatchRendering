public abstract class Singleton<T> where T : new()
{
	public static T Instance
	{
        get
		{
			if (defined == false)
			{
                T t = new();
                internal_instance = t;
				defined = true;
			}
			return internal_instance;
		}
		internal set { }
	}
	internal static T internal_instance;
	internal static bool defined;
}