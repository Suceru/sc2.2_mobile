using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Game
{
	public class TerrainUpdater
	{
		public class UpdateStatistics
		{
			public static int m_counter;

			public double FindBestChunkTime;

			public int FindBestChunkCount;

			public double LoadingTime;

			public int LoadingCount;

			public double ContentsTime1;

			public int ContentsCount1;

			public double ContentsTime2;

			public int ContentsCount2;

			public double ContentsTime3;

			public int ContentsCount3;

			public double ContentsTime4;

			public int ContentsCount4;

			public double LightTime;

			public int LightCount;

			public double LightSourcesTime;

			public int LightSourcesCount;

			public double LightPropagateTime;

			public int LightPropagateCount;

			public int LightSourceInstancesCount;

			public double VerticesTime1;

			public int VerticesCount1;

			public double VerticesTime2;

			public int VerticesCount2;

			public int HashCount;

			public double HashTime;

			public int GeneratedSlices;

			public int SkippedSlices;

			public void Log()
			{
				Engine.Log.Information("Terrain Update {0}", m_counter++);
				if (FindBestChunkCount > 0)
				{
					Engine.Log.Information("    FindBestChunk:          {0:0.0}ms ({1}x)", FindBestChunkTime * 1000.0, FindBestChunkCount);
				}
				if (LoadingCount > 0)
				{
					Engine.Log.Information("    Loading:                {0:0.0}ms ({1}x)", LoadingTime * 1000.0, LoadingCount);
				}
				if (ContentsCount1 > 0)
				{
					Engine.Log.Information("    Contents1:              {0:0.0}ms ({1}x)", ContentsTime1 * 1000.0, ContentsCount1);
				}
				if (ContentsCount2 > 0)
				{
					Engine.Log.Information("    Contents2:              {0:0.0}ms ({1}x)", ContentsTime2 * 1000.0, ContentsCount2);
				}
				if (ContentsCount3 > 0)
				{
					Engine.Log.Information("    Contents3:              {0:0.0}ms ({1}x)", ContentsTime3 * 1000.0, ContentsCount3);
				}
				if (ContentsCount4 > 0)
				{
					Engine.Log.Information("    Contents4:              {0:0.0}ms ({1}x)", ContentsTime4 * 1000.0, ContentsCount4);
				}
				if (LightCount > 0)
				{
					Engine.Log.Information("    Light:                  {0:0.0}ms ({1}x)", LightTime * 1000.0, LightCount);
				}
				if (LightSourcesCount > 0)
				{
					Engine.Log.Information("    LightSources:           {0:0.0}ms ({1}x)", LightSourcesTime * 1000.0, LightSourcesCount);
				}
				if (LightPropagateCount > 0)
				{
					Engine.Log.Information("    LightPropagate:         {0:0.0}ms ({1}x) {2} ls", LightPropagateTime * 1000.0, LightPropagateCount, LightSourceInstancesCount);
				}
				if (VerticesCount1 > 0)
				{
					Engine.Log.Information("    Vertices1:              {0:0.0}ms ({1}x)", VerticesTime1 * 1000.0, VerticesCount1);
				}
				if (VerticesCount2 > 0)
				{
					Engine.Log.Information("    Vertices2:              {0:0.0}ms ({1}x)", VerticesTime2 * 1000.0, VerticesCount2);
				}
				if (VerticesCount1 + VerticesCount2 > 0)
				{
					Engine.Log.Information("    AllVertices:            {0:0.0}ms ({1}x)", (VerticesTime1 + VerticesTime2) * 1000.0, VerticesCount1 + VerticesCount2);
				}
				if (HashCount > 0)
				{
					Engine.Log.Information("        Hash:               {0:0.0}ms ({1}x)", HashTime * 1000.0, HashCount);
				}
				if (GeneratedSlices > 0)
				{
					Engine.Log.Information("        Generated Slices:   {0}/{1}", GeneratedSlices, GeneratedSlices + SkippedSlices);
				}
			}
		}

		public struct UpdateLocation
		{
			public Vector2 Center;

			public Vector2? LastChunksUpdateCenter;

			public float VisibilityDistance;

			public float ContentDistance;
		}

		public struct UpdateParameters
		{
			public TerrainChunk[] Chunks;

			public Dictionary<int, UpdateLocation> Locations;
		}

		public struct LightSource
		{
			public int X;

			public int Y;

			public int Z;

			public int Light;
		}

		public const int m_lightAttenuationWithDistance = 1;

		public const float m_updateHysteresis = 8f;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public Terrain m_terrain;

		public DynamicArray<LightSource> m_lightSources = new DynamicArray<LightSource>();

		public UpdateStatistics m_statistics = new UpdateStatistics();

		public Task m_task;

		public AutoResetEvent m_updateEvent = new AutoResetEvent(initialState: true);

		public ManualResetEvent m_pauseEvent = new ManualResetEvent(initialState: true);

		public volatile bool m_quitUpdateThread;

		public bool m_unpauseUpdateThread;

		public object m_updateParametersLock = new object();

		public object m_unpauseLock = new object();

		public UpdateParameters m_updateParameters;

		public UpdateParameters m_threadUpdateParameters;

		public int m_lastSkylightValue;

		public int m_synchronousUpdateFrame;

		public Dictionary<int, UpdateLocation?> m_pendingLocations = new Dictionary<int, UpdateLocation?>();

		public static int SlowTerrainUpdate;

		public static bool LogTerrainUpdateStats;

		public AutoResetEvent UpdateEvent => m_updateEvent;

		public TerrainUpdater(SubsystemTerrain subsystemTerrain)
		{
			m_subsystemTerrain = subsystemTerrain;
			m_subsystemSky = m_subsystemTerrain.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemBlockBehaviors = m_subsystemTerrain.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_terrain = subsystemTerrain.Terrain;
			m_updateParameters.Chunks = new TerrainChunk[0];
			m_updateParameters.Locations = new Dictionary<int, UpdateLocation>();
			m_threadUpdateParameters.Chunks = new TerrainChunk[0];
			m_threadUpdateParameters.Locations = new Dictionary<int, UpdateLocation>();
			SettingsManager.SettingChanged += SettingsManager_SettingChanged;
		}

		public void Dispose()
		{
			SettingsManager.SettingChanged -= SettingsManager_SettingChanged;
			m_quitUpdateThread = true;
			UnpauseUpdateThread();
			m_updateEvent.Set();
			if (m_task != null)
			{
				m_task.Wait();
				m_task = null;
			}
			m_pauseEvent.Dispose();
			m_updateEvent.Dispose();
		}

		public void RequestSynchronousUpdate()
		{
			m_synchronousUpdateFrame = Time.FrameIndex;
		}

		public void SetUpdateLocation(int locationIndex, Vector2 center, float visibilityDistance, float contentDistance)
		{
			contentDistance = MathUtils.Max(contentDistance, visibilityDistance);
			m_updateParameters.Locations.TryGetValue(locationIndex, out UpdateLocation value);
			if (contentDistance != value.ContentDistance || visibilityDistance != value.VisibilityDistance || !value.LastChunksUpdateCenter.HasValue || Vector2.DistanceSquared(center, value.LastChunksUpdateCenter.Value) > 64f)
			{
				value.Center = center;
				value.VisibilityDistance = visibilityDistance;
				value.ContentDistance = contentDistance;
				value.LastChunksUpdateCenter = center;
				m_pendingLocations[locationIndex] = value;
			}
		}

		public void RemoveUpdateLocation(int locationIndex)
		{
			m_pendingLocations[locationIndex] = null;
		}

		public float GetUpdateProgress(int locationIndex, float visibilityDistance, float contentDistance)
		{
			int num = 0;
			int num2 = 0;
			if (m_updateParameters.Locations.TryGetValue(locationIndex, out UpdateLocation value))
			{
				visibilityDistance = MathUtils.Max(MathUtils.Min(visibilityDistance, value.VisibilityDistance) - 8f - 0.1f, 0f);
				contentDistance = MathUtils.Max(MathUtils.Min(contentDistance, value.ContentDistance) - 8f - 0.1f, 0f);
				float num3 = MathUtils.Sqr(visibilityDistance);
				float num4 = MathUtils.Sqr(contentDistance);
				float v = MathUtils.Max(visibilityDistance, contentDistance);
				Point2 point = Terrain.ToChunk(value.Center - new Vector2(v));
				Point2 point2 = Terrain.ToChunk(value.Center + new Vector2(v));
				for (int i = point.X; i <= point2.X; i++)
				{
					for (int j = point.Y; j <= point2.Y; j++)
					{
						TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(i, j);
						float num5 = Vector2.DistanceSquared(v2: new Vector2(((float)i + 0.5f) * 16f, ((float)j + 0.5f) * 16f), v1: value.Center);
						if (num5 <= num3)
						{
							if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.Valid)
							{
								num2++;
							}
							else
							{
								num++;
							}
						}
						else if (num5 <= num4)
						{
							if (chunkAtCoords == null || chunkAtCoords.State < TerrainChunkState.InvalidLight)
							{
								num2++;
							}
							else
							{
								num++;
							}
						}
					}
				}
				if (num2 <= 0)
				{
					return 1f;
				}
				return (float)num / (float)(num2 + num);
			}
			return 0f;
		}

		public void Update()
		{
			if (m_subsystemSky.SkyLightValue != m_lastSkylightValue)
			{
				m_lastSkylightValue = m_subsystemSky.SkyLightValue;
				DowngradeAllChunksState(TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
			}
			if (!SettingsManager.MultithreadedTerrainUpdate)
			{
				if (m_task != null)
				{
					m_quitUpdateThread = true;
					UnpauseUpdateThread();
					m_updateEvent.Set();
					m_task.Wait();
					m_task = null;
				}
				double realTime = Time.RealTime;
				while (!SynchronousUpdateFunction() && Time.RealTime - realTime < 0.0099999997764825821)
				{
				}
			}
			else if (m_task == null)
			{
				m_quitUpdateThread = false;
				m_task = Task.Run((Action)ThreadUpdateFunction);
				UnpauseUpdateThread();
				m_updateEvent.Set();
			}
			if (m_pendingLocations.Count > 0)
			{
				m_pauseEvent.Reset();
				if (m_updateEvent.WaitOne(0))
				{
					m_pauseEvent.Set();
					try
					{
						foreach (KeyValuePair<int, UpdateLocation?> pendingLocation in m_pendingLocations)
						{
							if (pendingLocation.Value.HasValue)
							{
								m_updateParameters.Locations[pendingLocation.Key] = pendingLocation.Value.Value;
							}
							else
							{
								m_updateParameters.Locations.Remove(pendingLocation.Key);
							}
						}
						if (AllocateAndFreeChunks(m_updateParameters.Locations.Values.ToArray()))
						{
							m_updateParameters.Chunks = m_terrain.AllocatedChunks;
						}
						m_pendingLocations.Clear();
					}
					finally
					{
						m_updateEvent.Set();
					}
				}
			}
			else
			{
				lock (m_updateParametersLock)
				{
					if (SendReceiveChunkStates())
					{
						UnpauseUpdateThread();
					}
				}
			}
			TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
			foreach (TerrainChunk terrainChunk in allocatedChunks)
			{
				if (terrainChunk.State >= TerrainChunkState.InvalidVertices1 && !terrainChunk.AreBehaviorsNotified)
				{
					terrainChunk.AreBehaviorsNotified = true;
					NotifyBlockBehaviors(terrainChunk);
				}
			}
		}

		public void PrepareForDrawing(Camera camera)
		{
			SetUpdateLocation(camera.GameWidget.PlayerData.PlayerIndex, camera.ViewPosition.XZ, m_subsystemSky.VisibilityRange, 64f);
			if (m_synchronousUpdateFrame == Time.FrameIndex)
			{
				List<TerrainChunk> list = DetermineSynchronousUpdateChunks(camera.ViewPosition, camera.ViewDirection);
				if (list.Count > 0)
				{
					m_updateEvent.WaitOne();
					try
					{
						SendReceiveChunkStates();
						SendReceiveChunkStatesThread();
						foreach (TerrainChunk item in list)
						{
							while (item.ThreadState < TerrainChunkState.Valid)
							{
								UpdateChunkSingleStep(item, m_subsystemSky.SkyLightValue);
							}
						}
						SendReceiveChunkStatesThread();
						SendReceiveChunkStates();
					}
					finally
					{
						m_updateEvent.Set();
					}
				}
			}
		}

		public void DowngradeChunkNeighborhoodState(Point2 coordinates, int radius, TerrainChunkState state, bool forceGeometryRegeneration)
		{
			for (int i = -radius; i <= radius; i++)
			{
				for (int j = -radius; j <= radius; j++)
				{
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(coordinates.X + i, coordinates.Y + j);
					if (chunkAtCoords == null)
					{
						continue;
					}
					if (chunkAtCoords.State > state)
					{
						chunkAtCoords.State = state;
						if (forceGeometryRegeneration)
						{
							chunkAtCoords.Geometry.InvalidateSliceContentsHashes();
						}
					}
					chunkAtCoords.WasDowngraded = true;
				}
			}
		}

		public void DowngradeAllChunksState(TerrainChunkState state, bool forceGeometryRegeneration)
		{
			TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
			foreach (TerrainChunk terrainChunk in allocatedChunks)
			{
				if (terrainChunk.State > state)
				{
					terrainChunk.State = state;
					if (forceGeometryRegeneration)
					{
						terrainChunk.Geometry.InvalidateSliceContentsHashes();
					}
				}
				terrainChunk.WasDowngraded = true;
			}
		}

		public static bool IsChunkInRange(Vector2 chunkCenter, UpdateLocation[] locations)
		{
			for (int i = 0; i < locations.Length; i++)
			{
				if (Vector2.DistanceSquared(locations[i].Center, chunkCenter) <= MathUtils.Sqr(locations[i].ContentDistance))
				{
					return true;
				}
			}
			return false;
		}

		public bool AllocateAndFreeChunks(UpdateLocation[] locations)
		{
			bool result = false;
			TerrainChunk[] allocatedChunks = m_terrain.AllocatedChunks;
			foreach (TerrainChunk terrainChunk in allocatedChunks)
			{
				if (!IsChunkInRange(terrainChunk.Center, locations))
				{
					result = true;
					foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
					{
						blockBehavior.OnChunkDiscarding(terrainChunk);
					}
					m_subsystemTerrain.TerrainSerializer.SaveChunk(terrainChunk);
					m_terrain.FreeChunk(terrainChunk);
					m_subsystemTerrain.TerrainRenderer.DisposeTerrainChunkGeometryVertexIndexBuffers(terrainChunk.Geometry);
				}
			}
			for (int j = 0; j < locations.Length; j++)
			{
				Point2 point = Terrain.ToChunk(locations[j].Center - new Vector2(locations[j].ContentDistance));
				Point2 point2 = Terrain.ToChunk(locations[j].Center + new Vector2(locations[j].ContentDistance));
				for (int k = point.X; k <= point2.X; k++)
				{
					for (int l = point.Y; l <= point2.Y; l++)
					{
						Vector2 chunkCenter = new Vector2(((float)k + 0.5f) * 16f, ((float)l + 0.5f) * 16f);
						TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(k, l);
						if (chunkAtCoords == null)
						{
							if (IsChunkInRange(chunkCenter, locations))
							{
								result = true;
								m_terrain.AllocateChunk(k, l);
								DowngradeChunkNeighborhoodState(new Point2(k, l), 0, TerrainChunkState.NotLoaded, forceGeometryRegeneration: false);
								DowngradeChunkNeighborhoodState(new Point2(k, l), 1, TerrainChunkState.InvalidLight, forceGeometryRegeneration: false);
							}
						}
						else if (chunkAtCoords.Coords.X != k || chunkAtCoords.Coords.Y != l)
						{
							Log.Error("Chunk wraparound detected at {0}", chunkAtCoords.Coords);
						}
					}
				}
			}
			return result;
		}

		public bool SendReceiveChunkStates()
		{
			bool result = false;
			TerrainChunk[] chunks = m_updateParameters.Chunks;
			foreach (TerrainChunk terrainChunk in chunks)
			{
				if (terrainChunk.WasDowngraded)
				{
					terrainChunk.DowngradedState = terrainChunk.State;
					terrainChunk.WasDowngraded = false;
					result = true;
				}
				else if (terrainChunk.UpgradedState.HasValue)
				{
					terrainChunk.State = terrainChunk.UpgradedState.Value;
				}
				terrainChunk.UpgradedState = null;
			}
			return result;
		}

		public void SendReceiveChunkStatesThread()
		{
			TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
			foreach (TerrainChunk terrainChunk in chunks)
			{
				if (terrainChunk.DowngradedState.HasValue)
				{
					terrainChunk.ThreadState = terrainChunk.DowngradedState.Value;
					terrainChunk.DowngradedState = null;
				}
				else if (terrainChunk.WasUpgraded)
				{
					terrainChunk.UpgradedState = terrainChunk.ThreadState;
				}
				terrainChunk.WasUpgraded = false;
			}
		}

		public void ThreadUpdateFunction()
		{
			while (!m_quitUpdateThread)
			{
				m_pauseEvent.WaitOne();
				m_updateEvent.WaitOne();
				try
				{
					if (SynchronousUpdateFunction())
					{
						lock (m_unpauseLock)
						{
							if (!m_unpauseUpdateThread)
							{
								m_pauseEvent.Reset();
							}
							m_unpauseUpdateThread = false;
						}
					}
				}
				catch (Exception)
				{
				}
				finally
				{
					m_updateEvent.Set();
				}
			}
		}

		public bool SynchronousUpdateFunction()
		{
			lock (m_updateParametersLock)
			{
				m_threadUpdateParameters = m_updateParameters;
				SendReceiveChunkStatesThread();
			}
			TerrainChunkState desiredState;
			TerrainChunk terrainChunk = FindBestChunkToUpdate(out desiredState);
			if (terrainChunk != null)
			{
				double realTime = Time.RealTime;
				do
				{
					UpdateChunkSingleStep(terrainChunk, m_subsystemSky.SkyLightValue);
				}
				while (terrainChunk.ThreadState < desiredState && Time.RealTime - realTime < 0.0099999997764825821);
				return false;
			}
			if (LogTerrainUpdateStats)
			{
				m_statistics.Log();
				m_statistics = new UpdateStatistics();
			}
			return true;
		}

		public TerrainChunk FindBestChunkToUpdate(out TerrainChunkState desiredState)
		{
			double realTime = Time.RealTime;
			TerrainChunk[] chunks = m_threadUpdateParameters.Chunks;
			UpdateLocation[] array = m_threadUpdateParameters.Locations.Values.ToArray();
			float num = 3.40282347E+38f;
			TerrainChunk result = null;
			desiredState = TerrainChunkState.NotLoaded;
			foreach (TerrainChunk terrainChunk in chunks)
			{
				if (terrainChunk.ThreadState >= TerrainChunkState.Valid)
				{
					continue;
				}
				for (int j = 0; j < array.Length; j++)
				{
					float num2 = Vector2.DistanceSquared(array[j].Center, terrainChunk.Center);
					if (num2 < num)
					{
						if (num2 <= MathUtils.Sqr(array[j].VisibilityDistance))
						{
							desiredState = TerrainChunkState.Valid;
							num = num2;
							result = terrainChunk;
						}
						else if (terrainChunk.ThreadState < TerrainChunkState.InvalidVertices1 && num2 <= MathUtils.Sqr(array[j].ContentDistance))
						{
							desiredState = TerrainChunkState.InvalidVertices1;
							num = num2;
							result = terrainChunk;
						}
					}
				}
			}
			double realTime2 = Time.RealTime;
			m_statistics.FindBestChunkTime += realTime2 - realTime;
			m_statistics.FindBestChunkCount++;
			return result;
		}

		public List<TerrainChunk> DetermineSynchronousUpdateChunks(Vector3 viewPosition, Vector3 viewDirection)
		{
			Vector3 vector = Vector3.Normalize(Vector3.Cross(viewDirection, Vector3.UnitY));
			Vector3 v = Vector3.Normalize(Vector3.Cross(viewDirection, vector));
			Vector3[] obj = new Vector3[6]
			{
				viewPosition,
				viewPosition + 6f * viewDirection,
				viewPosition + 6f * viewDirection - 6f * vector,
				viewPosition + 6f * viewDirection + 6f * vector,
				viewPosition + 6f * viewDirection - 2f * v,
				viewPosition + 6f * viewDirection + 2f * v
			};
			List<TerrainChunk> list = new List<TerrainChunk>();
			Vector3[] array = obj;
			for (int i = 0; i < array.Length; i++)
			{
				Vector3 vector2 = array[i];
				TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Z));
				if (chunkAtCell != null && chunkAtCell.State < TerrainChunkState.Valid && !list.Contains(chunkAtCell))
				{
					list.Add(chunkAtCell);
				}
			}
			return list;
		}

		public void UpdateChunkSingleStep(TerrainChunk chunk, int skylightValue)
		{
			switch (chunk.ThreadState)
			{
			case TerrainChunkState.NotLoaded:
			{
				double realTime19 = Time.RealTime;
				if (m_subsystemTerrain.TerrainSerializer.LoadChunk(chunk))
				{
					chunk.ThreadState = TerrainChunkState.InvalidLight;
					chunk.WasUpgraded = true;
					double realTime20 = Time.RealTime;
					chunk.IsLoaded = true;
					m_statistics.LoadingCount++;
					m_statistics.LoadingTime += realTime20 - realTime19;
				}
				else
				{
					chunk.ThreadState = TerrainChunkState.InvalidContents1;
					chunk.WasUpgraded = true;
				}
				break;
			}
			case TerrainChunkState.InvalidContents1:
			{
				double realTime17 = Time.RealTime;
				m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass1(chunk);
				chunk.ThreadState = TerrainChunkState.InvalidContents2;
				chunk.WasUpgraded = true;
				double realTime18 = Time.RealTime;
				m_statistics.ContentsCount1++;
				m_statistics.ContentsTime1 += realTime18 - realTime17;
				break;
			}
			case TerrainChunkState.InvalidContents2:
			{
				double realTime15 = Time.RealTime;
				m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass2(chunk);
				chunk.ThreadState = TerrainChunkState.InvalidContents3;
				chunk.WasUpgraded = true;
				double realTime16 = Time.RealTime;
				m_statistics.ContentsCount2++;
				m_statistics.ContentsTime2 += realTime16 - realTime15;
				break;
			}
			case TerrainChunkState.InvalidContents3:
			{
				double realTime13 = Time.RealTime;
				m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass3(chunk);
				chunk.ThreadState = TerrainChunkState.InvalidContents4;
				chunk.WasUpgraded = true;
				double realTime14 = Time.RealTime;
				m_statistics.ContentsCount3++;
				m_statistics.ContentsTime3 += realTime14 - realTime13;
				break;
			}
			case TerrainChunkState.InvalidContents4:
			{
				double realTime7 = Time.RealTime;
				m_subsystemTerrain.TerrainContentsGenerator.GenerateChunkContentsPass4(chunk);
				chunk.ThreadState = TerrainChunkState.InvalidLight;
				chunk.WasUpgraded = true;
				double realTime8 = Time.RealTime;
				m_statistics.ContentsCount4++;
				m_statistics.ContentsTime4 += realTime8 - realTime7;
				break;
			}
			case TerrainChunkState.InvalidLight:
			{
				double realTime3 = Time.RealTime;
				GenerateChunkSunLightAndHeight(chunk, skylightValue);
				chunk.ThreadState = TerrainChunkState.InvalidPropagatedLight;
				chunk.WasUpgraded = true;
				chunk.LightPropagationMask = 0;
				double realTime4 = Time.RealTime;
				m_statistics.LightCount++;
				m_statistics.LightTime += realTime4 - realTime3;
				break;
			}
			case TerrainChunkState.InvalidPropagatedLight:
			{
				for (int i = -2; i <= 2; i++)
				{
					for (int j = -2; j <= 2; j++)
					{
						TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(chunk.Origin.X + i * 16, chunk.Origin.Y + j * 16);
						if (chunkAtCell != null && chunkAtCell.ThreadState < TerrainChunkState.InvalidPropagatedLight)
						{
							UpdateChunkSingleStep(chunkAtCell, skylightValue);
							return;
						}
					}
				}
				double realTime9 = Time.RealTime;
				m_lightSources.Clear();
				for (int k = -1; k <= 1; k++)
				{
					for (int l = -1; l <= 1; l++)
					{
						int num = CalculateLightPropagationBitIndex(k, l);
						if (((chunk.LightPropagationMask >> num) & 1) == 0)
						{
							TerrainChunk chunkAtCell2 = m_terrain.GetChunkAtCell(chunk.Origin.X + k * 16, chunk.Origin.Y + l * 16);
							if (chunkAtCell2 != null)
							{
								GenerateChunkLightSources(chunkAtCell2);
								UpdateNeighborsLightPropagationBitmasks(chunkAtCell2);
							}
						}
					}
				}
				double realTime10 = Time.RealTime;
				m_statistics.LightSourcesCount++;
				m_statistics.LightSourcesTime += realTime10 - realTime9;
				double realTime11 = Time.RealTime;
				PropagateLight();
				chunk.ThreadState = TerrainChunkState.InvalidVertices1;
				chunk.WasUpgraded = true;
				double realTime12 = Time.RealTime;
				m_statistics.LightPropagateCount++;
				m_statistics.LightSourceInstancesCount += m_lightSources.Count;
				m_statistics.LightPropagateTime += realTime12 - realTime11;
				break;
			}
			case TerrainChunkState.InvalidVertices1:
			{
				double realTime5 = Time.RealTime;
				lock (chunk.Geometry)
				{
					chunk.NewGeometryData = false;
					GenerateChunkVertices(chunk, even: true);
				}
				chunk.ThreadState = TerrainChunkState.InvalidVertices2;
				chunk.WasUpgraded = true;
				double realTime6 = Time.RealTime;
				m_statistics.VerticesCount1++;
				m_statistics.VerticesTime1 += realTime6 - realTime5;
				break;
			}
			case TerrainChunkState.InvalidVertices2:
			{
				double realTime = Time.RealTime;
				lock (chunk.Geometry)
				{
					GenerateChunkVertices(chunk, even: false);
					chunk.NewGeometryData = true;
				}
				chunk.ThreadState = TerrainChunkState.Valid;
				chunk.WasUpgraded = true;
				double realTime2 = Time.RealTime;
				m_statistics.VerticesCount2++;
				m_statistics.VerticesTime2 += realTime2 - realTime;
				break;
			}
			}
		}

		public void GenerateChunkSunLightAndHeight(TerrainChunk chunk, int skylightValue)
		{
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = 0;
					int num2 = 255;
					int num3 = 0;
					int num4 = 255;
					int num5 = TerrainChunk.CalculateCellIndex(i, 255, j);
					while (num4 >= 0)
					{
						int cellValueFast = chunk.GetCellValueFast(num5);
						if (Terrain.ExtractContents(cellValueFast) != 0)
						{
							num = num4;
							break;
						}
						cellValueFast = Terrain.ReplaceLight(cellValueFast, skylightValue);
						chunk.SetCellValueFast(num5, cellValueFast);
						num4--;
						num5--;
					}
					num4 = 0;
					num5 = TerrainChunk.CalculateCellIndex(i, 0, j);
					while (num4 <= num + 1)
					{
						int cellValueFast2 = chunk.GetCellValueFast(num5);
						int num6 = Terrain.ExtractContents(cellValueFast2);
						if (BlocksManager.Blocks[num6].IsTransparent)
						{
							num2 = num4;
							break;
						}
						cellValueFast2 = Terrain.ReplaceLight(cellValueFast2, 0);
						chunk.SetCellValueFast(num5, cellValueFast2);
						num4++;
						num5++;
					}
					int num7 = skylightValue;
					num4 = num;
					num5 = TerrainChunk.CalculateCellIndex(i, num, j);
					if (num7 > 0)
					{
						while (num4 >= num2)
						{
							int cellValueFast3 = chunk.GetCellValueFast(num5);
							int num8 = Terrain.ExtractContents(cellValueFast3);
							if (num8 != 0)
							{
								Block block = BlocksManager.Blocks[num8];
								if (!block.IsTransparent || block.LightAttenuation >= num7)
								{
									break;
								}
								num7 -= block.LightAttenuation;
							}
							cellValueFast3 = Terrain.ReplaceLight(cellValueFast3, num7);
							chunk.SetCellValueFast(num5, cellValueFast3);
							num4--;
							num5--;
						}
					}
					num3 = num4 + 1;
					while (num4 >= num2)
					{
						int cellValueFast4 = chunk.GetCellValueFast(num5);
						cellValueFast4 = Terrain.ReplaceLight(cellValueFast4, 0);
						chunk.SetCellValueFast(num5, cellValueFast4);
						num4--;
						num5--;
					}
					chunk.SetTopHeightFast(i, j, num);
					chunk.SetBottomHeightFast(i, j, num2);
					chunk.SetSunlightHeightFast(i, j, num3);
				}
			}
		}

		public void GenerateChunkLightSources(TerrainChunk chunk)
		{
			Block[] blocks = BlocksManager.Blocks;
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int num = i + chunk.Origin.X;
					int num2 = j + chunk.Origin.Y;
					TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(num - 1, num2);
					TerrainChunk chunkAtCell2 = m_terrain.GetChunkAtCell(num + 1, num2);
					TerrainChunk chunkAtCell3 = m_terrain.GetChunkAtCell(num, num2 - 1);
					TerrainChunk chunkAtCell4 = m_terrain.GetChunkAtCell(num, num2 + 1);
					if (chunkAtCell == null || chunkAtCell2 == null || chunkAtCell3 == null || chunkAtCell4 == null)
					{
						continue;
					}
					int topHeightFast = chunk.GetTopHeightFast(i, j);
					int bottomHeightFast = chunk.GetBottomHeightFast(i, j);
					int x = num - 1 - chunkAtCell.Origin.X;
					int z = num2 - chunkAtCell.Origin.Y;
					int x2 = num + 1 - chunkAtCell2.Origin.X;
					int z2 = num2 - chunkAtCell2.Origin.Y;
					int x3 = num - chunkAtCell3.Origin.X;
					int z3 = num2 - 1 - chunkAtCell3.Origin.Y;
					int x4 = num - chunkAtCell4.Origin.X;
					int z4 = num2 + 1 - chunkAtCell4.Origin.Y;
					int shaftValueFast = chunkAtCell.GetShaftValueFast(x, z);
					int shaftValueFast2 = chunkAtCell2.GetShaftValueFast(x2, z2);
					int shaftValueFast3 = chunkAtCell3.GetShaftValueFast(x3, z3);
					int shaftValueFast4 = chunkAtCell4.GetShaftValueFast(x4, z4);
					int x5 = Terrain.ExtractSunlightHeight(shaftValueFast);
					int x6 = Terrain.ExtractSunlightHeight(shaftValueFast2);
					int x7 = Terrain.ExtractSunlightHeight(shaftValueFast3);
					int x8 = Terrain.ExtractSunlightHeight(shaftValueFast4);
					int num3 = MathUtils.Min(x5, x6, x7, x8);
					int num4 = bottomHeightFast;
					int num5 = TerrainChunk.CalculateCellIndex(i, bottomHeightFast, j);
					while (num4 <= topHeightFast)
					{
						int cellValueFast = chunk.GetCellValueFast(num5);
						int num6 = 0;
						Block block = blocks[Terrain.ExtractContents(cellValueFast)];
						if (num4 >= num3 && block.IsTransparent)
						{
							int cellLightFast = chunkAtCell.GetCellLightFast(x, num4, z);
							int cellLightFast2 = chunkAtCell2.GetCellLightFast(x2, num4, z2);
							int cellLightFast3 = chunkAtCell3.GetCellLightFast(x3, num4, z3);
							int cellLightFast4 = chunkAtCell4.GetCellLightFast(x4, num4, z4);
							num6 = MathUtils.Max(cellLightFast, cellLightFast2, cellLightFast3, cellLightFast4) - 1 - block.LightAttenuation;
						}
						if (block.DefaultEmittedLightAmount > 0)
						{
							num6 = MathUtils.Max(num6, block.GetEmittedLightAmount(cellValueFast));
						}
						if (num6 > Terrain.ExtractLight(cellValueFast))
						{
							chunk.SetCellValueFast(num5, Terrain.ReplaceLight(cellValueFast, num6));
							m_lightSources.Add(new LightSource
							{
								X = num,
								Y = num4,
								Z = num2,
								Light = num6
							});
						}
						num4++;
						num5++;
					}
				}
			}
		}

		public void PropagateLightSource(int x, int y, int z, int light)
		{
			TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(x, z);
			if (chunkAtCell == null)
			{
				return;
			}
			int index = TerrainChunk.CalculateCellIndex(x & 0xF, y, z & 0xF);
			int cellValueFast = chunkAtCell.GetCellValueFast(index);
			int num = Terrain.ExtractContents(cellValueFast);
			Block block = BlocksManager.Blocks[num];
			if (block.IsTransparent)
			{
				int num2 = light - block.LightAttenuation - 1;
				if (num2 > Terrain.ExtractLight(cellValueFast))
				{
					m_lightSources.Add(new LightSource
					{
						X = x,
						Y = y,
						Z = z,
						Light = num2
					});
					chunkAtCell.SetCellValueFast(index, Terrain.ReplaceLight(cellValueFast, num2));
				}
			}
		}

		public void PropagateLight()
		{
			for (int i = 0; i < m_lightSources.Count && i < 120000; i++)
			{
				LightSource lightSource = m_lightSources.Array[i];
				int light = lightSource.Light;
				if (light > 1)
				{
					PropagateLightSource(lightSource.X - 1, lightSource.Y, lightSource.Z, light);
					PropagateLightSource(lightSource.X + 1, lightSource.Y, lightSource.Z, light);
					if (lightSource.Y > 0)
					{
						PropagateLightSource(lightSource.X, lightSource.Y - 1, lightSource.Z, light);
					}
					if (lightSource.Y < 255)
					{
						PropagateLightSource(lightSource.X, lightSource.Y + 1, lightSource.Z, light);
					}
					PropagateLightSource(lightSource.X, lightSource.Y, lightSource.Z - 1, light);
					PropagateLightSource(lightSource.X, lightSource.Y, lightSource.Z + 1, light);
				}
			}
		}

		public void GenerateChunkVertices(TerrainChunk chunk, bool even)
		{
			m_subsystemTerrain.BlockGeometryGenerator.ResetCache();
			TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y - 1);
			TerrainChunk chunkAtCoords2 = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y - 1);
			TerrainChunk chunkAtCoords3 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y - 1);
			TerrainChunk chunkAtCoords4 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y);
			TerrainChunk chunkAtCoords5 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y);
			TerrainChunk chunkAtCoords6 = m_terrain.GetChunkAtCoords(chunk.Coords.X - 1, chunk.Coords.Y + 1);
			TerrainChunk chunkAtCoords7 = m_terrain.GetChunkAtCoords(chunk.Coords.X, chunk.Coords.Y + 1);
			TerrainChunk chunkAtCoords8 = m_terrain.GetChunkAtCoords(chunk.Coords.X + 1, chunk.Coords.Y + 1);
			int num = 0;
			int num2 = 0;
			int num3 = 16;
			int num4 = 16;
			if (chunkAtCoords4 == null)
			{
				num++;
			}
			if (chunkAtCoords2 == null)
			{
				num2++;
			}
			if (chunkAtCoords5 == null)
			{
				num3--;
			}
			if (chunkAtCoords7 == null)
			{
				num4--;
			}
			for (int i = 0; i < 16; i++)
			{
				if (i % 2 == 0 != even)
				{
					continue;
				}
				TerrainChunkSliceGeometry terrainChunkSliceGeometry = chunk.Geometry.Slices[i];
				chunk.SliceContentsHashes[i] = CalculateChunkSliceContentsHash(chunk, i);
				if (terrainChunkSliceGeometry.ContentsHash != 0 && terrainChunkSliceGeometry.ContentsHash == chunk.SliceContentsHashes[i])
				{
					m_statistics.SkippedSlices++;
					continue;
				}
				m_statistics.GeneratedSlices++;
				TerrainGeometrySubset[] subsets = terrainChunkSliceGeometry.Subsets;
				foreach (TerrainGeometrySubset obj in subsets)
				{
					obj.Vertices.Clear();
					obj.Indices.Clear();
				}
				for (int k = num; k < num3; k++)
				{
					for (int l = num2; l < num4; l++)
					{
						switch (k)
						{
						case 0:
							if ((l == 0 && chunkAtCoords == null) || (l == 15 && chunkAtCoords6 == null))
							{
								continue;
							}
							break;
						case 15:
							if ((l == 0 && chunkAtCoords3 == null) || (l == 15 && chunkAtCoords8 == null))
							{
								continue;
							}
							break;
						}
						int num5 = k + chunk.Origin.X;
						int num6 = l + chunk.Origin.Y;
						int bottomHeightFast = chunk.GetBottomHeightFast(k, l);
						int bottomHeight = m_terrain.GetBottomHeight(num5 - 1, num6);
						int bottomHeight2 = m_terrain.GetBottomHeight(num5 + 1, num6);
						int bottomHeight3 = m_terrain.GetBottomHeight(num5, num6 - 1);
						int bottomHeight4 = m_terrain.GetBottomHeight(num5, num6 + 1);
						int x = MathUtils.Min(bottomHeightFast - 1, MathUtils.Min(bottomHeight, bottomHeight2, bottomHeight3, bottomHeight4));
						int x2 = chunk.GetTopHeightFast(k, l) + 1;
						int num7 = MathUtils.Max(16 * i, x, 1);
						int num8 = MathUtils.Min(16 * (i + 1), x2, 255);
						int num9 = TerrainChunk.CalculateCellIndex(k, 0, l);
						for (int m = num7; m < num8; m++)
						{
							int cellValueFast = chunk.GetCellValueFast(num9 + m);
							int num10 = Terrain.ExtractContents(cellValueFast);
							if (num10 != 0)
							{
								BlocksManager.Blocks[num10].GenerateTerrainVertices(m_subsystemTerrain.BlockGeometryGenerator, chunk.Geometry.Slices[i], cellValueFast, num5, m, num6);
							}
						}
					}
				}
			}
		}

		public static int CalculateLightPropagationBitIndex(int x, int z)
		{
			return x + 1 + 3 * (z + 1);
		}

		public void UpdateNeighborsLightPropagationBitmasks(TerrainChunk chunk)
		{
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					TerrainChunk chunkAtCoords = m_terrain.GetChunkAtCoords(chunk.Coords.X + i, chunk.Coords.Y + j);
					if (chunkAtCoords != null)
					{
						int num = CalculateLightPropagationBitIndex(-i, -j);
						chunkAtCoords.LightPropagationMask |= 1 << num;
					}
				}
			}
		}

		public int CalculateChunkSliceContentsHash(TerrainChunk chunk, int sliceIndex)
		{
			double realTime = Time.RealTime;
			int num = 1;
			int num2 = chunk.Origin.X - 1;
			int num3 = chunk.Origin.X + 16 + 1;
			int num4 = chunk.Origin.Y - 1;
			int num5 = chunk.Origin.Y + 16 + 1;
			int x = MathUtils.Max(16 * sliceIndex - 1, 0);
			int x2 = MathUtils.Min(16 * (sliceIndex + 1) + 1, 256);
			for (int i = num2; i < num3; i++)
			{
				for (int j = num4; j < num5; j++)
				{
					TerrainChunk chunkAtCell = m_terrain.GetChunkAtCell(i, j);
					if (chunkAtCell != null)
					{
						int x3 = i & 0xF;
						int z = j & 0xF;
						int shaftValueFast = chunkAtCell.GetShaftValueFast(x3, z);
						int num6 = Terrain.ExtractBottomHeight(shaftValueFast);
						int num7 = Terrain.ExtractTopHeight(shaftValueFast);
						int num8 = MathUtils.Max(x, num6 - 1);
						int num9 = MathUtils.Min(x2, num7 + 2);
						int num10 = TerrainChunk.CalculateCellIndex(x3, num8, z);
						int num11 = num10 + num9 - num8;
						while (num10 < num11)
						{
							num += chunkAtCell.GetCellValueFast(num10++);
							num *= 31;
						}
						num += Terrain.ExtractTemperature(shaftValueFast);
						num *= 31;
						num += Terrain.ExtractHumidity(shaftValueFast);
						num *= 31;
						num += num8;
						num *= 31;
					}
				}
			}
			num += m_terrain.SeasonTemperature;
			num *= 31;
			num += m_terrain.SeasonHumidity;
			num *= 31;
			double realTime2 = Time.RealTime;
			m_statistics.HashCount++;
			m_statistics.HashTime += realTime2 - realTime;
			return num;
		}

		public void NotifyBlockBehaviors(TerrainChunk chunk)
		{
			foreach (SubsystemBlockBehavior blockBehavior in m_subsystemBlockBehaviors.BlockBehaviors)
			{
				blockBehavior.OnChunkInitialized(chunk);
			}
			bool isLoaded = chunk.IsLoaded;
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					int x = i + chunk.Origin.X;
					int z = j + chunk.Origin.Y;
					int num = TerrainChunk.CalculateCellIndex(i, 0, j);
					int num2 = 0;
					while (num2 < 255)
					{
						int cellValueFast = chunk.GetCellValueFast(num);
						int num3 = Terrain.ExtractContents(cellValueFast);
						if (num3 != 0)
						{
							SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num3);
							for (int k = 0; k < blockBehaviors.Length; k++)
							{
								blockBehaviors[k].OnBlockGenerated(cellValueFast, x, num2, z, isLoaded);
							}
						}
						num2++;
						num++;
					}
				}
			}
		}

		public void UnpauseUpdateThread()
		{
			lock (m_unpauseLock)
			{
				m_unpauseUpdateThread = true;
				m_pauseEvent.Set();
			}
		}

		public void SettingsManager_SettingChanged(string name)
		{
			if (name == "Brightness")
			{
				DowngradeAllChunksState(TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: true);
			}
		}
	}
}
