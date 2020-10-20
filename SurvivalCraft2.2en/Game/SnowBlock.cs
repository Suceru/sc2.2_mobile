using Engine;
using System.Collections.Generic;

namespace Game
{
	public class SnowBlock : CubeBlock
	{
		public const int Index = 61;

		public BoundingBox[] m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.125f, 1f))
		};

		public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
		{
			return face != 5;
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateCubeVertices(this, value, x, y, z, 0.125f, 0.125f, 0.125f, 0.125f, Color.White, Color.White, Color.White, Color.White, Color.White, -1, geometry.OpaqueSubsetsByFace);
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			showDebris = true;
			if (toolLevel >= RequiredToolLevel)
			{
				int num = Random.Int(1, 3);
				for (int i = 0; i < num; i++)
				{
					dropValues.Add(new BlockDropValue
					{
						Value = Terrain.MakeBlockValue(85),
						Count = 1
					});
				}
			}
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}
	}
}
