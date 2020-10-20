using Engine;

namespace Game
{
	public class CookedBirdBlock : FoodBlock
	{
		public const int Index = 78;

		public CookedBirdBlock()
			: base("Models/Bird", Matrix.Identity, new Color(150, 69, 15), 239)
		{
		}
	}
}
