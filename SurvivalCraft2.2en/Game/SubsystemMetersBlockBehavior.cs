using Engine;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemMetersBlockBehavior : SubsystemBlockBehavior, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemWeather m_subsystemWeather;

		public SubsystemSky m_subsystemSky;

		public Dictionary<Point3, int> m_thermometersByPoint = new Dictionary<Point3, int>();

		public DynamicArray<Point3> m_thermometersToSimulate = new DynamicArray<Point3>();

		public int m_thermometersToSimulateIndex;

		public const int m_diameterBits = 6;

		public const int m_diameter = 64;

		public const int m_diameterMask = 63;

		public const int m_radius = 32;

		public DynamicArray<int> m_toVisit = new DynamicArray<int>();

		public int[] m_visited = new int[8192];

		public override int[] HandledBlocks => new int[2]
		{
			120,
			121
		};

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			Point3 point = CellFace.FaceToPoint3(Terrain.ExtractData(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
			int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
			if (BlocksManager.Blocks[cellContents].IsTransparent)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}

		public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
		{
			AddMeter(value, x, y, z);
		}

		public override void OnBlockRemoved(int value, int oldValue, int x, int y, int z)
		{
			RemoveMeter(oldValue, x, y, z);
		}

		public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
		{
			RemoveMeter(oldValue, x, y, z);
			AddMeter(value, x, y, z);
		}

		public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
		{
			AddMeter(value, x, y, z);
		}

		public override void OnChunkDiscarding(TerrainChunk chunk)
		{
			List<Point3> list = new List<Point3>();
			foreach (Point3 key in m_thermometersByPoint.Keys)
			{
				if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
				{
					list.Add(key);
				}
			}
			foreach (Point3 item in list)
			{
				m_thermometersByPoint.Remove(item);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
		}

		public void Update(float dt)
		{
			if (m_thermometersToSimulateIndex < m_thermometersToSimulate.Count)
			{
				double period = MathUtils.Max(5.0 / (double)m_thermometersToSimulate.Count, 1.0);
				if (m_subsystemTime.PeriodicGameTimeEvent(period, 0.0))
				{
					Point3 point = m_thermometersToSimulate.Array[m_thermometersToSimulateIndex];
					SimulateThermometer(point.X, point.Y, point.Z, invalidateTerrainOnChange: true);
					m_thermometersToSimulateIndex++;
				}
			}
			else if (m_thermometersByPoint.Count > 0)
			{
				m_thermometersToSimulateIndex = 0;
				m_thermometersToSimulate.Clear();
				m_thermometersToSimulate.AddRange(m_thermometersByPoint.Keys);
			}
		}

		public int GetThermometerReading(int x, int y, int z)
		{
			int value = 0;
			m_thermometersByPoint.TryGetValue(new Point3(x, y, z), out value);
			return value;
		}

		public void CalculateTemperature(int x, int y, int z, float meterTemperature, float meterInsulation, out float temperature, out float temperatureFlux)
		{
			m_toVisit.Clear();
			for (int i = 0; i < m_visited.Length; i++)
			{
				m_visited[i] = 0;
			}
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			float num6 = 0f;
			m_toVisit.Add(133152);
			for (int j = 0; j < m_toVisit.Count; j++)
			{
				int num7 = m_toVisit.Array[j];
				if ((m_visited[num7 / 32] & (1 << num7)) != 0)
				{
					continue;
				}
				m_visited[num7 / 32] |= 1 << num7;
				int num8 = (num7 & 0x3F) - 32;
				int num9 = ((num7 >> 6) & 0x3F) - 32;
				int num10 = ((num7 >> 12) & 0x3F) - 32;
				int num11 = num8 + x;
				int num12 = num9 + y;
				int num13 = num10 + z;
				Terrain terrain = base.SubsystemTerrain.Terrain;
				TerrainChunk chunkAtCell = terrain.GetChunkAtCell(num11, num13);
				if (chunkAtCell == null || num12 < 0 || num12 >= 256)
				{
					continue;
				}
				int x2 = num11 & 0xF;
				int y2 = num12;
				int z2 = num13 & 0xF;
				int cellValueFast = chunkAtCell.GetCellValueFast(x2, y2, z2);
				int num14 = Terrain.ExtractContents(cellValueFast);
				Block block = BlocksManager.Blocks[num14];
				float heat = GetHeat(cellValueFast);
				if (heat > 0f)
				{
					int num15 = MathUtils.Abs(num8) + MathUtils.Abs(num9) + MathUtils.Abs(num10);
					int num16 = (num15 <= 0) ? 1 : (4 * num15 * num15 + 2);
					float num17 = 1f / (float)num16;
					num5 += num17 * 36f * heat;
					num6 += num17;
				}
				else if (block.IsHeatBlocker(cellValueFast))
				{
					int num18 = MathUtils.Abs(num8) + MathUtils.Abs(num9) + MathUtils.Abs(num10);
					int num19 = (num18 <= 0) ? 1 : (4 * num18 * num18 + 2);
					float num20 = 1f / (float)num19;
					float num21 = terrain.SeasonTemperature;
					float num22 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
					float num23 = (block is WaterBlock) ? (MathUtils.Max((float)chunkAtCell.GetTemperatureFast(x2, z2) + num21 - 6f, 0f) + num22) : ((!(block is IceBlock)) ? ((float)chunkAtCell.GetTemperatureFast(x2, z2) + num21 + num22) : (0f + num21 + num22));
					num += num20 * num23;
					num2 += num20;
				}
				else if (y >= chunkAtCell.GetTopHeightFast(x2, z2))
				{
					int num24 = MathUtils.Abs(num8) + MathUtils.Abs(num9) + MathUtils.Abs(num10);
					int num25 = (num24 <= 0) ? 1 : (4 * num24 * num24 + 2);
					float num26 = 1f / (float)num25;
					PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
					float num27 = terrain.SeasonTemperature;
					float num28 = (y >= precipitationShaftInfo.YLimit) ? MathUtils.Lerp(0f, -2f, precipitationShaftInfo.Intensity) : 0f;
					float num29 = MathUtils.Lerp(-6f, 0f, m_subsystemSky.SkyLightIntensity);
					float num30 = SubsystemWeather.GetTemperatureAdjustmentAtHeight(y2);
					num3 += num26 * ((float)chunkAtCell.GetTemperatureFast(x2, z2) + num27 + num28 + num29 + num30);
					num4 += num26;
				}
				else if (m_toVisit.Count < 4090)
				{
					if (num8 > -30)
					{
						m_toVisit.Add(num7 - 1);
					}
					if (num8 < 30)
					{
						m_toVisit.Add(num7 + 1);
					}
					if (num9 > -30)
					{
						m_toVisit.Add(num7 - 64);
					}
					if (num9 < 30)
					{
						m_toVisit.Add(num7 + 64);
					}
					if (num10 > -30)
					{
						m_toVisit.Add(num7 - 4096);
					}
					if (num10 < 30)
					{
						m_toVisit.Add(num7 + 4096);
					}
				}
			}
			float num31 = 0f;
			for (int k = -7; k <= 7; k++)
			{
				for (int l = -7; l <= 7; l++)
				{
					TerrainChunk chunkAtCell2 = base.SubsystemTerrain.Terrain.GetChunkAtCell(x + k, z + l);
					if (chunkAtCell2 == null || chunkAtCell2.State < TerrainChunkState.InvalidVertices1)
					{
						continue;
					}
					for (int m = -7; m <= 7; m++)
					{
						int num32 = k * k + m * m + l * l;
						if (num32 > 49 || num32 <= 0)
						{
							continue;
						}
						int x3 = (x + k) & 0xF;
						int num33 = y + m;
						int z3 = (z + l) & 0xF;
						if (num33 >= 0 && num33 < 256)
						{
							float heat2 = GetHeat(chunkAtCell2.GetCellValueFast(x3, num33, z3));
							if (heat2 > 0f && !base.SubsystemTerrain.Raycast(new Vector3(x, y, z) + new Vector3(0.5f, 0.75f, 0.5f), new Vector3(x + k, y + m, z + l) + new Vector3(0.5f, 0.75f, 0.5f), useInteractionBoxes: false, skipAirBlocks: true, delegate(int raycastValue, float d)
							{
								Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(raycastValue)];
								return block2.IsCollidable && !block2.IsTransparent;
							}).HasValue)
							{
								num31 += heat2 * 3f / (float)(num32 + 2);
							}
						}
					}
				}
			}
			float num34 = 0f;
			float num35 = 0f;
			if (num31 > 0f)
			{
				float num36 = 3f * num31;
				num34 += 35f * num36;
				num35 += num36;
			}
			if (num2 > 0f)
			{
				float num37 = 1f;
				num34 += num / num2 * num37;
				num35 += num37;
			}
			if (num4 > 0f)
			{
				float num38 = 4f * MathUtils.Pow(num4, 0.25f);
				num34 += num3 / num4 * num38;
				num35 += num38;
			}
			if (num6 > 0f)
			{
				float num39 = 1.5f * MathUtils.Pow(num6, 0.25f);
				num34 += num5 / num6 * num39;
				num35 += num39;
			}
			if (meterInsulation > 0f)
			{
				num34 += meterTemperature * meterInsulation;
				num35 += meterInsulation;
			}
			temperature = ((num35 > 0f) ? (num34 / num35) : meterTemperature);
			temperatureFlux = num35 - meterInsulation;
		}

		public static float GetHeat(int value)
		{
			int num = Terrain.ExtractContents(value);
			return BlocksManager.Blocks[num].GetHeat(value);
		}

		public void SimulateThermometer(int x, int y, int z, bool invalidateTerrainOnChange)
		{
			Point3 key = new Point3(x, y, z);
			if (!m_thermometersByPoint.ContainsKey(key))
			{
				return;
			}
			int num = m_thermometersByPoint[key];
			CalculateTemperature(x, y, z, 0f, 0f, out float temperature, out float _);
			int num2 = MathUtils.Clamp((int)MathUtils.Round(temperature), 0, 15);
			if (num2 == num)
			{
				return;
			}
			m_thermometersByPoint[new Point3(x, y, z)] = num2;
			if (invalidateTerrainOnChange)
			{
				TerrainChunk chunkAtCell = base.SubsystemTerrain.Terrain.GetChunkAtCell(x, z);
				if (chunkAtCell != null)
				{
					base.SubsystemTerrain.TerrainUpdater.DowngradeChunkNeighborhoodState(chunkAtCell.Coords, 0, TerrainChunkState.InvalidVertices1, forceGeometryRegeneration: true);
				}
			}
		}

		public void AddMeter(int value, int x, int y, int z)
		{
			if (Terrain.ExtractContents(value) == 120)
			{
				m_thermometersByPoint.Add(new Point3(x, y, z), 0);
				SimulateThermometer(x, y, z, invalidateTerrainOnChange: false);
			}
		}

		public void RemoveMeter(int value, int x, int y, int z)
		{
			m_thermometersByPoint.Remove(new Point3(x, y, z));
		}
	}
}
