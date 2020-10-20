using Engine;

namespace Game
{
	public class RawMeatBlock : FoodBlock
	{
		public const int Index = 88;

		public RawMeatBlock()
			: base("Models/Meat", Matrix.Identity, Color.White, 240)
		{
		}
	}
}
