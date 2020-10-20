using Engine;

namespace Game
{
	public class BasaltFenceBlock : FenceBlock
	{
		public const int Index = 163;

		public BasaltFenceBlock()
			: base("Models/StoneFence", doubleSidedPlanks: false, useAlphaTest: false, 40, new Color(212, 212, 212), Color.White)
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
