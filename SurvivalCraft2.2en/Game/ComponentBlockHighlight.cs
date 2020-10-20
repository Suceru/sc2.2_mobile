using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBlockHighlight : Component, IDrawable, IUpdateable
	{
		public class Geometry : TerrainGeometry
		{
			public Geometry()
			{
				TerrainGeometrySubset terrainGeometrySubset = new TerrainGeometrySubset();
				TerrainGeometrySubset[] array = new TerrainGeometrySubset[6]
				{
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset,
					terrainGeometrySubset
				};
				SubsetOpaque = terrainGeometrySubset;
				SubsetAlphaTest = terrainGeometrySubset;
				SubsetTransparent = terrainGeometrySubset;
				OpaqueSubsetsByFace = array;
				AlphaTestSubsetsByFace = array;
				TransparentSubsetsByFace = array;
			}
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemAnimatedTextures m_subsystemAnimatedTextures;

		public SubsystemSky m_subsystemSky;

		public ComponentPlayer m_componentPlayer;

		public PrimitivesRenderer3D m_primitivesRenderer3D = new PrimitivesRenderer3D();

		public Shader m_shader;

		public Geometry m_geometry;

		public CellFace m_cellFace;

		public int m_value;

		public object m_highlightRaycastResult;

		public static int[] m_drawOrders = new int[2]
		{
			1,
			2000
		};

		public Point3? NearbyEditableCell
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.BlockHighlight;

		public int[] DrawOrders => m_drawOrders;

		public void Update(float dt)
		{
			Camera activeCamera = m_componentPlayer.GameWidget.ActiveCamera;
			Ray3? ray = m_componentPlayer.ComponentInput.IsControlledByVr ? m_componentPlayer.ComponentInput.CalculateVrHandRay() : new Ray3?(new Ray3(activeCamera.ViewPosition, activeCamera.ViewDirection));
			NearbyEditableCell = null;
			if (ray.HasValue)
			{
				m_highlightRaycastResult = m_componentPlayer.ComponentMiner.Raycast(ray.Value, RaycastMode.Digging);
				if (!(m_highlightRaycastResult is TerrainRaycastResult))
				{
					return;
				}
				TerrainRaycastResult terrainRaycastResult = (TerrainRaycastResult)m_highlightRaycastResult;
				if (terrainRaycastResult.Distance < 3f)
				{
					Point3 point = terrainRaycastResult.CellFace.Point;
					int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
					Block obj = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
					if (obj is CrossBlock)
					{
						terrainRaycastResult.Distance = MathUtils.Max(terrainRaycastResult.Distance, 0.1f);
						m_highlightRaycastResult = terrainRaycastResult;
					}
					if (obj.IsEditable)
					{
						NearbyEditableCell = terrainRaycastResult.CellFace.Point;
					}
				}
			}
			else
			{
				m_highlightRaycastResult = null;
			}
		}

		public void Draw(Camera camera, int drawOrder)
		{
			if (camera.GameWidget.PlayerData == m_componentPlayer.PlayerData)
			{
				if (drawOrder == m_drawOrders[0])
				{
					DrawFillHighlight(camera);
					DrawOutlineHighlight(camera);
					DrawReticleHighlight(camera);
				}
				else
				{
					DrawRayHighlight(camera);
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemAnimatedTextures = base.Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			m_shader = ContentManager.Get<Shader>("Shaders/Highlight");
		}

		public void DrawRayHighlight(Camera camera)
		{
			if (!camera.Eye.HasValue)
			{
				return;
			}
			Ray3 ray = default(Ray3);
			float num;
			if (m_highlightRaycastResult is TerrainRaycastResult)
			{
				TerrainRaycastResult obj = (TerrainRaycastResult)m_highlightRaycastResult;
				ray = obj.Ray;
				num = MathUtils.Min(obj.Distance, 2f);
			}
			else if (m_highlightRaycastResult is BodyRaycastResult)
			{
				BodyRaycastResult obj2 = (BodyRaycastResult)m_highlightRaycastResult;
				ray = obj2.Ray;
				num = MathUtils.Min(obj2.Distance, 2f);
			}
			else if (m_highlightRaycastResult is MovingBlocksRaycastResult)
			{
				MovingBlocksRaycastResult obj3 = (MovingBlocksRaycastResult)m_highlightRaycastResult;
				ray = obj3.Ray;
				num = MathUtils.Min(obj3.Distance, 2f);
			}
			else
			{
				if (!(m_highlightRaycastResult is Ray3))
				{
					return;
				}
				ray = (Ray3)m_highlightRaycastResult;
				num = 2f;
			}
			Color color = Color.White * 0.5f;
			Color color2 = Color.Lerp(color, Color.Transparent, MathUtils.Saturate(num / 2f));
			FlatBatch3D flatBatch3D = m_primitivesRenderer3D.FlatBatch();
			flatBatch3D.QueueLine(ray.Position, ray.Position + ray.Direction * num, color, color2);
			flatBatch3D.Flush(camera.ViewProjectionMatrix);
		}

		public void DrawReticleHighlight(Camera camera)
		{
			if (camera.Eye.HasValue && m_highlightRaycastResult is TerrainRaycastResult)
			{
				TerrainRaycastResult terrainRaycastResult = (TerrainRaycastResult)m_highlightRaycastResult;
				Vector3 vector = terrainRaycastResult.HitPoint();
				Vector3 vector2 = (!(BlocksManager.Blocks[Terrain.ExtractContents(terrainRaycastResult.Value)] is CrossBlock)) ? CellFace.FaceToVector3(terrainRaycastResult.CellFace.Face) : (-terrainRaycastResult.Ray.Direction);
				float num = Vector3.Distance(camera.ViewPosition, vector);
				float s = 0.03f + MathUtils.Min(0.008f * num, 0.04f);
				float s2 = 0.01f * num;
				Vector3 v = (MathUtils.Abs(Vector3.Dot(vector2, Vector3.UnitY)) < 0.5f) ? Vector3.UnitY : Vector3.UnitX;
				Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, v));
				Vector3 v2 = Vector3.Normalize(Vector3.Cross(vector2, vector3));
				Subtexture subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Reticle");
				TexturedBatch3D texturedBatch3D = m_primitivesRenderer3D.TexturedBatch(subtexture.Texture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, null, SamplerState.LinearClamp);
				Vector3 p = vector + s * (-vector3 + v2) + s2 * vector2;
				Vector3 p2 = vector + s * (vector3 + v2) + s2 * vector2;
				Vector3 p3 = vector + s * (vector3 - v2) + s2 * vector2;
				Vector3 p4 = vector + s * (-vector3 - v2) + s2 * vector2;
				Vector2 texCoord = new Vector2(subtexture.TopLeft.X, subtexture.TopLeft.Y);
				Vector2 texCoord2 = new Vector2(subtexture.BottomRight.X, subtexture.TopLeft.Y);
				Vector2 texCoord3 = new Vector2(subtexture.BottomRight.X, subtexture.BottomRight.Y);
				Vector2 texCoord4 = new Vector2(subtexture.TopLeft.X, subtexture.BottomRight.Y);
				texturedBatch3D.QueueQuad(p, p2, p3, p4, texCoord, texCoord2, texCoord3, texCoord4, Color.White);
				texturedBatch3D.Flush(camera.ViewProjectionMatrix);
			}
		}

		public void DrawFillHighlight(Camera camera)
		{
			if (camera.Eye.HasValue && m_highlightRaycastResult is TerrainRaycastResult)
			{
				CellFace cellFace = ((TerrainRaycastResult)m_highlightRaycastResult).CellFace;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				int num = Terrain.ExtractContents(cellValue);
				Block block = BlocksManager.Blocks[num];
				if (m_geometry == null || cellValue != m_value || cellFace != m_cellFace)
				{
					m_geometry = new Geometry();
					block.GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, m_geometry, cellValue, cellFace.X, cellFace.Y, cellFace.Z);
					m_cellFace = cellFace;
					m_value = cellValue;
				}
				DynamicArray<TerrainVertex> vertices = m_geometry.SubsetOpaque.Vertices;
				DynamicArray<ushort> indices = m_geometry.SubsetOpaque.Indices;
				float x = m_subsystemSky.ViewFogRange.X;
				float y = m_subsystemSky.ViewFogRange.Y;
				Vector3 viewPosition = camera.ViewPosition;
				Vector3 v = new Vector3(MathUtils.Floor(viewPosition.X), 0f, MathUtils.Floor(viewPosition.Z));
				Matrix value = Matrix.CreateTranslation(v - viewPosition) * camera.ViewMatrix.OrientationMatrix * camera.ProjectionMatrix;
				Display.BlendState = BlendState.NonPremultiplied;
				Display.DepthStencilState = DepthStencilState.Default;
				Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
				m_shader.GetParameter("u_origin").SetValue(v.XZ);
				m_shader.GetParameter("u_viewProjectionMatrix").SetValue(value);
				m_shader.GetParameter("u_viewPosition").SetValue(viewPosition);
				m_shader.GetParameter("u_texture").SetValue(m_subsystemAnimatedTextures.AnimatedBlocksTexture);
				m_shader.GetParameter("u_samplerState").SetValue(SamplerState.PointWrap);
				m_shader.GetParameter("u_fogColor").SetValue(new Vector3(m_subsystemSky.ViewFogColor));
				m_shader.GetParameter("u_fogStartInvLength").SetValue(new Vector2(x, 1f / (y - x)));
				Display.DrawUserIndexed(PrimitiveType.TriangleList, m_shader, TerrainVertex.VertexDeclaration, vertices.Array, 0, vertices.Count, indices.Array, 0, indices.Count);
			}
		}

		public void DrawOutlineHighlight(Camera camera)
		{
			if (camera.UsesMovementControls || !(m_componentPlayer.ComponentHealth.Health > 0f) || !m_componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible)
			{
				return;
			}
			if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue)
			{
				CellFace value = m_componentPlayer.ComponentMiner.DigCellFace.Value;
				BoundingBox cellFaceBoundingBox = GetCellFaceBoundingBox(value.Point);
				DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), value.Face, cellFaceBoundingBox.Min, cellFaceBoundingBox.Max, Color.Black);
			}
			else
			{
				if (!m_componentPlayer.ComponentAimingSights.IsSightsVisible && (SettingsManager.LookControlMode == LookControlMode.SplitTouch || !m_componentPlayer.ComponentInput.IsControlledByTouch) && m_highlightRaycastResult is TerrainRaycastResult)
				{
					CellFace cellFace = ((TerrainRaycastResult)m_highlightRaycastResult).CellFace;
					BoundingBox cellFaceBoundingBox2 = GetCellFaceBoundingBox(cellFace.Point);
					DrawBoundingBoxFace(m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None), cellFace.Face, cellFaceBoundingBox2.Min, cellFaceBoundingBox2.Max, Color.Black);
				}
				if (NearbyEditableCell.HasValue)
				{
					BoundingBox cellFaceBoundingBox3 = GetCellFaceBoundingBox(NearbyEditableCell.Value);
					m_primitivesRenderer3D.FlatBatch(0, DepthStencilState.None).QueueBoundingBox(cellFaceBoundingBox3, Color.Black);
				}
			}
			m_primitivesRenderer3D.Flush(camera.ViewProjectionMatrix);
		}

		public static void DrawBoundingBoxFace(FlatBatch3D batch, int face, Vector3 c1, Vector3 c2, Color color)
		{
			switch (face)
			{
			case 0:
				batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
				break;
			case 1:
				batch.QueueLine(new Vector3(c2.X, c1.Y, c2.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
				break;
			case 2:
				batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c2.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
				break;
			case 3:
				batch.QueueLine(new Vector3(c1.X, c2.Y, c2.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c1.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
				break;
			case 4:
				batch.QueueLine(new Vector3(c2.X, c2.Y, c2.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c1.X, c2.Y, c1.Z), new Vector3(c1.X, c2.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c2.Y, c1.Z), new Vector3(c2.X, c2.Y, c2.Z), color);
				break;
			case 5:
				batch.QueueLine(new Vector3(c1.X, c1.Y, c2.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c1.Z), color);
				batch.QueueLine(new Vector3(c1.X, c1.Y, c1.Z), new Vector3(c1.X, c1.Y, c2.Z), color);
				batch.QueueLine(new Vector3(c2.X, c1.Y, c1.Z), new Vector3(c2.X, c1.Y, c2.Z), color);
				break;
			}
		}

		public BoundingBox GetCellFaceBoundingBox(Point3 point)
		{
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z);
			BoundingBox[] customCollisionBoxes = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)].GetCustomCollisionBoxes(m_subsystemTerrain, cellValue);
			Vector3 vector = new Vector3(point.X, point.Y, point.Z);
			if (customCollisionBoxes.Length != 0)
			{
				BoundingBox? boundingBox = null;
				for (int i = 0; i < customCollisionBoxes.Length; i++)
				{
					if (customCollisionBoxes[i] != default(BoundingBox))
					{
						boundingBox = (boundingBox.HasValue ? BoundingBox.Union(boundingBox.Value, customCollisionBoxes[i]) : customCollisionBoxes[i]);
					}
				}
				if (!boundingBox.HasValue)
				{
					boundingBox = new BoundingBox(Vector3.Zero, Vector3.One);
				}
				return new BoundingBox(boundingBox.Value.Min + vector, boundingBox.Value.Max + vector);
			}
			return new BoundingBox(vector, vector + Vector3.One);
		}
	}
}
