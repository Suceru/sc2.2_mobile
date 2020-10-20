using Engine;
using Engine.Graphics;

namespace Game
{
	public class GrassTrapBlock : Block
	{
		public const int Index = 87;

		public BlockMesh m_blockMesh = new BlockMesh();

		public BlockMesh m_standaloneBlockMesh = new BlockMesh();

		public BoundingBox[] m_collisionBoxes = new BoundingBox[1];

		public override void Initialize()
		{
			Model model = ContentManager.Get<Model>("Models/GrassTrap");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("GrassTrap").ParentBone);
			Color color = BlockColorsMap.GrassColorsMap.Lookup(8, 15);
			m_blockMesh.AppendModelMeshPart(model.FindMesh("GrassTrap").MeshParts[0], boneAbsoluteTransform * Matrix.CreateTranslation(0.5f, 0.75f, 0.5f), makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
			m_standaloneBlockMesh.AppendModelMeshPart(model.FindMesh("GrassTrap").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, color);
			m_collisionBoxes[0] = new BoundingBox(new Vector3(0f, 0.75f, 0f), new Vector3(1f, 0.95f, 1f));
			base.Initialize();
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
			generator.GenerateShadedMeshVertices(this, x, y, z, m_blockMesh, BlockColorsMap.GrassColorsMap.Lookup(generator.Terrain, x, y, z), null, null, geometry.SubsetAlphaTest);
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			BlocksManager.DrawMeshBlock(primitivesRenderer, m_standaloneBlockMesh, color, size, ref matrix, environmentData);
		}

		public override BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			Color color = BlockColorsMap.GrassColorsMap.Lookup(subsystemTerrain.Terrain, Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, color, GetFaceTextureSlot(4, value));
		}

		public override BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_collisionBoxes;
		}
	}
}
