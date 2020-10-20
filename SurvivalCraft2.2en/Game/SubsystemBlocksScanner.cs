using Engine;
using GameEntitySystem;
using System;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemBlocksScanner : Subsystem, IUpdateable
	{
		public const float ScanPeriod = 60f;

		public SubsystemPollableBlockBehavior[][] m_pollableBehaviorsByContents;

		public Point2 m_pollChunkCoordinates;

		public int m_pollX;

		public int m_pollZ;

		public int m_pollPass;

		public float m_pollCount;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public UpdateOrder UpdateOrder => UpdateOrder.BlocksScanner;

		public event Action<TerrainChunk> ScanningChunkCompleted;

		public void Update(float dt)
		{
			Terrain terrain = m_subsystemTerrain.Terrain;
			m_pollCount += (float)(terrain.AllocatedChunks.Length * 16 * 16) * dt / 60f;
			m_pollCount = MathUtils.Clamp(m_pollCount, 0f, 200f);
			TerrainChunk nextChunk = terrain.GetNextChunk(m_pollChunkCoordinates.X, m_pollChunkCoordinates.Y);
			if (nextChunk == null)
			{
				return;
			}
			while (m_pollCount >= 1f)
			{
				if (nextChunk.State <= TerrainChunkState.InvalidContents4)
				{
					m_pollCount -= 65536f;
				}
				else
				{
					while (m_pollX < 16)
					{
						while (m_pollZ < 16)
						{
							if (m_pollCount < 1f)
							{
								return;
							}
							m_pollCount -= 1f;
							int topHeightFast = nextChunk.GetTopHeightFast(m_pollX, m_pollZ);
							int num = TerrainChunk.CalculateCellIndex(m_pollX, 0, m_pollZ);
							int num2 = 0;
							while (num2 <= topHeightFast)
							{
								int cellValueFast = nextChunk.GetCellValueFast(num);
								int num3 = Terrain.ExtractContents(cellValueFast);
								if (num3 != 0)
								{
									SubsystemPollableBlockBehavior[] array = m_pollableBehaviorsByContents[num3];
									for (int i = 0; i < array.Length; i++)
									{
										array[i].OnPoll(cellValueFast, nextChunk.Origin.X + m_pollX, num2, nextChunk.Origin.Y + m_pollZ, m_pollPass);
									}
								}
								num2++;
								num++;
							}
							m_pollZ++;
						}
						m_pollZ = 0;
						m_pollX++;
					}
					m_pollX = 0;
				}
				this.ScanningChunkCompleted?.Invoke(nextChunk);
				nextChunk = terrain.GetNextChunk(nextChunk.Coords.X + 1, nextChunk.Coords.Y);
				if (nextChunk == null)
				{
					break;
				}
				if (Terrain.ComparePoints(nextChunk.Coords, m_pollChunkCoordinates) < 0)
				{
					m_pollPass++;
				}
				m_pollChunkCoordinates = nextChunk.Coords;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_pollChunkCoordinates = valuesDictionary.GetValue<Point2>("PollChunkCoordinates");
			Point2 value = valuesDictionary.GetValue<Point2>("PollPoint");
			m_pollX = value.X;
			m_pollZ = value.Y;
			m_pollPass = valuesDictionary.GetValue<int>("PollPass");
			m_pollableBehaviorsByContents = new SubsystemPollableBlockBehavior[BlocksManager.Blocks.Length][];
			for (int i = 0; i < m_pollableBehaviorsByContents.Length; i++)
			{
				m_pollableBehaviorsByContents[i] = (from s in m_subsystemBlockBehaviors.GetBlockBehaviors(i)
					where s is SubsystemPollableBlockBehavior
					select (SubsystemPollableBlockBehavior)s).ToArray();
			}
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			valuesDictionary.SetValue("PollChunkCoordinates", m_pollChunkCoordinates);
			valuesDictionary.SetValue("PollPoint", new Point2(m_pollX, m_pollZ));
			valuesDictionary.SetValue("PollPass", m_pollPass);
		}
	}
}
