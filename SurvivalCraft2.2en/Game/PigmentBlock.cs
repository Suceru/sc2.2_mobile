using Engine;
using Engine.Graphics;

namespace Game
{
	public class PigmentBlock : FlatBlock
	{
		public const int Index = 130;

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int value2 = Terrain.ExtractData(value);
			BlocksManager.DrawFlatBlock(primitivesRenderer, value, size, ref matrix, null, color * SubsystemPalette.GetColor(environmentData, value2), isEmissive: false, environmentData);
		}
	}
}
