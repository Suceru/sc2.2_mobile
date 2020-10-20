using Engine;
using Engine.Graphics;
using System.Collections.Generic;

namespace Game
{
	public class SoilBlock : CubeBlock
	{
		public const int Index = 168;

		public static BoundingBox[] m_collisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(new Vector3(0f, 0f, 0f), new Vector3(1f, 0.9375f, 1f))
		};

		public override int GetFaceTextureSlot(int face, int value)
		{
			int nitrogen = GetNitrogen(Terrain.ExtractData(value));
			if (face != 4)
			{
				return 2;
			}
			if (nitrogen <= 0)
			{
				return 37;
			}
			return 53;
		}

		public static bool GetHydration(int data)
		{
			return (data & 1) != 0;
		}

		public static int GetNitrogen(int data)
		{
			return (data >> 1) & 3;
		}

		public static int SetHydration(int data, bool hydration)
		{
			if (!hydration)
			{
				return data & -2;
			}
			return data | 1;
		}

		public static int SetNitrogen(int data, int nitrogen)
		{
			nitrogen = MathUtils.Clamp(nitrogen, 0, 3);
			return (data & -7) | ((nitrogen & 3) << 1);
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, hydration: false));
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(0, hydration: true));
			yield return Terrain.MakeBlockValue(BlockIndex, 0, SetHydration(SetNitrogen(0, 3), hydration: true));
		}

		public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			int data = Terrain.ExtractData(value);
			int nitrogen = GetNitrogen(data);
			bool hydration = GetHydration(data);
			if (nitrogen > 0 && hydration)
			{
				return "Moist Compost";
			}
			if (nitrogen > 0)
			{
				return "Compost";
			}
			if (hydration)
			{
				return "Moist soil";
			}
			return "Soil";
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			Color color = GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White;
			generator.GenerateCubeVertices(this, value, x, y, z, 0.9375f, 0.9375f, 0.9375f, 0.9375f, color, color, color, color, color, -1, geometry.OpaqueSubsetsByFace);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			Color c = GetHydration(Terrain.ExtractData(value)) ? new Color(180, 170, 150) : Color.White;
			base.DrawBlock(primitivesRenderer, value, color * c, size, ref matrix, environmentData);
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}

		public override bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
		{
			return face != 5;
		}
	}
}
