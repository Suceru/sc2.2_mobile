using Engine;
using Engine.Graphics;
using System;

namespace Game
{
	public class SwitchBlock : MountedElectricElementBlock
	{
		public const int Index = 141;

		public BlockMesh m_standaloneBlockMesh = new BlockMesh();

		public BlockMesh[] m_blockMeshesByData = new BlockMesh[12];

		public BoundingBox[][] m_collisionBoxesByData = new BoundingBox[12][];

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/Switch");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Body").ParentBone);
			Matrix boneAbsoluteTransform2 = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Lever").ParentBone);
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					int num = (i << 1) | j;
					Matrix m = (i >= 4) ? ((i != 4) ? (Matrix.CreateRotationX((float)Math.PI) * Matrix.CreateTranslation(0.5f, 1f, 0.5f)) : Matrix.CreateTranslation(0.5f, 0f, 0.5f)) : (Matrix.CreateRotationX((float)Math.PI / 2f) * Matrix.CreateTranslation(0f, 0f, -0.5f) * Matrix.CreateRotationY((float)i * (float)Math.PI / 2f) * Matrix.CreateTranslation(0.5f, 0.5f, 0.5f));
					Matrix m2 = Matrix.CreateRotationX((j == 0) ? MathUtils.DegToRad(30f) : MathUtils.DegToRad(-30f));
					m_blockMeshesByData[num] = new BlockMesh();
					m_blockMeshesByData[num].AppendModelMeshPart(model.FindMesh("Body").MeshParts[0], boneAbsoluteTransform * m, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_blockMeshesByData[num].AppendModelMeshPart(model.FindMesh("Lever").MeshParts[0], boneAbsoluteTransform2 * m2 * m, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_collisionBoxesByData[num] = new BoundingBox[1]
					{
						m_blockMeshesByData[num].CalculateBoundingBox()
					};
				}
			}
			Matrix m3 = Matrix.CreateRotationY(-(float)Math.PI / 2f) * Matrix.CreateRotationZ((float)Math.PI / 2f);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Body").MeshParts[0], boneAbsoluteTransform * m3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("Lever").MeshParts[0], boneAbsoluteTransform2 * m3, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
		}

		public static bool GetLeverState(int value)
		{
			return (Terrain.ExtractData(value) & 1) != 0;
		}

		public static int SetLeverState(int value, bool state)
		{
			return Terrain.ReplaceData(value, state ? (Terrain.ExtractData(value) | 1) : (Terrain.ExtractData(value) & -2));
		}

		public override int GetFace(int value)
		{
			return (Terrain.ExtractData(value) >> 1) & 7;
		}

		public override BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default(BlockPlacementData);
			result.Value = Terrain.ReplaceData(value, raycastResult.CellFace.Face << 1);
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			int num = Terrain.ExtractData(value);
			if (num >= m_collisionBoxesByData.Length)
			{
				return null;
			}
			return m_collisionBoxesByData[num];
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			int num = Terrain.ExtractData(value);
			if (num < m_blockMeshesByData.Length)
			{
				generator.GenerateMeshVertices(this, x, y, z, m_blockMeshesByData[num], Color.White, null, geometry.SubsetOpaque);
				generator.GenerateWireVertices(value, x, y, z, GetFace(value), 0.25f, Vector2.Zero, geometry.SubsetOpaque);
			}
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, 2f * size, ref matrix, environmentData);
		}

		public override ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z)
		{
			return new SwitchElectricElement(subsystemElectricity, new CellFace(x, y, z, GetFace(value)), value);
		}

		public override ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z)
		{
			int face2 = GetFace(value);
			if (face == face2 && SubsystemElectricity.GetConnectorDirection(face2, 0, connectorFace).HasValue)
			{
				return ElectricConnectorType.Output;
			}
			return null;
		}
	}
}
