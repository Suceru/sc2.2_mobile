using Engine;

namespace Game
{
	public class RawBirdBlock : FoodBlock
	{
		public const int Index = 77;

		public RawBirdBlock()
			: base("Models/Bird", Matrix.Identity, new Color(224, 170, 164), 239)
		{
		}
	}
}
