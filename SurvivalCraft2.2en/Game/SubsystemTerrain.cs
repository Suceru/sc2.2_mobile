using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemTerrain : Subsystem, IDrawable, IUpdateable
	{
		public static bool TerrainRenderingEnabled = true;

		public Dictionary<Point3, bool> m_modifiedCells = new Dictionary<Point3, bool>();

		public DynamicArray<Point3> m_modifiedList = new DynamicArray<Point3>();

		public static Point3[] m_neighborOffsets = new Point3[7]
		{
			new Point3(0, 0, 0),
			new Point3(-1, 0, 0),
			new Point3(1, 0, 0),
			new Point3(0, -1, 0),
			new Point3(0, 1, 0),
			new Point3(0, 0, -1),
			new Point3(0, 0, 1)
		};

		public SubsystemGameWidgets m_subsystemViews;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public List<BlockDropValue> m_dropValues = new List<BlockDropValue>();

		public static int[] m_drawOrders = new int[2]
		{
			0,
			100
		};

		public SubsystemGameInfo SubsystemGameInfo
		{
			get;
			set;
		}

		public SubsystemAnimatedTextures SubsystemAnimatedTextures
		{
			get;
			set;
		}

		public SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior
		{
			get;
			set;
		}

		public SubsystemPalette SubsystemPalette
		{
			get;
			set;
		}

		public Terrain Terrain
		{
			get;
			set;
		}

		public TerrainUpdater TerrainUpdater
		{
			get;
			set;
		}

		public TerrainRenderer TerrainRenderer
		{
			get;
			set;
		}

		public TerrainSerializer22 TerrainSerializer
		{
			get;
			set;
		}

		public ITerrainContentsGenerator TerrainContentsGenerator
		{
			get;
			set;
		}

		public BlockGeometryGenerator BlockGeometryGenerator
		{
			get;
			set;
		}

		public int[] DrawOrders => m_drawOrders;

		public UpdateOrder UpdateOrder => UpdateOrder.Terrain;

		public void ProcessModifiedCells()
		{
			m_modifiedList.Clear();
			foreach (Point3 key in m_modifiedCells.Keys)
			{
				m_modifiedList.Add(key);
			}
			m_modifiedCells.Clear();
			for (int i = 0; i < m_modifiedList.Count; i++)
			{
				Point3 point = m_modifiedList.Array[i];
				for (int j = 0; j < m_neighborOffsets.Length; j++)
				{
					Point3 point2 = m_neighborOffsets[j];
					int cellContents = Terrain.GetCellContents(point.X + point2.X, point.Y + point2.Y, point.Z + point2.Z);
					SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(cellContents);
					for (int k = 0; k < blockBehaviors.Length; k++)
					{
						blockBehaviors[k].OnNeighborBlockChanged(point.X + point2.X, point.Y + point2.Y, point.Z + point2.Z, point.X, point.Y, point.Z);
					}
				}
			}
		}

		public TerrainRaycastResult? Raycast(Vector3 start, Vector3 end, bool useInteractionBoxes, bool skipAirBlocks, Func<int, float, bool> action)
		{
			float num = Vector3.Distance(start, end);
			if (num > 1000f)
			{
				Log.Warning("Terrain raycast too long, trimming.");
				end = start + 1000f * Vector3.Normalize(end - start);
			}
			Ray3 ray = new Ray3(start, Vector3.Normalize(end - start));
			float x = start.X;
			float y = start.Y;
			float z = start.Z;
			float x2 = end.X;
			float y2 = end.Y;
			float z2 = end.Z;
			int num2 = Terrain.ToCell(x);
			int num3 = Terrain.ToCell(y);
			int num4 = Terrain.ToCell(z);
			int num5 = Terrain.ToCell(x2);
			int num6 = Terrain.ToCell(y2);
			int num7 = Terrain.ToCell(z2);
			int num8 = (x < x2) ? 1 : ((x > x2) ? (-1) : 0);
			int num9 = (y < y2) ? 1 : ((y > y2) ? (-1) : 0);
			int num10 = (z < z2) ? 1 : ((z > z2) ? (-1) : 0);
			float num11 = MathUtils.Floor(x);
			float num12 = num11 + 1f;
			float num13 = ((x > x2) ? (x - num11) : (num12 - x)) / Math.Abs(x2 - x);
			float num14 = MathUtils.Floor(y);
			float num15 = num14 + 1f;
			float num16 = ((y > y2) ? (y - num14) : (num15 - y)) / Math.Abs(y2 - y);
			float num17 = MathUtils.Floor(z);
			float num18 = num17 + 1f;
			float num19 = ((z > z2) ? (z - num17) : (num18 - z)) / Math.Abs(z2 - z);
			float num20 = 1f / Math.Abs(x2 - x);
			float num21 = 1f / Math.Abs(y2 - y);
			float num22 = 1f / Math.Abs(z2 - z);
			while (true)
			{
				BoundingBox boundingBox = default(BoundingBox);
				int collisionBoxIndex = 0;
				float? num23 = null;
				int cellValue = Terrain.GetCellValue(num2, num3, num4);
				int num24 = Terrain.ExtractContents(cellValue);
				if (num24 != 0 || !skipAirBlocks)
				{
					Ray3 ray2 = new Ray3(ray.Position - new Vector3(num2, num3, num4), ray.Direction);
					int nearestBoxIndex;
					BoundingBox nearestBox;
					float? num25 = BlocksManager.Blocks[num24].Raycast(ray2, this, cellValue, useInteractionBoxes, out nearestBoxIndex, out nearestBox);
					if (num25.HasValue && (!num23.HasValue || num25.Value < num23.Value))
					{
						num23 = num25;
						collisionBoxIndex = nearestBoxIndex;
						boundingBox = nearestBox;
					}
				}
				if (num23.HasValue && num23.Value <= num && (action == null || action(cellValue, num23.Value)))
				{
					int face = 0;
					Vector3 vector = start - new Vector3(num2, num3, num4) + num23.Value * ray.Direction;
					float num26 = 3.40282347E+38f;
					float num27 = MathUtils.Abs(vector.X - boundingBox.Min.X);
					if (num27 < num26)
					{
						num26 = num27;
						face = 3;
					}
					num27 = MathUtils.Abs(vector.X - boundingBox.Max.X);
					if (num27 < num26)
					{
						num26 = num27;
						face = 1;
					}
					num27 = MathUtils.Abs(vector.Y - boundingBox.Min.Y);
					if (num27 < num26)
					{
						num26 = num27;
						face = 5;
					}
					num27 = MathUtils.Abs(vector.Y - boundingBox.Max.Y);
					if (num27 < num26)
					{
						num26 = num27;
						face = 4;
					}
					num27 = MathUtils.Abs(vector.Z - boundingBox.Min.Z);
					if (num27 < num26)
					{
						num26 = num27;
						face = 2;
					}
					num27 = MathUtils.Abs(vector.Z - boundingBox.Max.Z);
					if (num27 < num26)
					{
						num26 = num27;
						face = 0;
					}
					TerrainRaycastResult value = default(TerrainRaycastResult);
					value.Ray = ray;
					value.Value = cellValue;
					value.CellFace = new CellFace
					{
						X = num2,
						Y = num3,
						Z = num4,
						Face = face
					};
					value.CollisionBoxIndex = collisionBoxIndex;
					value.Distance = num23.Value;
					return value;
				}
				if (num13 <= num16 && num13 <= num19)
				{
					if (num2 == num5)
					{
						break;
					}
					num13 += num20;
					num2 += num8;
				}
				else if (num16 <= num13 && num16 <= num19)
				{
					if (num3 == num6)
					{
						break;
					}
					num16 += num21;
					num3 += num9;
				}
				else
				{
					if (num4 == num7)
					{
						break;
					}
					num19 += num22;
					num4 += num10;
				}
			}
			return null;
		}

		public void ChangeCell(int x, int y, int z, int value, bool updateModificationCounter = true)
		{
			if (!Terrain.IsCellValid(x, y, z))
			{
				return;
			}
			int cellValueFast = Terrain.GetCellValueFast(x, y, z);
			value = Terrain.ReplaceLight(value, 0);
			cellValueFast = Terrain.ReplaceLight(cellValueFast, 0);
			if (value == cellValueFast)
			{
				return;
			}
			Terrain.SetCellValueFast(x, y, z, value);
			TerrainChunk chunkAtCell = Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null)
			{
				if (updateModificationCounter)
				{
					chunkAtCell.ModificationCounter++;
				}
				TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
			}
			m_modifiedCells[new Point3(x, y, z)] = true;
			int num = Terrain.ExtractContents(cellValueFast);
			int num2 = Terrain.ExtractContents(value);
			if (num2 != num)
			{
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					blockBehaviors[i].OnBlockRemoved(cellValueFast, value, x, y, z);
				}
				SubsystemBlockBehavior[] blockBehaviors2 = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int j = 0; j < blockBehaviors2.Length; j++)
				{
					blockBehaviors2[j].OnBlockAdded(value, cellValueFast, x, y, z);
				}
			}
			else
			{
				SubsystemBlockBehavior[] blockBehaviors3 = m_subsystemBlockBehaviors.GetBlockBehaviors(num2);
				for (int k = 0; k < blockBehaviors3.Length; k++)
				{
					blockBehaviors3[k].OnBlockModified(value, cellValueFast, x, y, z);
				}
			}
		}

		public void DestroyCell(int toolLevel, int x, int y, int z, int newValue, bool noDrop, bool noParticleSystem)
		{
			int cellValue = Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			Block block = BlocksManager.Blocks[num];
			if (num != 0)
			{
				bool showDebris = true;
				if (!noDrop)
				{
					m_dropValues.Clear();
					block.GetDropValues(this, cellValue, newValue, toolLevel, m_dropValues, out showDebris);
					for (int i = 0; i < m_dropValues.Count; i++)
					{
						BlockDropValue dropValue = m_dropValues[i];
						if (dropValue.Count > 0)
						{
							SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(dropValue.Value));
							for (int j = 0; j < blockBehaviors.Length; j++)
							{
								blockBehaviors[j].OnItemHarvested(x, y, z, cellValue, ref dropValue, ref newValue);
							}
							if (dropValue.Count > 0 && Terrain.ExtractContents(dropValue.Value) != 0)
							{
								Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
								m_subsystemPickables.AddPickable(dropValue.Value, dropValue.Count, position, null, null);
							}
						}
					}
				}
				if (showDebris && !noParticleSystem && m_subsystemViews.CalculateDistanceFromNearestView(new Vector3(x, y, z)) < 16f)
				{
					m_subsystemParticles.AddParticleSystem(block.CreateDebrisParticleSystem(this, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), cellValue, 1f));
				}
			}
			ChangeCell(x, y, z, newValue);
		}

		public void Draw(Camera camera, int drawOrder)
		{
			if (TerrainRenderingEnabled)
			{
				if (drawOrder == m_drawOrders[0])
				{
					TerrainUpdater.PrepareForDrawing(camera);
					TerrainRenderer.PrepareForDrawing(camera);
					TerrainRenderer.DrawOpaque(camera);
					TerrainRenderer.DrawAlphaTested(camera);
				}
				else if (drawOrder == m_drawOrders[1])
				{
					TerrainRenderer.DrawTransparent(camera);
				}
			}
		}

		public void Update(float dt)
		{
			TerrainUpdater.Update();
			ProcessModifiedCells();
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemViews = base.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
			SubsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			SubsystemAnimatedTextures = base.Project.FindSubsystem<SubsystemAnimatedTextures>(throwOnError: true);
			SubsystemFurnitureBlockBehavior = base.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
			SubsystemPalette = base.Project.FindSubsystem<SubsystemPalette>(throwOnError: true);
			Terrain = new Terrain();
			TerrainRenderer = new TerrainRenderer(this);
			TerrainUpdater = new TerrainUpdater(this);
			TerrainSerializer = new TerrainSerializer22(Terrain, SubsystemGameInfo.DirectoryName);
			BlockGeometryGenerator = new BlockGeometryGenerator(Terrain, this, base.Project.FindSubsystem<SubsystemElectricity>(throwOnError: true), SubsystemFurnitureBlockBehavior, base.Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true), SubsystemPalette);
			if (string.CompareOrdinal(SubsystemGameInfo.WorldSettings.OriginalSerializationVersion, "2.1") <= 0)
			{
				TerrainGenerationMode terrainGenerationMode = SubsystemGameInfo.WorldSettings.TerrainGenerationMode;
				if (terrainGenerationMode == TerrainGenerationMode.FlatContinent || terrainGenerationMode == TerrainGenerationMode.FlatIsland)
				{
					TerrainContentsGenerator = new TerrainContentsGeneratorFlat(this);
				}
				else
				{
					TerrainContentsGenerator = new TerrainContentsGenerator21(this);
				}
			}
			else
			{
				TerrainGenerationMode terrainGenerationMode2 = SubsystemGameInfo.WorldSettings.TerrainGenerationMode;
				if (terrainGenerationMode2 == TerrainGenerationMode.FlatContinent || terrainGenerationMode2 == TerrainGenerationMode.FlatIsland)
				{
					TerrainContentsGenerator = new TerrainContentsGeneratorFlat(this);
				}
				else
				{
					TerrainContentsGenerator = new TerrainContentsGenerator22(this);
				}
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			TerrainUpdater.UpdateEvent.WaitOne();
			try
			{
				TerrainChunk[] allocatedChunks = Terrain.AllocatedChunks;
				foreach (TerrainChunk chunk in allocatedChunks)
				{
					TerrainSerializer.SaveChunk(chunk);
				}
			}
			finally
			{
				TerrainUpdater.UpdateEvent.Set();
			}
		}

		public override void Dispose()
		{
			TerrainRenderer.Dispose();
			TerrainUpdater.Dispose();
			TerrainSerializer.Dispose();
			Terrain.Dispose();
		}
	}
}
