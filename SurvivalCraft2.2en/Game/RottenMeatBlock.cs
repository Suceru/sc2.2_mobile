using Engine;

namespace Game
{
	public class RottenMeatBlock : FoodBlock
	{
		public const int Index = 240;

		public RottenMeatBlock()
			: base("Models/Meat", Matrix.CreateTranslation(-0.0625f, 0f, 0f), Color.White, FoodBlock.m_compostValue)
		{
		}
	}
}
