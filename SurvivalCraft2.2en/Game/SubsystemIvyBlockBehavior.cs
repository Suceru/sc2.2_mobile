using Engine;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemIvyBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public Random m_random = new Random();

		public Dictionary<Point3, int> m_toUpdate = new Dictionary<Point3, int>();

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override int[] HandledBlocks => new int[0];

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

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			int face = IvyBlock.GetFace(Terrain.ExtractData(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
			bool flag = false;
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y + 1, z);
			if (Terrain.ExtractContents(cellValue) == 197 && IvyBlock.GetFace(Terrain.ExtractData(cellValue)) == face)
			{
				flag = true;
			}
			if (!flag)
			{
				Point3 point = CellFace.FaceToPoint3(face);
				int cellValue2 = base.SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
				if (!BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)].IsCollidable)
				{
					base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: true, noParticleSystem: false);
				}
			}
		}

		public override void OnPoll(int value, int x, int y, int z, int pollPass)
		{
			if (m_random.Float(0f, 1f) < 0.5f && !IvyBlock.IsGrowthStopCell(x, y, z) && Terrain.ExtractContents(base.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)) == 0)
			{
				m_toUpdate[new Point3(x, y - 1, z)] = value;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
		}
	}
}
