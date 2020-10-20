using Engine;
using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
	public abstract class LeavesBlock : AlphaTestCubeBlock
	{
		public BlockColorsMap m_blockColorsMap;

		public Random m_random = new Random();

		protected LeavesBlock(BlockColorsMap blockColorsMap)
		{
			m_blockColorsMap = blockColorsMap;
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			Color color = m_blockColorsMap.Lookup(generator.Terrain, x, y, z);
			generator.GenerateCubeVertices(this, value, x, y, z, color, geometry.AlphaTestSubsetsByFace);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			color *= m_blockColorsMap.Lookup(environmentData.Temperature, environmentData.Humidity);
			BlocksManager.DrawCubeBlock(primitivesRenderer, value, new Vector3(size), ref matrix, color, color, environmentData);
		}

		public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			Color color = m_blockColorsMap.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			if (m_random.Bool(0.15f))
			{
				dropValues.Add(new BlockDropValue
				{
					Value = 23,
					Count = 1
				});
				showDebris = true;
			}
			else
			{
				base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
			}
		}
	}
}
