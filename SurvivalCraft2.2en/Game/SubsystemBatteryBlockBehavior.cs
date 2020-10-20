namespace Game
{
	public class SubsystemBatteryBlockBehavior : SubsystemBlockBehavior
	{
		public override int[] HandledBlocks => new int[1]
		{
			138
		};

		public override bool OnEditInventoryItem(IInventory inventory, int slotIndex, ComponentPlayer componentPlayer)
		{
			int value = inventory.GetSlotValue(slotIndex);
			int count = inventory.GetSlotCount(slotIndex);
			int data = Terrain.ExtractData(value);
			int voltageLevel = BatteryBlock.GetVoltageLevel(data);
			DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditBatteryDialog(voltageLevel, delegate(int newVoltageLevel)
			{
				int data2 = BatteryBlock.SetVoltageLevel(data, newVoltageLevel);
				int num = Terrain.ReplaceData(value, data2);
				if (num != value)
				{
					inventory.RemoveSlotItems(slotIndex, count);
					inventory.AddSlotItems(slotIndex, num, 1);
				}
			}));
			return true;
		}

		public override bool OnEditBlock(int x, int y, int z, int value, ComponentPlayer componentPlayer)
		{
			int data = Terrain.ExtractData(value);
			int voltageLevel = BatteryBlock.GetVoltageLevel(data);
			DialogsManager.ShowDialog(componentPlayer.GuiWidget, new EditBatteryDialog(voltageLevel, delegate(int newVoltageLevel)
			{
				int num = BatteryBlock.SetVoltageLevel(data, newVoltageLevel);
				if (num != data)
				{
					int value2 = Terrain.ReplaceData(value, num);
					base.SubsystemTerrain.ChangeCell(x, y, z, value2);
					SubsystemElectricity subsystemElectricity = base.Project.FindSubsystem<SubsystemElectricity>(throwOnError: true);
					ElectricElement electricElement = subsystemElectricity.GetElectricElement(x, y, z, 4);
					if (electricElement != null)
					{
						subsystemElectricity.QueueElectricElementConnectionsForSimulation(electricElement, subsystemElectricity.CircuitStep + 1);
					}
				}
			}));
			return true;
		}
	}
}
