using Engine;
using Engine.Audio;
using GameEntitySystem;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemWeather : Subsystem, IDrawable, IUpdateable
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemBlocksScanner m_subsystemBlocksScanner;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemAudio m_subsystemAudio;

		public Random m_random = new Random();

		public Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>> m_activeShafts = new Dictionary<GameWidget, Dictionary<Point2, PrecipitationShaftParticleSystem>>();

		public List<PrecipitationShaftParticleSystem> m_toRemove = new List<PrecipitationShaftParticleSystem>();

		public Dictionary<GameWidget, Vector2?> m_lastShaftsUpdatePositions = new Dictionary<GameWidget, Vector2?>();

		public float m_targetRainSoundVolume;

		public double m_precipitationStartTime;

		public double m_precipitationEndTime;

		public float m_lightningIntensity;

		public const int m_rainSoundRadius = 5;

		public float m_rainVolumeFactor;

		public Sound m_rainSound;

		public static int[] m_drawOrders = new int[1]
		{
			50
		};

		public SubsystemTerrain SubsystemTerrain
		{
			get;
			set;
		}

		public SubsystemSky SubsystemSky
		{
			get;
			set;
		}

		public SubsystemTime SubsystemTime
		{
			get;
			set;
		}

		public RainSplashParticleSystem RainSplashParticleSystem
		{
			get;
			set;
		}

		public SnowSplashParticleSystem SnowSplashParticleSystem
		{
			get;
			set;
		}

		public Color RainColor
		{
			get;
			set;
		}

		public Color SnowColor
		{
			get;
			set;
		}

		public float GlobalPrecipitationIntensity
		{
			get;
			set;
		}

		public int[] DrawOrders => m_drawOrders;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public PrecipitationShaftInfo GetPrecipitationShaftInfo(int x, int z)
		{
			int shaftValue = SubsystemTerrain.Terrain.GetShaftValue(x, z);
			int seasonalTemperature = SubsystemTerrain.Terrain.GetSeasonalTemperature(shaftValue);
			int num = Terrain.ExtractTopHeight(shaftValue);
			PrecipitationShaftInfo result;
			if (IsPlaceFrozen(seasonalTemperature, num))
			{
				result = default(PrecipitationShaftInfo);
				result.Intensity = GlobalPrecipitationIntensity;
				result.Type = PrecipitationType.Snow;
				result.YLimit = num + 1;
				return result;
			}
			int seasonalHumidity = SubsystemTerrain.Terrain.GetSeasonalHumidity(shaftValue);
			if (seasonalTemperature <= 8 || seasonalHumidity >= 8)
			{
				result = default(PrecipitationShaftInfo);
				result.Intensity = GlobalPrecipitationIntensity;
				result.Type = PrecipitationType.Rain;
				result.YLimit = num + 1;
				return result;
			}
			result = default(PrecipitationShaftInfo);
			result.Intensity = 0f;
			result.Type = PrecipitationType.Rain;
			result.YLimit = num + 1;
			return result;
		}

		public void ManualLightingStrike(Vector3 position, Vector3 direction)
		{
			int num = Terrain.ToCell(position.X + direction.X * 32f);
			int num2 = Terrain.ToCell(position.Z + direction.Z * 32f);
			Vector3? vector = null;
			for (int i = 0; i < 300; i++)
			{
				int num3 = m_random.Int(-8, 8);
				int num4 = m_random.Int(-8, 8);
				int num5 = num + num3;
				int num6 = num2 + num4;
				int num7 = SubsystemTerrain.Terrain.CalculateTopmostCellHeight(num5, num6);
				if (!vector.HasValue || (float)num7 > vector.Value.Y)
				{
					vector = new Vector3(num5, num7, num6);
				}
			}
			if (vector.HasValue)
			{
				SubsystemSky.MakeLightningStrike(vector.Value);
			}
		}

		public static int GetTemperatureAdjustmentAtHeight(int y)
		{
			return (int)MathUtils.Round((y > 64) ? (-0.0008f * (float)MathUtils.Sqr(y - 64)) : (0.1f * (float)(64 - y)));
		}

		public static bool IsPlaceFrozen(int temperature, int y)
		{
			return temperature + GetTemperatureAdjustmentAtHeight(y) <= 0;
		}

		public static bool ShaftHasSnowOnIce(int x, int z)
		{
			return MathUtils.Hash((uint)((x & 0xFFFF) | (z << 16))) > 429496729;
		}

		public void Draw(Camera camera, int drawOrder)
		{
			int num = (SettingsManager.VisibilityRange > 128) ? 9 : ((SettingsManager.VisibilityRange <= 64) ? 7 : 8);
			int num2 = num * num;
			Dictionary<Point2, PrecipitationShaftParticleSystem> activeShafts = GetActiveShafts(camera.GameWidget);
			byte b = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
			RainColor = new Color(b, b, b);
			byte b2 = (byte)(255f * MathUtils.Lerp(0.15f, 1f, SubsystemSky.SkyLightIntensity));
			SnowColor = new Color(b2, b2, b2);
			Vector2 vector = new Vector2(camera.ViewPosition.X, camera.ViewPosition.Z);
			Point2 point = Terrain.ToCell(vector);
			Vector2? value = null;
			m_lastShaftsUpdatePositions.TryGetValue(camera.GameWidget, out value);
			if (value.HasValue && !(Vector2.DistanceSquared(value.Value, vector) > 1f))
			{
				return;
			}
			m_lastShaftsUpdatePositions[camera.GameWidget] = vector;
			m_toRemove.Clear();
			foreach (PrecipitationShaftParticleSystem value2 in activeShafts.Values)
			{
				if (MathUtils.Sqr((float)value2.Point.X + 0.5f - vector.X) + MathUtils.Sqr((float)value2.Point.Y + 0.5f - vector.Y) > (float)num2 + 1f)
				{
					m_toRemove.Add(value2);
				}
			}
			foreach (PrecipitationShaftParticleSystem item in m_toRemove)
			{
				if (m_subsystemParticles.ContainsParticleSystem(item))
				{
					m_subsystemParticles.RemoveParticleSystem(item);
				}
				activeShafts.Remove(item.Point);
			}
			for (int i = point.X - num; i <= point.X + num; i++)
			{
				for (int j = point.Y - num; j <= point.Y + num; j++)
				{
					if (MathUtils.Sqr((float)i + 0.5f - vector.X) + MathUtils.Sqr((float)j + 0.5f - vector.Y) <= (float)num2)
					{
						Point2 point2 = new Point2(i, j);
						if (!activeShafts.ContainsKey(point2))
						{
							PrecipitationShaftParticleSystem precipitationShaftParticleSystem = new PrecipitationShaftParticleSystem(camera.GameWidget, this, m_random, point2, GetPrecipitationShaftInfo(point2.X, point2.Y).Type);
							m_subsystemParticles.AddParticleSystem(precipitationShaftParticleSystem);
							activeShafts.Add(point2, precipitationShaftParticleSystem);
						}
					}
				}
			}
		}

		public void Update(float dt)
		{
			if (m_subsystemGameInfo.TotalElapsedGameTime > m_precipitationEndTime)
			{
				if (m_precipitationEndTime == 0.0)
				{
					if (m_subsystemGameInfo.WorldSettings.StartingPositionMode == StartingPositionMode.Hard)
					{
						m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(2f, 3f));
						m_lightningIntensity = m_random.Float(0.5f, 1f);
					}
					else
					{
						m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(3f, 6f));
						m_lightningIntensity = m_random.Float(0.33f, 0.66f);
					}
				}
				else
				{
					m_precipitationStartTime = m_subsystemGameInfo.TotalElapsedGameTime + (double)(60f * m_random.Float(5f, 45f));
					m_lightningIntensity = ((m_random.Float(0f, 1f) < 0.5f) ? m_random.Float(0.33f, 1f) : 0f);
				}
				m_precipitationEndTime = m_precipitationStartTime + (double)(60f * m_random.Float(3f, 6f));
			}
			float num = (float)MathUtils.Max(0.0, MathUtils.Min(m_subsystemGameInfo.TotalElapsedGameTime - m_precipitationStartTime, m_precipitationEndTime - m_subsystemGameInfo.TotalElapsedGameTime));
			GlobalPrecipitationIntensity = (m_subsystemGameInfo.WorldSettings.AreWeatherEffectsEnabled ? MathUtils.Saturate(num * 0.04f) : 0f);
			if (GlobalPrecipitationIntensity == 1f && SubsystemTime.PeriodicGameTimeEvent(1.0, 0.0))
			{
				TerrainChunk[] allocatedChunks = SubsystemTerrain.Terrain.AllocatedChunks;
				for (int i = 0; i < allocatedChunks.Length; i++)
				{
					TerrainChunk terrainChunk = allocatedChunks[m_random.Int(0, allocatedChunks.Length - 1)];
					if (terrainChunk.State < TerrainChunkState.InvalidVertices1 || !m_random.Bool(m_lightningIntensity * 0.0002f))
					{
						continue;
					}
					int num2 = terrainChunk.Origin.X + m_random.Int(0, 15);
					int num3 = terrainChunk.Origin.Y + m_random.Int(0, 15);
					Vector3? vector = null;
					for (int j = num2 - 8; j < num2 + 8; j++)
					{
						for (int k = num3 - 8; k < num3 + 8; k++)
						{
							int topHeight = SubsystemTerrain.Terrain.GetTopHeight(j, k);
							if (!vector.HasValue || (float)topHeight > vector.Value.Y)
							{
								vector = new Vector3(j, topHeight, k);
							}
						}
					}
					if (vector.HasValue)
					{
						SubsystemSky.MakeLightningStrike(vector.Value);
						return;
					}
				}
			}
			if (Time.PeriodicEvent(0.5, 0.0))
			{
				float num4 = 0f;
				if (GlobalPrecipitationIntensity > 0f)
				{
					float num5 = 0f;
					foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
					{
						int num6 = Terrain.ToCell(listenerPosition.X) - 5;
						int num7 = Terrain.ToCell(listenerPosition.Z) - 5;
						int num8 = Terrain.ToCell(listenerPosition.X) + 5;
						int num9 = Terrain.ToCell(listenerPosition.Z) + 5;
						Vector3 vector2 = default(Vector3);
						for (int l = num6; l <= num8; l++)
						{
							for (int m = num7; m <= num9; m++)
							{
								PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(l, m);
								if (precipitationShaftInfo.Type == PrecipitationType.Rain && precipitationShaftInfo.Intensity > 0f)
								{
									vector2.X = (float)l + 0.5f;
									vector2.Y = MathUtils.Max(precipitationShaftInfo.YLimit, listenerPosition.Y);
									vector2.Z = (float)m + 0.5f;
									float num10 = vector2.X - listenerPosition.X;
									float num11 = 8f * (vector2.Y - listenerPosition.Y);
									float num12 = vector2.Z - listenerPosition.Z;
									float distance = MathUtils.Sqrt(num10 * num10 + num11 * num11 + num12 * num12);
									num5 += m_subsystemAudio.CalculateVolume(distance, 1.5f) * precipitationShaftInfo.Intensity;
								}
							}
						}
					}
					num4 = MathUtils.Max(num4, num5);
				}
				m_targetRainSoundVolume = MathUtils.Saturate(1.5f * num4 / m_rainVolumeFactor);
			}
			m_rainSound.Volume = MathUtils.Saturate(MathUtils.Lerp(m_rainSound.Volume, SettingsManager.SoundsVolume * m_targetRainSoundVolume, 5f * dt));
			if (m_rainSound.Volume > AudioManager.MinAudibleVolume)
			{
				m_rainSound.Play();
			}
			else
			{
				m_rainSound.Pause();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			SubsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBlocksScanner = base.Project.FindSubsystem<SubsystemBlocksScanner>(throwOnError: true);
			SubsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			SubsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_precipitationStartTime = valuesDictionary.GetValue<double>("WeatherStartTime");
			m_precipitationEndTime = valuesDictionary.GetValue<double>("WeatherEndTime");
			m_lightningIntensity = valuesDictionary.GetValue<float>("LightningIntensity");
			m_rainSound = m_subsystemAudio.CreateSound("Audio/Rain");
			m_rainSound.IsLooped = true;
			m_rainSound.Volume = 0f;
			RainSplashParticleSystem = new RainSplashParticleSystem();
			m_subsystemParticles.AddParticleSystem(RainSplashParticleSystem);
			SnowSplashParticleSystem = new SnowSplashParticleSystem();
			m_subsystemParticles.AddParticleSystem(SnowSplashParticleSystem);
			m_rainVolumeFactor = 0f;
			for (int i = -5; i <= 5; i++)
			{
				for (int j = -5; j <= 5; j++)
				{
					float distance = MathUtils.Sqrt(i * i + j * j);
					m_rainVolumeFactor += m_subsystemAudio.CalculateVolume(distance, 1.5f);
				}
			}
			m_subsystemBlocksScanner.ScanningChunkCompleted += delegate(TerrainChunk chunk)
			{
				if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
				{
					FreezeThawAndDepositSnow(chunk);
				}
			};
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			valuesDictionary.SetValue("WeatherStartTime", m_precipitationStartTime);
			valuesDictionary.SetValue("WeatherEndTime", m_precipitationEndTime);
			valuesDictionary.SetValue("LightningIntensity", m_lightningIntensity);
		}

		public Dictionary<Point2, PrecipitationShaftParticleSystem> GetActiveShafts(GameWidget gameWidget)
		{
			if (!m_activeShafts.TryGetValue(gameWidget, out Dictionary<Point2, PrecipitationShaftParticleSystem> value))
			{
				value = new Dictionary<Point2, PrecipitationShaftParticleSystem>();
				m_activeShafts.Add(gameWidget, value);
			}
			return value;
		}

		public void FreezeThawAndDepositSnow(TerrainChunk chunk)
		{
			Terrain terrain = SubsystemTerrain.Terrain;
			for (int i = 0; i < 16; i++)
			{
				for (int j = 0; j < 16; j++)
				{
					if (m_random.Int() % 2 == 0)
					{
						continue;
					}
					int topHeightFast = chunk.GetTopHeightFast(i, j);
					int cellValueFast = chunk.GetCellValueFast(i, topHeightFast, j);
					int num = Terrain.ExtractContents(cellValueFast);
					int num2 = chunk.Origin.X + i;
					int num3 = topHeightFast;
					int num4 = chunk.Origin.Y + j;
					PrecipitationShaftInfo precipitationShaftInfo = GetPrecipitationShaftInfo(num2, num4);
					if (precipitationShaftInfo.Type == PrecipitationType.Snow)
					{
						if (num == 18)
						{
							int cellContents = terrain.GetCellContents(num2 + 1, num3, num4);
							int cellContents2 = terrain.GetCellContents(num2 - 1, num3, num4);
							int cellContents3 = terrain.GetCellContents(num2, num3, num4 - 1);
							int cellContents4 = terrain.GetCellContents(num2, num3, num4 + 1);
							bool num5 = cellContents != 18 && cellContents != 0;
							bool flag = cellContents2 != 18 && cellContents2 != 0;
							bool flag2 = cellContents3 != 18 && cellContents3 != 0;
							bool flag3 = cellContents4 != 18 && cellContents4 != 0;
							if (num5 | flag | flag2 | flag3)
							{
								SubsystemTerrain.ChangeCell(num2, num3, num4, Terrain.MakeBlockValue(62));
							}
						}
						else if (precipitationShaftInfo.Intensity > 0.5f && SubsystemSnowBlockBehavior.CanSupportSnow(cellValueFast) && (num != 62 || ShaftHasSnowOnIce(num2, num4)) && num3 + 1 < 255)
						{
							SubsystemTerrain.ChangeCell(num2, num3 + 1, num4, Terrain.MakeBlockValue(61));
						}
					}
					else
					{
						switch (num)
						{
						case 61:
							SubsystemTerrain.DestroyCell(0, num2, num3, num4, 0, noDrop: true, noParticleSystem: true);
							break;
						case 62:
							SubsystemTerrain.DestroyCell(0, num2, num3, num4, 0, noDrop: false, noParticleSystem: true);
							break;
						}
					}
				}
			}
		}
	}
}
