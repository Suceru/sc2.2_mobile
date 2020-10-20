using Engine;

namespace Game
{
	public class IronFenceBlock : FenceBlock
	{
		public const int Index = 193;

		public IronFenceBlock()
			: base("Models/IronFence", doubleSidedPlanks: true, useAlphaTest: true, 58, new Color(192, 192, 192), new Color(80, 80, 80))
		{
		}
	}
}
