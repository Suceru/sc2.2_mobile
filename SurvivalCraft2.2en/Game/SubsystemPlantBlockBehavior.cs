using Engine;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemPlantBlockBehavior : SubsystemPollableBlockBehavior, IUpdateable
	{
		public struct Replacement
		{
			public int RequiredValue;

			public int Value;
		}

		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public Random m_random = new Random();

		public Dictionary<Point3, Replacement> m_toReplace = new Dictionary<Point3, Replacement>();

		public override int[] HandledBlocks => new int[11]
		{
			19,
			20,
			24,
			25,
			28,
			99,
			131,
			244,
			132,
			174,
			204
		};

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			int num = Terrain.ExtractContents(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z));
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z);
			int num2 = Terrain.ExtractContents(cellValue);
			switch (num)
			{
			case 131:
			case 244:
				if (num2 != 8 && num2 != 2 && num2 != 168)
				{
					base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				}
				break;
			case 132:
			{
				Block block = BlocksManager.Blocks[num2];
				if (block.IsFaceTransparent(base.SubsystemTerrain, 4, cellValue) && !(block is FenceBlock))
				{
					base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				}
				break;
			}
			default:
				if (num2 != 8 && num2 != 2 && num2 != 7 && num2 != 168)
				{
					base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				}
				break;
			}
		}

		public override void OnPoll(int value, int x, int y, int z, int pollPass)
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0 || y <= 0 || y >= 255)
			{
				return;
			}
			int num = Terrain.ExtractContents(value);
			Block block = BlocksManager.Blocks[num];
			if (num == 19)
			{
				GrowTallGrass(value, x, y, z, pollPass);
				return;
			}
			if (block is FlowerBlock)
			{
				GrowFlower(value, x, y, z, pollPass);
				return;
			}
			switch (num)
			{
			case 174:
				GrowRye(value, x, y, z, pollPass);
				break;
			case 204:
				GrowCotton(value, x, y, z, pollPass);
				break;
			case 131:
				GrowPumpkin(value, x, y, z, pollPass);
				break;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.PeriodicGameTimeEvent(30.0, 0.0))
			{
				foreach (KeyValuePair<Point3, Replacement> item in m_toReplace)
				{
					Point3 key = item.Key;
					if (Terrain.ReplaceLight(base.SubsystemTerrain.Terrain.GetCellValue(key.X, key.Y, key.Z), 0) == Terrain.ReplaceLight(item.Value.RequiredValue, 0))
					{
						base.SubsystemTerrain.ChangeCell(key.X, key.Y, key.Z, item.Value.Value);
					}
				}
				m_toReplace.Clear();
			}
		}

		public void GrowTallGrass(int value, int x, int y, int z, int pollPass)
		{
			int data = Terrain.ExtractData(value);
			if (TallGrassBlock.GetIsSmall(data) && Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) >= 9)
			{
				int data2 = TallGrassBlock.SetIsSmall(data, isSmall: false);
				int value2 = Terrain.ReplaceData(value, data2);
				m_toReplace[new Point3(x, y, z)] = new Replacement
				{
					Value = value2,
					RequiredValue = value
				};
			}
		}

		public void GrowFlower(int value, int x, int y, int z, int pollPass)
		{
			int data = Terrain.ExtractData(value);
			if (FlowerBlock.GetIsSmall(data) && Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) >= 9)
			{
				int data2 = FlowerBlock.SetIsSmall(data, isSmall: false);
				int value2 = Terrain.ReplaceData(value, data2);
				m_toReplace[new Point3(x, y, z)] = new Replacement
				{
					Value = value2,
					RequiredValue = value
				};
			}
		}

		public void GrowRye(int value, int x, int y, int z, int pollPass)
		{
			if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
			{
				return;
			}
			int data = Terrain.ExtractData(value);
			int size = RyeBlock.GetSize(data);
			if (size == 7)
			{
				return;
			}
			Replacement value3;
			if (RyeBlock.GetIsWild(data))
			{
				if (size < 7)
				{
					int data2 = RyeBlock.SetSize(RyeBlock.SetIsWild(data, isWild: true), size + 1);
					int value2 = Terrain.ReplaceData(value, data2);
					Dictionary<Point3, Replacement> toReplace = m_toReplace;
					Point3 key = new Point3(x, y, z);
					value3 = new Replacement
					{
						Value = value2,
						RequiredValue = value
					};
					toReplace[key] = value3;
				}
				return;
			}
			int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
			if (Terrain.ExtractContents(cellValueFast) == 168)
			{
				int data3 = Terrain.ExtractData(cellValueFast);
				bool hydration = SoilBlock.GetHydration(data3);
				int nitrogen = SoilBlock.GetNitrogen(data3);
				int num = 3;
				float num2 = 0.8f;
				if (nitrogen > 0)
				{
					num--;
					num2 -= 0.4f;
				}
				if (hydration)
				{
					num--;
					num2 -= 0.4f;
				}
				if (pollPass % MathUtils.Max(num, 1) == 0)
				{
					int data4 = RyeBlock.SetSize(data, MathUtils.Min(size + 1, 7));
					if (m_random.Float(0f, 1f) < num2 && size == 3)
					{
						data4 = RyeBlock.SetIsWild(data4, isWild: true);
					}
					int value4 = Terrain.ReplaceData(value, data4);
					value3 = (m_toReplace[new Point3(x, y, z)] = new Replacement
					{
						Value = value4,
						RequiredValue = value
					});
					if (size + 1 == 7)
					{
						int data5 = SoilBlock.SetNitrogen(data3, MathUtils.Max(nitrogen - 1, 0));
						int value5 = Terrain.ReplaceData(cellValueFast, data5);
						Dictionary<Point3, Replacement> toReplace2 = m_toReplace;
						Point3 key2 = new Point3(x, y - 1, z);
						value3 = new Replacement
						{
							Value = value5,
							RequiredValue = cellValueFast
						};
						toReplace2[key2] = value3;
					}
				}
			}
			else
			{
				int value6 = Terrain.ReplaceData(value, RyeBlock.SetIsWild(data, isWild: true));
				Dictionary<Point3, Replacement> toReplace3 = m_toReplace;
				Point3 key3 = new Point3(x, y, z);
				value3 = new Replacement
				{
					Value = value6,
					RequiredValue = value
				};
				toReplace3[key3] = value3;
			}
		}

		public void GrowCotton(int value, int x, int y, int z, int pollPass)
		{
			if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
			{
				return;
			}
			int data = Terrain.ExtractData(value);
			int size = CottonBlock.GetSize(data);
			if (size >= 2)
			{
				return;
			}
			Replacement value3;
			if (CottonBlock.GetIsWild(data))
			{
				if (size < 2)
				{
					int data2 = CottonBlock.SetSize(CottonBlock.SetIsWild(data, isWild: true), size + 1);
					int value2 = Terrain.ReplaceData(value, data2);
					Dictionary<Point3, Replacement> toReplace = m_toReplace;
					Point3 key = new Point3(x, y, z);
					value3 = new Replacement
					{
						Value = value2,
						RequiredValue = value
					};
					toReplace[key] = value3;
				}
				return;
			}
			int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
			if (Terrain.ExtractContents(cellValueFast) == 168)
			{
				int data3 = Terrain.ExtractData(cellValueFast);
				bool hydration = SoilBlock.GetHydration(data3);
				int nitrogen = SoilBlock.GetNitrogen(data3);
				int num = 6;
				float num2 = 0.8f;
				if (nitrogen > 0)
				{
					num -= 2;
					num2 -= 0.4f;
				}
				if (hydration)
				{
					num -= 2;
					num2 -= 0.4f;
				}
				if (pollPass % MathUtils.Max(num, 1) == 0)
				{
					int data4 = CottonBlock.SetSize(data, MathUtils.Min(size + 1, 2));
					if (m_random.Float(0f, 1f) < num2 && size == 1)
					{
						data4 = CottonBlock.SetIsWild(data4, isWild: true);
					}
					int value4 = Terrain.ReplaceData(value, data4);
					value3 = (m_toReplace[new Point3(x, y, z)] = new Replacement
					{
						Value = value4,
						RequiredValue = value
					});
					if (size + 1 == 2)
					{
						int data5 = SoilBlock.SetNitrogen(data3, MathUtils.Max(nitrogen - 1, 0));
						int value5 = Terrain.ReplaceData(cellValueFast, data5);
						Dictionary<Point3, Replacement> toReplace2 = m_toReplace;
						Point3 key2 = new Point3(x, y - 1, z);
						value3 = new Replacement
						{
							Value = value5,
							RequiredValue = cellValueFast
						};
						toReplace2[key2] = value3;
					}
				}
			}
			else
			{
				int value6 = Terrain.ReplaceData(value, CottonBlock.SetIsWild(data, isWild: true));
				Dictionary<Point3, Replacement> toReplace3 = m_toReplace;
				Point3 key3 = new Point3(x, y, z);
				value3 = new Replacement
				{
					Value = value6,
					RequiredValue = value
				};
				toReplace3[key3] = value3;
			}
		}

		public void GrowPumpkin(int value, int x, int y, int z, int pollPass)
		{
			if (Terrain.ExtractLight(base.SubsystemTerrain.Terrain.GetCellValueFast(x, y + 1, z)) < 9)
			{
				return;
			}
			int data = Terrain.ExtractData(value);
			int size = BasePumpkinBlock.GetSize(data);
			if (BasePumpkinBlock.GetIsDead(data) || size >= 7)
			{
				return;
			}
			int cellValueFast = base.SubsystemTerrain.Terrain.GetCellValueFast(x, y - 1, z);
			int num = Terrain.ExtractContents(cellValueFast);
			int data2 = Terrain.ExtractData(cellValueFast);
			bool flag = num == 168 && SoilBlock.GetHydration(data2);
			int num2 = (num == 168) ? SoilBlock.GetNitrogen(data2) : 0;
			int num3 = 4;
			float num4 = 0.15f;
			if (num == 168)
			{
				num3--;
				num4 -= 0.05f;
			}
			if (num2 > 0)
			{
				num3--;
				num4 -= 0.05f;
			}
			if (flag)
			{
				num3--;
				num4 -= 0.05f;
			}
			if (pollPass % MathUtils.Max(num3, 1) == 0)
			{
				int data3 = BasePumpkinBlock.SetSize(data, MathUtils.Min(size + 1, 7));
				if (m_random.Float(0f, 1f) < num4)
				{
					data3 = BasePumpkinBlock.SetIsDead(data3, isDead: true);
				}
				int value2 = Terrain.ReplaceData(value, data3);
				Replacement value3 = m_toReplace[new Point3(x, y, z)] = new Replacement
				{
					Value = value2,
					RequiredValue = value
				};
				if (num == 168 && size + 1 == 7)
				{
					int data4 = SoilBlock.SetNitrogen(data2, MathUtils.Max(num2 - 3, 0));
					int value4 = Terrain.ReplaceData(cellValueFast, data4);
					Dictionary<Point3, Replacement> toReplace = m_toReplace;
					Point3 key = new Point3(x, y - 1, z);
					value3 = new Replacement
					{
						Value = value4,
						RequiredValue = cellValueFast
					};
					toReplace[key] = value3;
				}
			}
		}
	}
}
