using Engine;

namespace Game
{
	public class WaterBlock : FluidBlock
	{
		public const int Index = 18;

		public new static int MaxLevel = 7;

		public WaterBlock()
			: base(MaxLevel)
		{
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			Color sideColor;
			Color color = sideColor = BlockColorsMap.WaterColorsMap.Lookup(generator.Terrain, x, y, z);
			sideColor.A = 255;
			Color topColor = color;
			topColor.A = 0;
			GenerateFluidTerrainVertices(generator, value, x, y, z, sideColor, topColor, geometry.TransparentSubsetsByFace);
		}
	}
}
