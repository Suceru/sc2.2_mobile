using Engine;

namespace Game
{
	public class BreadBlock : FoodBlock
	{
		public const int Index = 177;

		public BreadBlock()
			: base("Models/Bread", Matrix.Identity, Color.White, 242)
		{
		}
	}
}
