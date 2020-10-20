using Engine;

namespace Game
{
	public class StoneFenceBlock : FenceBlock
	{
		public const int Index = 202;

		public StoneFenceBlock()
			: base("Models/StoneFence", doubleSidedPlanks: false, useAlphaTest: false, 24, new Color(212, 212, 212), Color.White)
		{
		}

		public override bool ShouldConnectTo(int value)
		{
			int num = Terrain.ExtractContents(value);
			if (BlocksManager.Blocks[num].IsTransparent)
			{
				return base.ShouldConnectTo(value);
			}
			return true;
		}
	}
}
