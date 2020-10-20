namespace Game
{
	public class CraftingRecipe
	{
		public const int MaxSize = 3;

		public int ResultValue;

		public int ResultCount;

		public int RemainsValue;

		public int RemainsCount;

		public float RequiredHeatLevel;

		public float RequiredPlayerLevel;

		public string[] Ingredients = new string[9];

		public string Description;

		public string Message;
	}
}
