using Engine;
using Engine.Graphics;

namespace Game
{
	public class CottonWadBlock : FlatBlock
	{
		public const int Index = 205;

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color, isEmissive: false, environmentData);
		}
	}
}
