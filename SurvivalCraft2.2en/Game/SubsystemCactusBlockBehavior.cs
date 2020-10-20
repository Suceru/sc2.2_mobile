using Engine;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemCactusBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

		public Random m_random = new Random();

		public override int[] HandledBlocks => new int[1]
		{
			127
		};

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
			if (cellContents != 7 && cellContents != 127)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}

		public override void OnPoll(int value, int x, int y, int z, int pollPass)
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0)
			{
				return;
			}
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
			if (Terrain.ExtractContents(cellValue) == 0 && Terrain.ExtractLight(cellValue) >= 12)
			{
				int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
				int cellContents2 = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 2, z);
				if ((cellContents != 127 || cellContents2 != 127) && m_random.Float(0f, 1f) < 0.25f)
				{
					m_toUpdate[new Point3(x, y + 1, z)] = Terrain.MakeBlockValue(127, 0, 0);
				}
			}
		}

		public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
		{
			componentBody.Entity.FindComponent<ComponentCreature>()?.ComponentHealth.Injure(0.01f * MathUtils.Abs(velocity), null, ignoreInvulnerability: false, "Spiked by cactus");
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			base.Load(valuesDictionary);
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.PeriodicGameTimeEvent(60.0, 0.0))
			{
				foreach (KeyValuePair<Point3, int> item in m_toUpdate)
				{
					if (base.SubsystemTerrain.Terrain.GetCellContents(item.Key.X, item.Key.Y, item.Key.Z) == 0)
					{
						base.SubsystemTerrain.ChangeCell(item.Key.X, item.Key.Y, item.Key.Z, item.Value);
					}
				}
				m_toUpdate.Clear();
			}
		}
	}
}
