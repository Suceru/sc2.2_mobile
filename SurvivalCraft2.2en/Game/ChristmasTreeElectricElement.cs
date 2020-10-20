namespace Game
{
	public class ChristmasTreeElectricElement : ElectricElement
	{
		public int m_lastChangeCircuitStep;

		public float m_voltage;

		public ChristmasTreeElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace, int value)
			: base(subsystemElectricity, cellFace)
		{
			m_lastChangeCircuitStep = base.SubsystemElectricity.CircuitStep;
			m_voltage = (ChristmasTreeBlock.GetLightState(Terrain.ExtractData(value)) ? 1 : 0);
		}

		public override bool Simulate()
		{
			int num = base.SubsystemElectricity.CircuitStep - m_lastChangeCircuitStep;
			float voltage = (CalculateHighInputsCount() > 0) ? 1 : 0;
			if (ElectricElement.IsSignalHigh(voltage) != ElectricElement.IsSignalHigh(m_voltage))
			{
				m_lastChangeCircuitStep = base.SubsystemElectricity.CircuitStep;
			}
			m_voltage = voltage;
			if (num >= 10)
			{
				CellFace cellFace = base.CellFaces[0];
				int cellValue = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				int data = ChristmasTreeBlock.SetLightState(Terrain.ExtractData(cellValue), ElectricElement.IsSignalHigh(m_voltage));
				int value = Terrain.ReplaceData(cellValue, data);
				base.SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value);
			}
			else
			{
				base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + 10 - num);
			}
			return false;
		}
	}
}
