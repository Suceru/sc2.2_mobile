using Engine;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemTrapdoorBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemElectricity m_subsystemElectricity;

		public static Random m_random = new Random();

		public override int[] HandledBlocks => new int[2]
		{
			83,
			84
		};

		public bool IsTrapdoorElectricallyConnected(int x, int y, int z)
		{
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			int data = Terrain.ExtractData(cellValue);
			if (BlocksManager.Blocks[num] is TrapdoorBlock)
			{
				ElectricElement electricElement = m_subsystemElectricity.GetElectricElement(x, y, z, TrapdoorBlock.GetMountingFace(data));
				if (electricElement != null && electricElement.Connections.Count > 0)
				{
					return true;
				}
			}
			return false;
		}

		public bool OpenCloseTrapdoor(int x, int y, int z, bool open)
		{
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			if (BlocksManager.Blocks[num] is TrapdoorBlock)
			{
				int data = TrapdoorBlock.SetOpen(Terrain.ExtractData(cellValue), open);
				int value = Terrain.ReplaceData(cellValue, data);
				base.SubsystemTerrain.ChangeCell(x, y, z, value);
				string name = open ? "Audio/Doors/DoorOpen" : "Audio/Doors/DoorClose";
				base.SubsystemTerrain.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlaySound(name, 0.7f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 4f, autoDelay: true);
				return true;
			}
			return false;
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			CellFace cellFace = raycastResult.CellFace;
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
			int num = Terrain.ExtractContents(cellValue);
			int data = Terrain.ExtractData(cellValue);
			if (num == 83 || !IsTrapdoorElectricallyConnected(cellFace.X, cellFace.Y, cellFace.Z))
			{
				bool open = TrapdoorBlock.GetOpen(data);
				return OpenCloseTrapdoor(cellFace.X, cellFace.Y, cellFace.Z, !open);
			}
			return true;
		}

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractContents(cellValue);
			Block obj = BlocksManager.Blocks[num];
			int data = Terrain.ExtractData(cellValue);
			if (obj is TrapdoorBlock)
			{
				int rotation = TrapdoorBlock.GetRotation(data);
				bool upsideDown = TrapdoorBlock.GetUpsideDown(data);
				bool flag = false;
				Point3 point = CellFace.FaceToPoint3(rotation);
				int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x - point.X, y - point.Y, z - point.Z);
				flag |= !BlocksManager.Blocks[cellContents].IsTransparent;
				if (upsideDown)
				{
					int cellContents2 = base.SubsystemTerrain.Terrain.GetCellContents(x, y + 1, z);
					flag |= !BlocksManager.Blocks[cellContents2].IsTransparent;
					int cellContents3 = base.SubsystemTerrain.Terrain.GetCellContents(x - point.X, y - point.Y + 1, z - point.Z);
					flag |= !BlocksManager.Blocks[cellContents3].IsTransparent;
				}
				else
				{
					int cellContents4 = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
					flag |= !BlocksManager.Blocks[cellContents4].IsTransparent;
					int cellContents5 = base.SubsystemTerrain.Terrain.GetCellContents(x - point.X, y - point.Y - 1, z - point.Z);
					flag |= !BlocksManager.Blocks[cellContents5].IsTransparent;
				}
				if (!flag)
				{
					base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemElectricity = base.Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
		}
	}
}
