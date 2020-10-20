using Engine;
using Engine.Graphics;

namespace Game
{
	public class FlourBlock : FlatBlock
	{
		public const int Index = 175;

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * new Color(248, 255, 232), isEmissive: false, environmentData);
		}
	}
}
