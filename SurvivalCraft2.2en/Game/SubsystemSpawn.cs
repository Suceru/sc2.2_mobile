using Engine;
using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemSpawn : Subsystem, IUpdateable
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemPlayers m_subsystemPlayers;

		public SubsystemGameWidgets m_subsystemViews;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemTime m_subsystemTime;

		public Random m_random = new Random();

		public double m_nextDiscardOldChunksTime = 1.0;

		public double m_nextVisitedTime = 1.0;

		public double m_nextChunkSpawnTime = 1.0;

		public double m_nextDespawnTime = 1.0;

		public Dictionary<Point2, SpawnChunk> m_chunks = new Dictionary<Point2, SpawnChunk>();

		public Dictionary<ComponentSpawn, bool> m_spawns = new Dictionary<ComponentSpawn, bool>();

		public const float MaxChunkAge = 76800f;

		public const float VisitedRadius = 8f;

		public const float SpawnRadius = 40f;

		public const float DespawnRadius = 52f;

		public Dictionary<ComponentSpawn, bool>.KeyCollection Spawns => m_spawns.Keys;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public event Action<SpawnChunk> SpawningChunk;

		public SpawnChunk GetSpawnChunk(Point2 point)
		{
			m_chunks.TryGetValue(point, out SpawnChunk value);
			return value;
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.GameTime >= m_nextDiscardOldChunksTime)
			{
				m_nextDiscardOldChunksTime = m_subsystemTime.GameTime + 60.0;
				DiscardOldChunks();
			}
			if (m_subsystemTime.GameTime >= m_nextVisitedTime)
			{
				m_nextVisitedTime = m_subsystemTime.GameTime + 5.0;
				UpdateLastVisitedTime();
			}
			if (m_subsystemTime.GameTime >= m_nextChunkSpawnTime)
			{
				m_nextChunkSpawnTime = m_subsystemTime.GameTime + 4.0;
				SpawnChunks();
			}
			if (m_subsystemTime.GameTime >= m_nextDespawnTime)
			{
				m_nextDespawnTime = m_subsystemTime.GameTime + 2.0;
				DespawnChunks();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemPlayers = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			m_subsystemViews = base.Project.FindSubsystem<SubsystemGameWidgets>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Chunks"))
			{
				ValuesDictionary valuesDictionary2 = (ValuesDictionary)item.Value;
				SpawnChunk spawnChunk = new SpawnChunk();
				spawnChunk.Point = HumanReadableConverter.ConvertFromString<Point2>(item.Key);
				spawnChunk.IsSpawned = valuesDictionary2.GetValue<bool>("IsSpawned");
				spawnChunk.LastVisitedTime = valuesDictionary2.GetValue<double>("LastVisitedTime");
				string value = valuesDictionary2.GetValue("SpawnsData", string.Empty);
				if (!string.IsNullOrEmpty(value))
				{
					LoadSpawnsData(value, spawnChunk.SpawnsData);
				}
				m_chunks[spawnChunk.Point] = spawnChunk;
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Chunks", valuesDictionary2);
			foreach (SpawnChunk value2 in m_chunks.Values)
			{
				if (value2.LastVisitedTime.HasValue)
				{
					ValuesDictionary valuesDictionary3 = new ValuesDictionary();
					valuesDictionary2.SetValue(HumanReadableConverter.ConvertToString(value2.Point), valuesDictionary3);
					valuesDictionary3.SetValue("IsSpawned", value2.IsSpawned);
					valuesDictionary3.SetValue("LastVisitedTime", value2.LastVisitedTime.Value);
					string value = SaveSpawnsData(value2.SpawnsData);
					if (!string.IsNullOrEmpty(value))
					{
						valuesDictionary3.SetValue("SpawnsData", value);
					}
				}
			}
		}

		public override void OnEntityAdded(Entity entity)
		{
			foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>())
			{
				m_spawns.Add(item, value: true);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			foreach (ComponentSpawn item in entity.FindComponents<ComponentSpawn>())
			{
				m_spawns.Remove(item);
			}
		}

		public SpawnChunk GetOrCreateSpawnChunk(Point2 point)
		{
			SpawnChunk spawnChunk = GetSpawnChunk(point);
			if (spawnChunk == null)
			{
				spawnChunk = new SpawnChunk
				{
					Point = point
				};
				m_chunks.Add(point, spawnChunk);
			}
			return spawnChunk;
		}

		public void DiscardOldChunks()
		{
			List<Point2> list = new List<Point2>();
			foreach (SpawnChunk value in m_chunks.Values)
			{
				if (!value.LastVisitedTime.HasValue || m_subsystemGameInfo.TotalElapsedGameTime - value.LastVisitedTime.Value > 76800.0)
				{
					list.Add(value.Point);
				}
			}
			foreach (Point2 item in list)
			{
				m_chunks.Remove(item);
			}
		}

		public void UpdateLastVisitedTime()
		{
			foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
			{
				Vector2 v = new Vector2(componentPlayer.ComponentBody.Position.X, componentPlayer.ComponentBody.Position.Z);
				Vector2 p = v - new Vector2(8f);
				Vector2 p2 = v + new Vector2(8f);
				Point2 point = Terrain.ToChunk(p);
				Point2 point2 = Terrain.ToChunk(p2);
				for (int i = point.X; i <= point2.X; i++)
				{
					for (int j = point.Y; j <= point2.Y; j++)
					{
						SpawnChunk spawnChunk = GetSpawnChunk(new Point2(i, j));
						if (spawnChunk != null)
						{
							spawnChunk.LastVisitedTime = m_subsystemGameInfo.TotalElapsedGameTime;
						}
					}
				}
			}
		}

		public void SpawnChunks()
		{
			List<SpawnChunk> list = new List<SpawnChunk>();
			foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
			{
				Vector2 v = new Vector2(gameWidget.ActiveCamera.ViewPosition.X, gameWidget.ActiveCamera.ViewPosition.Z);
				Vector2 p = v - new Vector2(40f);
				Vector2 p2 = v + new Vector2(40f);
				Point2 point = Terrain.ToChunk(p);
				Point2 point2 = Terrain.ToChunk(p2);
				for (int i = point.X; i <= point2.X; i++)
				{
					for (int j = point.Y; j <= point2.Y; j++)
					{
						Vector2 v2 = new Vector2(((float)i + 0.5f) * 16f, ((float)j + 0.5f) * 16f);
						if (Vector2.DistanceSquared(v, v2) < 1600f)
						{
							TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(v2.X), Terrain.ToCell(v2.Y));
							if (chunkAtCell != null && chunkAtCell.State > TerrainChunkState.InvalidPropagatedLight)
							{
								Point2 point3 = new Point2(i, j);
								SpawnChunk orCreateSpawnChunk = GetOrCreateSpawnChunk(point3);
								foreach (SpawnEntityData spawnsDatum in orCreateSpawnChunk.SpawnsData)
								{
									SpawnEntity(spawnsDatum);
								}
								orCreateSpawnChunk.SpawnsData.Clear();
								this.SpawningChunk?.Invoke(orCreateSpawnChunk);
								orCreateSpawnChunk.IsSpawned = true;
							}
						}
					}
				}
			}
			foreach (SpawnChunk item in list)
			{
				foreach (SpawnEntityData spawnsDatum2 in item.SpawnsData)
				{
					SpawnEntity(spawnsDatum2);
				}
				item.SpawnsData.Clear();
			}
		}

		public void DespawnChunks()
		{
			List<ComponentSpawn> list = new List<ComponentSpawn>(0);
			foreach (ComponentSpawn key in m_spawns.Keys)
			{
				if (key.AutoDespawn && !key.IsDespawning)
				{
					bool flag = true;
					Vector3 position = key.ComponentFrame.Position;
					Vector2 v = new Vector2(position.X, position.Z);
					foreach (GameWidget gameWidget in m_subsystemViews.GameWidgets)
					{
						Vector3 viewPosition = gameWidget.ActiveCamera.ViewPosition;
						Vector2 v2 = new Vector2(viewPosition.X, viewPosition.Z);
						if (Vector2.DistanceSquared(v, v2) <= 2704f)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						list.Add(key);
					}
				}
			}
			foreach (ComponentSpawn item in list)
			{
				Point2 point = Terrain.ToChunk(item.ComponentFrame.Position.XZ);
				GetOrCreateSpawnChunk(point).SpawnsData.Add(new SpawnEntityData
				{
					TemplateName = item.Entity.ValuesDictionary.DatabaseObject.Name,
					Position = item.ComponentFrame.Position,
					ConstantSpawn = (item.ComponentCreature?.ConstantSpawn ?? false)
				});
				item.Despawn();
			}
		}

		public Entity SpawnEntity(SpawnEntityData data)
		{
			try
			{
				Entity entity = DatabaseManager.CreateEntity(base.Project, data.TemplateName, throwIfNotFound: true);
				entity.FindComponent<ComponentBody>(throwOnError: true).Position = data.Position;
				entity.FindComponent<ComponentBody>(throwOnError: true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
				ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
				if (componentCreature != null)
				{
					componentCreature.ConstantSpawn = data.ConstantSpawn;
				}
				base.Project.AddEntity(entity);
				return entity;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to spawn entity with template \"{data.TemplateName}\". Reason: {ex.Message}");
				return null;
			}
		}

		public void LoadSpawnsData(string data, List<SpawnEntityData> creaturesData)
		{
			string[] array = data.Split(new char[1]
			{
				';'
			}, StringSplitOptions.RemoveEmptyEntries);
			int num = 0;
			while (true)
			{
				if (num < array.Length)
				{
					string[] array2 = array[num].Split(new char[1]
					{
						','
					}, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length < 4)
					{
						break;
					}
					SpawnEntityData spawnEntityData = new SpawnEntityData
					{
						TemplateName = array2[0],
						Position = new Vector3
						{
							X = float.Parse(array2[1], CultureInfo.InvariantCulture),
							Y = float.Parse(array2[2], CultureInfo.InvariantCulture),
							Z = float.Parse(array2[3], CultureInfo.InvariantCulture)
						}
					};
					if (array2.Length >= 5)
					{
						spawnEntityData.ConstantSpawn = bool.Parse(array2[4]);
					}
					creaturesData.Add(spawnEntityData);
					num++;
					continue;
				}
				return;
			}
			throw new InvalidOperationException("Invalid spawn data string.");
		}

		public string SaveSpawnsData(List<SpawnEntityData> spawnsData)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (SpawnEntityData spawnsDatum in spawnsData)
			{
				stringBuilder.Append(spawnsDatum.TemplateName);
				stringBuilder.Append(',');
				stringBuilder.Append((MathUtils.Round(spawnsDatum.Position.X * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append((MathUtils.Round(spawnsDatum.Position.Y * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append((MathUtils.Round(spawnsDatum.Position.Z * 10f) / 10f).ToString(CultureInfo.InvariantCulture));
				stringBuilder.Append(',');
				stringBuilder.Append(spawnsDatum.ConstantSpawn.ToString());
				stringBuilder.Append(';');
			}
			return stringBuilder.ToString();
		}
	}
}
