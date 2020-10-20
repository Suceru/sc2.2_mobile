using Engine;

namespace Game
{
	public struct TouchInput
	{
		public TouchInputType InputType;

		public Vector2 Position;

		public Vector2 Move;

		public Vector2 TotalMove;

		public Vector2 TotalMoveLimited;

		public float Duration;

		public int DurationFrames;
	}
}
