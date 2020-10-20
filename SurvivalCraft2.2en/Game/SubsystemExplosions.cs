using Engine;
using GameEntitySystem;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemExplosions : Subsystem, IUpdateable
	{
		public class SparseSpatialArray<T>
		{
			public const int m_bits1 = 4;

			public const int m_bits2 = 4;

			public const int m_mask1 = 15;

			public const int m_mask2 = 15;

			public const int m_diameter = 256;

			public int m_originX;

			public int m_originY;

			public int m_originZ;

			public T[][] m_data;

			public T m_outside;

			public SparseSpatialArray(int centerX, int centerY, int centerZ, T outside)
			{
				m_data = new T[4096][];
				m_originX = centerX - 128;
				m_originY = centerY - 128;
				m_originZ = centerZ - 128;
				m_outside = outside;
			}

			public T Get(int x, int y, int z)
			{
				x -= m_originX;
				y -= m_originY;
				z -= m_originZ;
				if (x >= 0 && x < 256 && y >= 0 && y < 256 && z >= 0 && z < 256)
				{
					int num = x >> 4;
					int num2 = y >> 4;
					int num3 = z >> 4;
					int num4 = num + (num2 << 4) + (num3 << 4 << 4);
					T[] array = m_data[num4];
					if (array != null)
					{
						int num5 = x & 0xF;
						int num6 = y & 0xF;
						int num7 = z & 0xF;
						int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
						return array[num8];
					}
					return default(T);
				}
				return m_outside;
			}

			public void Set(int x, int y, int z, T value)
			{
				x -= m_originX;
				y -= m_originY;
				z -= m_originZ;
				if (x >= 0 && x < 256 && y >= 0 && y < 256 && z >= 0 && z < 256)
				{
					int num = x >> 4;
					int num2 = y >> 4;
					int num3 = z >> 4;
					int num4 = num + (num2 << 4) + (num3 << 4 << 4);
					T[] array = m_data[num4];
					if (array == null)
					{
						array = new T[4096];
						m_data[num4] = array;
					}
					int num5 = x & 0xF;
					int num6 = y & 0xF;
					int num7 = z & 0xF;
					int num8 = num5 + (num6 << 4) + (num7 << 4 << 4);
					array[num8] = value;
				}
			}

			public void Clear()
			{
				for (int i = 0; i < m_data.Length; i++)
				{
					m_data[i] = null;
				}
			}

			public Dictionary<Point3, T> ToDictionary()
			{
				Dictionary<Point3, T> dictionary = new Dictionary<Point3, T>();
				for (int i = 0; i < m_data.Length; i++)
				{
					T[] array = m_data[i];
					if (array == null)
					{
						continue;
					}
					int num = m_originX + ((i & 0xF) << 4);
					int num2 = m_originY + (((i >> 4) & 0xF) << 4);
					int num3 = m_originZ + (((i >> 8) & 0xF) << 4);
					for (int j = 0; j < array.Length; j++)
					{
						if (!object.Equals(array[j], default(T)))
						{
							int num4 = j & 0xF;
							int num5 = (j >> 4) & 0xF;
							int num6 = (j >> 8) & 0xF;
							dictionary.Add(new Point3(num + num4, num2 + num5, num3 + num6), array[j]);
						}
					}
				}
				return dictionary;
			}
		}

		public struct ExplosionData
		{
			public int X;

			public int Y;

			public int Z;

			public float Pressure;

			public bool IsIncendiary;

			public bool NoExplosionSound;
		}

		public struct ProcessPoint
		{
			public int X;

			public int Y;

			public int Z;

			public int Axis;
		}

		public struct SurroundingPressurePoint
		{
			public float Pressure;

			public bool IsIncendiary;
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemNoise m_subsystemNoise;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemProjectiles m_subsystemProjectiles;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public List<ExplosionData> m_queuedExplosions = new List<ExplosionData>();

		public SparseSpatialArray<float> m_pressureByPoint;

		public SparseSpatialArray<SurroundingPressurePoint> m_surroundingPressureByPoint;

		public int m_projectilesCount;

		public Dictionary<Projectile, bool> m_generatedProjectiles = new Dictionary<Projectile, bool>();

		public Random m_random = new Random();

		public ExplosionParticleSystem m_explosionParticleSystem;

		public bool ShowExplosionPressure;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public bool TryExplodeBlock(int x, int y, int z, int value)
		{
			int num = Terrain.ExtractContents(value);
			Block obj = BlocksManager.Blocks[num];
			float explosionPressure = obj.GetExplosionPressure(value);
			bool explosionIncendiary = obj.GetExplosionIncendiary(value);
			if (explosionPressure > 0f)
			{
				AddExplosion(x, y, z, explosionPressure, explosionIncendiary, noExplosionSound: false);
				return true;
			}
			return false;
		}

		public void AddExplosion(int x, int y, int z, float pressure, bool isIncendiary, bool noExplosionSound)
		{
			if (pressure > 0f)
			{
				m_queuedExplosions.Add(new ExplosionData
				{
					X = x,
					Y = y,
					Z = z,
					Pressure = pressure,
					IsIncendiary = isIncendiary,
					NoExplosionSound = noExplosionSound
				});
			}
		}

		public void Update(float dt)
		{
			if (m_queuedExplosions.Count <= 0)
			{
				return;
			}
			int x = m_queuedExplosions[0].X;
			int y = m_queuedExplosions[0].Y;
			int z = m_queuedExplosions[0].Z;
			m_pressureByPoint = new SparseSpatialArray<float>(x, y, z, 0f);
			m_surroundingPressureByPoint = new SparseSpatialArray<SurroundingPressurePoint>(x, y, z, new SurroundingPressurePoint
			{
				IsIncendiary = false,
				Pressure = 0f
			});
			m_projectilesCount = 0;
			m_generatedProjectiles.Clear();
			bool flag = false;
			int num = 0;
			while (num < m_queuedExplosions.Count)
			{
				ExplosionData explosionData = m_queuedExplosions[num];
				if (MathUtils.Abs(explosionData.X - x) <= 4 && MathUtils.Abs(explosionData.Y - y) <= 4 && MathUtils.Abs(explosionData.Z - z) <= 4)
				{
					m_queuedExplosions.RemoveAt(num);
					SimulateExplosion(explosionData.X, explosionData.Y, explosionData.Z, explosionData.Pressure, explosionData.IsIncendiary);
					flag |= !explosionData.NoExplosionSound;
				}
				else
				{
					num++;
				}
			}
			PostprocessExplosions(flag);
			if (!ShowExplosionPressure)
			{
				m_pressureByPoint = null;
				m_surroundingPressureByPoint = null;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemNoise = base.Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_subsystemProjectiles = base.Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_subsystemFireBlockBehavior = base.Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			m_explosionParticleSystem = new ExplosionParticleSystem();
			m_subsystemParticles.AddParticleSystem(m_explosionParticleSystem);
		}

		public void SimulateExplosion(int x, int y, int z, float pressure, bool isIncendiary)
		{
			float num = MathUtils.Max(0.13f * MathUtils.Pow(pressure, 0.5f), 1f);
			m_subsystemTerrain.ChangeCell(x, y, z, Terrain.MakeBlockValue(0));
			SparseSpatialArray<bool> processed = new SparseSpatialArray<bool>(x, y, z, outside: true);
			List<ProcessPoint> list = new List<ProcessPoint>();
			List<ProcessPoint> list2 = new List<ProcessPoint>();
			List<ProcessPoint> list3 = new List<ProcessPoint>();
			TryAddPoint(x, y, z, -1, pressure, isIncendiary, list, processed);
			int num2 = 0;
			int num3 = 0;
			while (list.Count > 0 || list2.Count > 0)
			{
				num2 += list.Count;
				num3++;
				float num4 = 5f * (float)MathUtils.Max(num3 - 7, 0);
				float num5 = pressure / (MathUtils.Pow(num2, 0.66f) + num4);
				if (num5 >= num)
				{
					foreach (ProcessPoint item in list)
					{
						float num6 = m_pressureByPoint.Get(item.X, item.Y, item.Z);
						float num7 = num5 + num6;
						m_pressureByPoint.Set(item.X, item.Y, item.Z, num7);
						if (item.Axis == 0)
						{
							TryAddPoint(item.X - 1, item.Y, item.Z, 0, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X + 1, item.Y, item.Z, 0, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y - 1, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y + 1, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y, item.Z - 1, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y, item.Z + 1, -1, num7, isIncendiary, list2, processed);
						}
						else if (item.Axis == 1)
						{
							TryAddPoint(item.X - 1, item.Y, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X + 1, item.Y, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y - 1, item.Z, 1, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y + 1, item.Z, 1, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y, item.Z - 1, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y, item.Z + 1, -1, num7, isIncendiary, list2, processed);
						}
						else if (item.Axis == 2)
						{
							TryAddPoint(item.X - 1, item.Y, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X + 1, item.Y, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y - 1, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y + 1, item.Z, -1, num7, isIncendiary, list2, processed);
							TryAddPoint(item.X, item.Y, item.Z - 1, 2, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y, item.Z + 1, 2, num7, isIncendiary, list3, processed);
						}
						else
						{
							TryAddPoint(item.X - 1, item.Y, item.Z, 0, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X + 1, item.Y, item.Z, 0, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y - 1, item.Z, 1, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y + 1, item.Z, 1, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y, item.Z - 1, 2, num7, isIncendiary, list3, processed);
							TryAddPoint(item.X, item.Y, item.Z + 1, 2, num7, isIncendiary, list3, processed);
						}
					}
				}
				List<ProcessPoint> list4 = list;
				list4.Clear();
				list = list2;
				list2 = list3;
				list3 = list4;
			}
		}

		public void TryAddPoint(int x, int y, int z, int axis, float currentPressure, bool isIncendiary, List<ProcessPoint> toProcess, SparseSpatialArray<bool> processed)
		{
			if (processed.Get(x, y, z))
			{
				return;
			}
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			if (num != 0)
			{
				int num2 = (int)(MathUtils.Hash((uint)(x + 913 * y + 217546 * z)) % 100u);
				float num3 = MathUtils.Lerp(1f, 2f, (float)num2 / 100f);
				if (num2 % 8 == 0)
				{
					num3 *= 3f;
				}
				Block block = BlocksManager.Blocks[num];
				float num4 = m_pressureByPoint.Get(x - 1, y, z) + m_pressureByPoint.Get(x + 1, y, z) + m_pressureByPoint.Get(x, y - 1, z) + m_pressureByPoint.Get(x, y + 1, z) + m_pressureByPoint.Get(x, y, z - 1) + m_pressureByPoint.Get(x, y, z + 1);
				float num5 = MathUtils.Max(block.ExplosionResilience * num3, 1f);
				float num6 = num4 / num5;
				if (num6 > 1f)
				{
					int newValue = Terrain.MakeBlockValue(0);
					m_subsystemTerrain.DestroyCell(0, x, y, z, newValue, noDrop: true, noParticleSystem: true);
					bool flag = false;
					float probability = (num6 > 5f) ? 0.95f : 0.75f;
					if (m_random.Bool(probability))
					{
						flag = TryExplodeBlock(x, y, z, cellValue);
					}
					if (!flag)
					{
						CalculateImpulseAndDamage(new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), 60f, 2f * num4, out Vector3 impulse, out float _);
						bool flag2 = false;
						List<BlockDropValue> list = new List<BlockDropValue>();
						block.GetDropValues(m_subsystemTerrain, cellValue, newValue, 0, list, out bool _);
						if (list.Count == 0)
						{
							list.Add(new BlockDropValue
							{
								Value = cellValue,
								Count = 1
							});
							flag2 = true;
						}
						foreach (BlockDropValue item in list)
						{
							int num7 = Terrain.ExtractContents(item.Value);
							if (!(BlocksManager.Blocks[num7] is FluidBlock))
							{
								float num8 = (m_projectilesCount < 40) ? 1f : ((m_projectilesCount < 60) ? 0.5f : ((m_projectilesCount >= 80) ? 0.125f : 0.25f));
								if (m_random.Float(0f, 1f) < num8)
								{
									Vector3 velocity = impulse + m_random.Vector3(0.05f * impulse.Length());
									if (m_projectilesCount >= 1)
									{
										velocity *= m_random.Float(0.5f, 1f);
										velocity += m_random.Vector3(0.2f * velocity.Length());
									}
									float num9 = flag2 ? 0f : MathUtils.Lerp(1f, 0f, (float)m_projectilesCount / 20f);
									Projectile projectile = m_subsystemProjectiles.AddProjectile(item.Value, new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f), velocity, m_random.Vector3(0f, 20f), null);
									projectile.ProjectileStoppedAction = ((!(m_random.Float(0f, 1f) < num9)) ? ProjectileStoppedAction.Disappear : ProjectileStoppedAction.TurnIntoPickable);
									if (m_random.Float(0f, 1f) < 0.5f && m_projectilesCount < 35)
									{
										float num10 = (num4 > 60f) ? m_random.Float(3f, 7f) : m_random.Float(1f, 3f);
										if (isIncendiary)
										{
											num10 += 10f;
										}
										m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new SmokeTrailParticleSystem(15, m_random.Float(0.75f, 1.5f), num10, isIncendiary ? new Color(255, 140, 192) : Color.White));
										projectile.IsIncendiary = isIncendiary;
									}
									m_generatedProjectiles.Add(projectile, value: true);
									m_projectilesCount++;
								}
							}
						}
					}
				}
				else
				{
					m_surroundingPressureByPoint.Set(x, y, z, new SurroundingPressurePoint
					{
						Pressure = num4,
						IsIncendiary = isIncendiary
					});
					if (block.IsCollidable)
					{
						return;
					}
				}
			}
			toProcess.Add(new ProcessPoint
			{
				X = x,
				Y = y,
				Z = z,
				Axis = axis
			});
			processed.Set(x, y, z, value: true);
		}

		public void PostprocessExplosions(bool playExplosionSound)
		{
			Point3 point = Point3.Zero;
			float num = 3.40282347E+38f;
			float num2 = 0f;
			foreach (KeyValuePair<Point3, float> item in m_pressureByPoint.ToDictionary())
			{
				num2 += item.Value;
				float num3 = m_subsystemAudio.CalculateListenerDistance(new Vector3(item.Key));
				if (num3 < num)
				{
					num = num3;
					point = item.Key;
				}
				float num4 = 0.001f * MathUtils.Pow(num2, 0.5f);
				float num5 = MathUtils.Saturate(item.Value / 15f - num4) * m_random.Float(0.2f, 1f);
				if (num5 > 0.1f)
				{
					m_explosionParticleSystem.SetExplosionCell(item.Key, num5);
				}
			}
			foreach (KeyValuePair<Point3, SurroundingPressurePoint> item2 in m_surroundingPressureByPoint.ToDictionary())
			{
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(item2.Key.X, item2.Key.Y, item2.Key.Z);
				int num6 = Terrain.ExtractContents(cellValue);
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num6);
				if (blockBehaviors.Length != 0)
				{
					for (int i = 0; i < blockBehaviors.Length; i++)
					{
						blockBehaviors[i].OnExplosion(cellValue, item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value.Pressure);
					}
				}
				float probability = item2.Value.IsIncendiary ? 0.5f : 0.2f;
				Block block = BlocksManager.Blocks[num6];
				if (block.FireDuration > 0f && item2.Value.Pressure / block.ExplosionResilience > 0.2f && m_random.Bool(probability))
				{
					m_subsystemFireBlockBehavior.SetCellOnFire(item2.Key.X, item2.Key.Y, item2.Key.Z, item2.Value.IsIncendiary ? 1f : 0.3f);
				}
			}
			foreach (ComponentBody body in m_subsystemBodies.Bodies)
			{
				CalculateImpulseAndDamage(body, null, out Vector3 impulse, out float damage);
				impulse *= m_random.Float(0.5f, 1.5f);
				damage *= m_random.Float(0.5f, 1.5f);
				body.ApplyImpulse(impulse);
				body.Entity.FindComponent<ComponentHealth>()?.Injure(damage, null, ignoreInvulnerability: false, "Blasted by explosion");
				body.Entity.FindComponent<ComponentDamage>()?.Damage(damage);
				ComponentOnFire componentOnFire = body.Entity.FindComponent<ComponentOnFire>();
				if (componentOnFire != null && m_random.Float(0f, 1f) < MathUtils.Min(damage - 0.1f, 0.5f))
				{
					componentOnFire.SetOnFire(null, m_random.Float(6f, 8f));
				}
			}
			foreach (Pickable pickable in m_subsystemPickables.Pickables)
			{
				Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(pickable.Value)];
				CalculateImpulseAndDamage(pickable.Position + new Vector3(0f, 0.5f, 0f), 20f, null, out Vector3 impulse2, out float damage2);
				if (damage2 / block2.ExplosionResilience > 0.1f)
				{
					TryExplodeBlock(Terrain.ToCell(pickable.Position.X), Terrain.ToCell(pickable.Position.Y), Terrain.ToCell(pickable.Position.Z), pickable.Value);
					pickable.ToRemove = true;
				}
				else
				{
					Vector3 vector = (impulse2 + new Vector3(0f, 0.1f * impulse2.Length(), 0f)) * m_random.Float(0.75f, 1f);
					if (vector.Length() > 10f)
					{
						Projectile projectile = m_subsystemProjectiles.AddProjectile(pickable.Value, pickable.Position, pickable.Velocity + vector, m_random.Vector3(0f, 20f), null);
						if (m_random.Float(0f, 1f) < 0.33f)
						{
							m_subsystemProjectiles.AddTrail(projectile, Vector3.Zero, new SmokeTrailParticleSystem(15, m_random.Float(0.75f, 1.5f), m_random.Float(1f, 6f), Color.White));
						}
						pickable.ToRemove = true;
					}
					else
					{
						pickable.Velocity += vector;
					}
				}
			}
			foreach (Projectile projectile2 in m_subsystemProjectiles.Projectiles)
			{
				if (!m_generatedProjectiles.ContainsKey(projectile2))
				{
					CalculateImpulseAndDamage(projectile2.Position + new Vector3(0f, 0.5f, 0f), 20f, null, out Vector3 impulse3, out float _);
					projectile2.Velocity += (impulse3 + new Vector3(0f, 0.1f * impulse3.Length(), 0f)) * m_random.Float(0.75f, 1f);
				}
			}
			Vector3 position = new Vector3(point.X, point.Y, point.Z);
			float delay = m_subsystemAudio.CalculateDelay(num);
			if (num2 > 1000000f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionEnormous", 1f, m_random.Float(-0.1f, 0.1f), position, 40f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 100f);
			}
			else if (num2 > 100000f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionHuge", 1f, m_random.Float(-0.2f, 0.2f), position, 30f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 70f);
			}
			else if (num2 > 20000f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionLarge", 1f, m_random.Float(-0.2f, 0.2f), position, 26f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 50f);
			}
			else if (num2 > 4000f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionMedium", 1f, m_random.Float(-0.2f, 0.2f), position, 24f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 40f);
			}
			else if (num2 > 100f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionSmall", 1f, m_random.Float(-0.2f, 0.2f), position, 22f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 35f);
			}
			else if (num2 > 0f)
			{
				if (playExplosionSound)
				{
					m_subsystemAudio.PlaySound("Audio/ExplosionTiny", 1f, m_random.Float(-0.2f, 0.2f), position, 20f, delay);
				}
				m_subsystemNoise.MakeNoise(position, 1f, 30f);
			}
		}

		public void CalculateImpulseAndDamage(ComponentBody componentBody, float? obstaclePressure, out Vector3 impulse, out float damage)
		{
			CalculateImpulseAndDamage(0.5f * (componentBody.BoundingBox.Min + componentBody.BoundingBox.Max), componentBody.Mass, obstaclePressure, out impulse, out damage);
		}

		public void CalculateImpulseAndDamage(Vector3 position, float mass, float? obstaclePressure, out Vector3 impulse, out float damage)
		{
			Point3 point = Terrain.ToCell(position);
			if (!obstaclePressure.HasValue)
			{
				obstaclePressure = m_pressureByPoint.Get(point.X, point.Y, point.Z);
			}
			float num = 0f;
			Vector3 zero = Vector3.Zero;
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						int num2 = point.X + i;
						int num3 = point.Y + j;
						int num4 = point.Z + k;
						float num5 = (m_subsystemTerrain.Terrain.GetCellContents(num2, num3, num4) != 0) ? obstaclePressure.Value : m_pressureByPoint.Get(num2, num3, num4);
						if (i != 0 || j != 0 || k != 0)
						{
							zero += num5 * Vector3.Normalize(new Vector3(point.X - num2, point.Y - num3, point.Z - num4));
						}
						num += num5;
					}
				}
			}
			float num6 = MathUtils.Max(MathUtils.Pow(mass, 0.5f), 1f);
			impulse = 9.259259f * Vector3.Normalize(zero) * MathUtils.Pow(zero.Length(), 0.5f) / num6;
			damage = 2.59259248f * MathUtils.Pow(num, 0.5f) / num6;
		}
	}
}
