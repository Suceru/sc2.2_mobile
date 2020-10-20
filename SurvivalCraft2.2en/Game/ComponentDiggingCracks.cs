using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentDiggingCracks : Component, IDrawable
	{
		public class Geometry : TerrainGeometry
		{
			public Geometry()
			{
				TerrainGeometrySubset terrainGeometrySubset = SubsetTransparent = (SubsetAlphaTest = (SubsetOpaque = new TerrainGeometrySubset()));
				OpaqueSubsetsByFace = new TerrainGeometrySubset[6]
				{
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset
				};
				AlphaTestSubsetsByFace = new TerrainGeometrySubset[6]
				{
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset
				};
				TransparentSubsetsByFace = new TerrainGeometrySubset[6]
				{
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset
				};
			}
		}

		public struct CracksVertex
		{
			public float X;

			public float Y;

			public float Z;

			public float Tx;

			public float Ty;

			public Color Color;

			public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate), new VertexElement(20, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public ComponentMiner m_componentMiner;

		public Texture2D[] m_textures;

		public Shader m_shader;

		public Geometry m_geometry;

		public DynamicArray<CracksVertex> m_vertices = new DynamicArray<CracksVertex>();

		public Point3 m_point;

		public int m_value;

		public static int[] m_drawOrders = new int[1]
		{
			1
		};

		public int[] DrawOrders => m_drawOrders;

		public void Draw(Camera camera, int drawOrder)
		{
			if (!m_componentMiner.DigCellFace.HasValue || !(m_componentMiner.DigProgress > 0f) || !(m_componentMiner.DigTime > 0.2f))
			{
				return;
			}
			Point3 point = m_componentMiner.DigCellFace.Value.Point;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			int num = Terrain.ExtractContents(cellValue);
			Block block = BlocksManager.Blocks[num];
			if (m_geometry == null || cellValue != m_value || point != m_point)
			{
				m_geometry = new Geometry();
				block.GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, m_geometry, cellValue, point.X, point.Y, point.Z);
				m_point = point;
				m_value = cellValue;
				m_vertices.Clear();
				CracksVertex item = default(CracksVertex);
				for (int i = 0; i < m_geometry.SubsetOpaque.Vertices.Count; i++)
				{
					TerrainVertex terrainVertex = m_geometry.SubsetOpaque.Vertices.Array[i];
					byte b = (byte)((terrainVertex.Color.R + terrainVertex.Color.G + terrainVertex.Color.B) / 3);
					item.X = terrainVertex.X;
					item.Y = terrainVertex.Y;
					item.Z = terrainVertex.Z;
					item.Tx = (float)terrainVertex.Tx / 32767f * 16f;
					item.Ty = (float)terrainVertex.Ty / 32767f * 16f;
					item.Color = new Color(b, b, b, (byte)128);
					m_vertices.Add(item);
				}
			}
			Vector3 viewPosition = camera.ViewPosition;
			Vector3 v = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
			Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
			DynamicArray<ushort> indices = m_geometry.SubsetOpaque.Indices;
			float x = m_subsystemSky.ViewFogRange.X;
			float y = m_subsystemSky.ViewFogRange.Y;
			int num2 = MathUtils.Clamp((int)(m_componentMiner.DigProgress * 8f), 0, 7);
			Display.BlendState = BlendState.NonPremultiplied;
			Display.DepthStencilState = DepthStencilState.Default;
			Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
			m_shader.GetParameter("u_origin").SetValue(v.XZ);
			m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
			m_shader.GetParameter("u_viewPosition").SetValue(camera.ViewPosition);
			m_shader.GetParameter("u_texture").SetValue(m_textures[num2]);
			m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointWrap);
			m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
			m_shader.GetParameter("u_fogStartInvLength").SetValue(new Vector2(x, 1f / (y - x)));
			Display.DrawUserIndexed(PrimitiveType.TriangleList, m_shader, CracksVertex.VertexDeclaration, m_vertices.Array, 0, m_vertices.Count, indices.Array, 0, indices.Count);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_componentMiner = base.Entity.FindComponent<ComponentMiner>(throwOnError: true);
			m_shader = ContentManager.Get<Shader>("Shaders/AlphaTested");
			m_textures = new Texture2D[8];
			for (int i = 0; i < 8; i++)
			{
				m_textures[i] = ContentManager.Get<Texture2D>($"Textures/Cracks{i + 1}");
			}
		}
	}
}
