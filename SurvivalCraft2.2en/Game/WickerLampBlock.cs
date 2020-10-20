namespace Game
{
	public class WickerLampBlock : AlphaTestCubeBlock
	{
		public const int Index = 17;

		public override int GetFaceTextureSlot(int face, int value)
		{
			if (face != 5)
			{
				return DefaultTextureSlot;
			}
			return 4;
		}
	}
}
