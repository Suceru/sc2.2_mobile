using Engine;

namespace Game
{
	public class CookedMeatBlock : FoodBlock
	{
		public const int Index = 89;

		public CookedMeatBlock()
			: base("Models/Meat", Matrix.Identity, new Color(155, 122, 51), 240)
		{
		}
	}
}
