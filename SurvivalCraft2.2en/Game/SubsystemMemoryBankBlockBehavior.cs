using Engine;

namespace Game
{
	public class SubsystemMemoryBankBlockBehavior : SubsystemEditableItemBehavior<MemoryBankData>
	{
		public override int[] HandledBlocks => new int[1]
		{
			186
		};

		public SubsystemMemoryBankBlockBehavior()
			: base(186)
		{
		}

		public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
		{
			int value = inventory.GetSlotValue(slotIndex);
			int count = inventory.GetSlotCount(slotIndex);
			int id = Terrain.ExtractData(value);
			MemoryBankData memoryBankData = GetItemData(id);
			if (memoryBankData != null)
			{
				memoryBankData = (MemoryBankData)memoryBankData.Copy();
			}
			else
			{
				memoryBankData = new MemoryBankData();
			}
			DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditMemoryBankDialog(memoryBankData, delegate
			{
				int data = StoreItemDataAtUniqueId(memoryBankData);
				int value2 = Terrain.ReplaceData(value, data);
				inventory.RemoveSlotItems(slotIndex, count);
				inventory.AddSlotItems(slotIndex, value2, 1);
			}));
			return true;
		}

		public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
		{
			MemoryBankData memoryBankData = GetBlockData(new Point3(x, y, z)) ?? new MemoryBankData();
			DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditMemoryBankDialog(memoryBankData, delegate
			{
				SetBlockData(new Point3(x, y, z), memoryBankData);
				int face = ((MemoryBankBlock)BlocksManager.Blocks[186]).GetFace(value);
				SubsystemElectricity subsystemElectricity = base.SubsystemTerrain.Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
				ElectricElement electricElement = subsystemElectricity.GetElectricElement(x, y, z, face);
				if (electricElement != null)
				{
					subsystemElectricity.QueueElectricElementForSimulation(electricElement, subsystemElectricity.CircuitStep + 1);
				}
			}));
			return true;
		}
	}
}
