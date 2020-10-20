using Engine;

namespace Game
{
	public class Pickable : WorldItem
	{
		public int Count;

		public Vector3? FlyToPosition;

		public Matrix? StuckMatrix;

		public bool SplashGenerated = true;
	}
}
