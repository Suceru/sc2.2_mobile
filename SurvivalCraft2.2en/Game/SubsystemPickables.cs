using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemPickables : Subsystem, IDrawable, IUpdateable
	{
		public SubsystemAudio m_subsystemAudio;

		public SubsystemPlayers m_subsystemPlayers;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemExplosions m_subsystemExplosions;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

		public List<ComponentPlayer> m_tmpPlayers = new List<ComponentPlayer>();

		public List<Pickable> m_pickables = new List<Pickable>();

		public List<Pickable> m_pickablesToRemove = new List<Pickable>();

		public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

		public Random m_random = new Random();

		public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();

		public static int[] m_drawOrders = new int[1]
		{
			10
		};

		public ReadOnlyList<Pickable> Pickables => new ReadOnlyList<Pickable>(m_pickables);

		public int[] DrawOrders => m_drawOrders;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public event Action<Pickable> PickableAdded;

		public event Action<Pickable> PickableRemoved;

		public Pickable AddPickable(int value, int count, Vector3 position, Vector3? velocity, Matrix? stuckMatrix)
		{
			Pickable pickable = new Pickable();
			pickable.Value = value;
			pickable.Count = count;
			pickable.Position = position;
			pickable.StuckMatrix = stuckMatrix;
			pickable.CreationTime = m_subsystemGameInfo.TotalElapsedGameTime;
			if (velocity.HasValue)
			{
				pickable.Velocity = velocity.Value;
			}
			else if (Terrain.ExtractContents(value) == 248)
			{
				Vector2 vector = m_random.Vector2(1.5f, 2f);
				pickable.Velocity = new Vector3(vector.X, 3f, vector.Y);
			}
			else
			{
				pickable.Velocity = new Vector3(m_random.Float(-0.5f, 0.5f), m_random.Float(1f, 1.2f), m_random.Float(-0.5f, 0.5f));
			}
			m_pickables.Add(pickable);
			if (this.PickableAdded != null)
			{
				this.PickableAdded(pickable);
			}
			return pickable;
		}

		public void Draw(Camera camera, int drawOrder)
		{
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
			Matrix matrix = Matrix.CreateRotationY((float)MathUtils.Remainder(totalElapsedGameTime, 6.2831854820251465));
			float num = MathUtils.Min(m_subsystemSky.VisibilityRange, 30f);
			foreach (Pickable pickable in m_pickables)
			{
				Vector3 position = pickable.Position;
				float num2 = Vector3.Dot(camera.ViewDirection, position - camera.ViewPosition);
				if (num2 > -0.5f && num2 < num)
				{
					int num3 = Terrain.ExtractContents(pickable.Value);
					Block block = BlocksManager.Blocks[num3];
					float num4 = (float)(totalElapsedGameTime - pickable.CreationTime);
					if (!pickable.StuckMatrix.HasValue)
					{
						position.Y += 0.25f * MathUtils.Saturate(3f * num4);
					}
					int x = Terrain.ToCell(position.X);
					int num5 = Terrain.ToCell(position.Y);
					int z = Terrain.ToCell(position.Z);
					TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
					if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidVertices1 && num5 >= 0 && num5 < 255)
					{
						m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
						m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(num5);
						float f = MathUtils.Max(position.Y - (float)num5 - 0.75f, 0f) / 0.25f;
						int num6 = pickable.Light = (int)MathUtils.Lerp(m_subsystemTerrain.Terrain.GetCellLightFast(x, num5, z), m_subsystemTerrain.Terrain.GetCellLightFast(x, num5 + 1, z), f);
					}
					m_drawBlockEnvironmentData.Light = pickable.Light;
					m_drawBlockEnvironmentData.BillboardDirection = pickable.Position - camera.ViewPosition;
					m_drawBlockEnvironmentData.InWorldMatrix.Translation = position;
					if (pickable.StuckMatrix.HasValue)
					{
						Matrix matrix2 = pickable.StuckMatrix.Value;
						block.DrawBlock(m_primitivesRenderer, pickable.Value, Color.White, 0.3f, ref matrix2, m_drawBlockEnvironmentData);
					}
					else
					{
						matrix.Translation = position + new Vector3(0f, 0.04f * MathUtils.Sin(3f * num4), 0f);
						block.DrawBlock(m_primitivesRenderer, pickable.Value, Color.White, 0.3f, ref matrix, m_drawBlockEnvironmentData);
					}
				}
			}
			m_primitivesRenderer.Flush(camera.ViewProjectionMatrix);
		}

		public void Update(float dt)
		{
			double totalElapsedGameTime = m_subsystemGameInfo.TotalElapsedGameTime;
			float num = MathUtils.Pow(0.5f, dt);
			float num2 = MathUtils.Pow(0.001f, dt);
			m_tmpPlayers.Clear();
			foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
			{
				if (componentPlayer.ComponentHealth.Health > 0f)
				{
					m_tmpPlayers.Add(componentPlayer);
				}
			}
			foreach (Pickable pickable in m_pickables)
			{
				if (pickable.ToRemove)
				{
					m_pickablesToRemove.Add(pickable);
				}
				else
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
					int num3 = m_pickables.Count - m_pickablesToRemove.Count;
					float num4 = MathUtils.Lerp(300f, 90f, MathUtils.Saturate((float)num3 / 60f));
					double num5 = totalElapsedGameTime - pickable.CreationTime;
					if (num5 > (double)num4)
					{
						pickable.ToRemove = true;
					}
					else
					{
						TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Z));
						if (chunkAtCell != null && chunkAtCell.State > TerrainChunkState.InvalidContents4)
						{
							Vector3 position = pickable.Position;
							Vector3 vector = position + pickable.Velocity * dt;
							if (!pickable.FlyToPosition.HasValue && num5 > 0.5)
							{
								foreach (ComponentPlayer tmpPlayer in m_tmpPlayers)
								{
									ComponentBody componentBody = tmpPlayer.ComponentBody;
									Vector3 v = componentBody.Position + new Vector3(0f, 0.75f, 0f);
									float num6 = (v - pickable.Position).LengthSquared();
									if (num6 < 3.0625f)
									{
										bool flag = Terrain.ExtractContents(pickable.Value) == 248;
										IInventory inventory = tmpPlayer.ComponentMiner.Inventory;
										if (flag || ComponentInventoryBase.FindAcquireSlotForItem(inventory, pickable.Value) >= 0)
										{
											if (num6 < 1f)
											{
												if (flag)
												{
													tmpPlayer.ComponentLevel.AddExperience(pickable.Count, playSound: true);
													pickable.ToRemove = true;
												}
												else
												{
													pickable.Count = ComponentInventoryBase.AcquireItems(inventory, pickable.Value, pickable.Count);
													if (pickable.Count == 0)
													{
														pickable.ToRemove = true;
														m_subsystemAudio.PlaySound("Audio/PickableCollected", 0.7f, -0.4f, pickable.Position, 2f, autoDelay: false);
													}
												}
											}
											else if (!pickable.StuckMatrix.HasValue)
											{
												pickable.FlyToPosition = v + 0.1f * MathUtils.Sqrt(num6) * componentBody.Velocity;
											}
										}
									}
								}
							}
							if (pickable.FlyToPosition.HasValue)
							{
								Vector3 v2 = pickable.FlyToPosition.Value - pickable.Position;
								float num7 = v2.LengthSquared();
								if (num7 >= 0.25f)
								{
									pickable.Velocity = 6f * v2 / MathUtils.Sqrt(num7);
								}
								else
								{
									pickable.FlyToPosition = null;
								}
							}
							else
							{
								FluidBlock surfaceBlock;
								float? surfaceHeight;
								Vector2? vector2 = m_subsystemFluidBlockBehavior.CalculateFlowSpeed(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z), out surfaceBlock, out surfaceHeight);
								if (!pickable.StuckMatrix.HasValue)
								{
									TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
									if (terrainRaycastResult.HasValue)
									{
										int contents = Terrain.ExtractContents(m_subsystemTerrain.Terrain.GetCellValue(terrainRaycastResult.Value.CellFace.X, terrainRaycastResult.Value.CellFace.Y, terrainRaycastResult.Value.CellFace.Z));
										SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents);
										for (int i = 0; i < blockBehaviors.Length; i++)
										{
											blockBehaviors[i].OnHitByProjectile(terrainRaycastResult.Value.CellFace, pickable);
										}
										if (m_subsystemTerrain.Raycast(position, position, useInteractionBoxes: false, skipAirBlocks: true, (int value2, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value2)].IsCollidable).HasValue)
										{
											int num8 = Terrain.ToCell(position.X);
											int num9 = Terrain.ToCell(position.Y);
											int num10 = Terrain.ToCell(position.Z);
											int num11 = 0;
											int num12 = 0;
											int num13 = 0;
											int? num14 = null;
											for (int j = -3; j <= 3; j++)
											{
												for (int k = -3; k <= 3; k++)
												{
													for (int l = -3; l <= 3; l++)
													{
														if (!BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(j + num8, k + num9, l + num10)].IsCollidable)
														{
															int num15 = j * j + k * k + l * l;
															if (!num14.HasValue || num15 < num14.Value)
															{
																num11 = j + num8;
																num12 = k + num9;
																num13 = l + num10;
																num14 = num15;
															}
														}
													}
												}
											}
											if (num14.HasValue)
											{
												pickable.FlyToPosition = new Vector3(num11, num12, num13) + new Vector3(0.5f);
											}
											else
											{
												pickable.ToRemove = true;
											}
										}
										else
										{
											Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
											bool flag2 = vector2.HasValue && vector2.Value != Vector2.Zero;
											if (plane.Normal.X != 0f)
											{
												float num16 = (flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.Y) + MathUtils.Sqr(pickable.Velocity.Z)) > 10f) ? 0.95f : 0.25f;
												pickable.Velocity *= new Vector3(0f - num16, num16, num16);
											}
											if (plane.Normal.Y != 0f)
											{
												float num17 = (flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.X) + MathUtils.Sqr(pickable.Velocity.Z)) > 10f) ? 0.95f : 0.25f;
												pickable.Velocity *= new Vector3(num17, 0f - num17, num17);
												if (flag2)
												{
													pickable.Velocity.Y += 0.1f * plane.Normal.Y;
												}
											}
											if (plane.Normal.Z != 0f)
											{
												float num18 = (flag2 || MathUtils.Sqrt(MathUtils.Sqr(pickable.Velocity.X) + MathUtils.Sqr(pickable.Velocity.Y)) > 10f) ? 0.95f : 0.25f;
												pickable.Velocity *= new Vector3(num18, num18, 0f - num18);
											}
											vector = position;
										}
									}
								}
								else
								{
									Vector3 vector3 = pickable.StuckMatrix.Value.Translation + pickable.StuckMatrix.Value.Up * block.ProjectileTipOffset;
									if (!m_subsystemTerrain.Raycast(vector3, vector3, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable).HasValue)
									{
										pickable.Position = pickable.StuckMatrix.Value.Translation;
										pickable.Velocity = Vector3.Zero;
										pickable.StuckMatrix = null;
									}
								}
								if (surfaceBlock is WaterBlock && !pickable.SplashGenerated)
								{
									m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, pickable.Position, large: false));
									m_subsystemAudio.PlayRandomSound("Audio/Splashes", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 6f, autoDelay: true);
									pickable.SplashGenerated = true;
								}
								else if (surfaceBlock is MagmaBlock && !pickable.SplashGenerated)
								{
									m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, pickable.Position, large: false));
									m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 3f, autoDelay: true);
									pickable.ToRemove = true;
									pickable.SplashGenerated = true;
									m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
								}
								else if (surfaceBlock == null)
								{
									pickable.SplashGenerated = false;
								}
								if (m_subsystemTime.PeriodicGameTimeEvent(1.0, (double)(pickable.GetHashCode() % 100) / 100.0) && (m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z)) == 104 || m_subsystemFireBlockBehavior.IsCellOnFire(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y + 0.1f), Terrain.ToCell(pickable.Position.Z))))
								{
									m_subsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.2f, 0.2f), pickable.Position, 3f, autoDelay: true);
									pickable.ToRemove = true;
									m_subsystemExplosions.TryExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
								}
								if (!pickable.StuckMatrix.HasValue)
								{
									if (vector2.HasValue && surfaceHeight.HasValue)
									{
										float num19 = surfaceHeight.Value - pickable.Position.Y;
										float num20 = MathUtils.Saturate(3f * num19);
										pickable.Velocity.X += 4f * dt * (vector2.Value.X - pickable.Velocity.X);
										pickable.Velocity.Y -= 10f * dt;
										pickable.Velocity.Y += 10f * (1f / block.Density * num20) * dt;
										pickable.Velocity.Z += 4f * dt * (vector2.Value.Y - pickable.Velocity.Z);
										pickable.Velocity.Y *= num2;
									}
									else
									{
										pickable.Velocity.Y -= 10f * dt;
										pickable.Velocity *= num;
									}
								}
							}
							pickable.Position = vector;
						}
					}
				}
			}
			foreach (Pickable item in m_pickablesToRemove)
			{
				m_pickables.Remove(item);
				if (this.PickableRemoved != null)
				{
					this.PickableRemoved(item);
				}
			}
			m_pickablesToRemove.Clear();
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemPlayers = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemExplosions = base.Project.FindSubsystem<SubsystemExplosions>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_subsystemFireBlockBehavior = base.Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			m_subsystemFluidBlockBehavior = base.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			foreach (ValuesDictionary item in valuesDictionary.GetValue<ValuesDictionary>("Pickables").Values.Where((object v) => v is ValuesDictionary))
			{
				Pickable pickable = new Pickable();
				pickable.Value = item.GetValue<int>("Value");
				pickable.Count = item.GetValue<int>("Count");
				pickable.Position = item.GetValue<Vector3>("Position");
				pickable.Velocity = item.GetValue<Vector3>("Velocity");
				pickable.CreationTime = item.GetValue("CreationTime", 0.0);
				if (item.ContainsKey("StuckMatrix"))
				{
					pickable.StuckMatrix = item.GetValue<Matrix>("StuckMatrix");
				}
				m_pickables.Add(pickable);
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Pickables", valuesDictionary2);
			int num = 0;
			foreach (Pickable pickable in m_pickables)
			{
				ValuesDictionary valuesDictionary3 = new ValuesDictionary();
				valuesDictionary2.SetValue(num.ToString(), valuesDictionary3);
				valuesDictionary3.SetValue("Value", pickable.Value);
				valuesDictionary3.SetValue("Count", pickable.Count);
				valuesDictionary3.SetValue("Position", pickable.Position);
				valuesDictionary3.SetValue("Velocity", pickable.Velocity);
				valuesDictionary3.SetValue("CreationTime", pickable.CreationTime);
				if (pickable.StuckMatrix.HasValue)
				{
					valuesDictionary3.SetValue("StuckMatrix", pickable.StuckMatrix.Value);
				}
				num++;
			}
		}
	}
}
