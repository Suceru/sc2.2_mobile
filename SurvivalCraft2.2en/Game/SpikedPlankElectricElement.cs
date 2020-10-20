namespace Game
{
	public class SpikedPlankElectricElement : MountedElectricElement
	{
		public int m_lastChangeCircuitStep;

		public bool m_needsReset;

		public float m_voltage;

		public SpikedPlankElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
			m_lastChangeCircuitStep = base.SubsystemElectricity.CircuitStep;
			m_needsReset = true;
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
			if (!ElectricElement.IsSignalHigh(m_voltage))
			{
				m_needsReset = false;
			}
			if (!m_needsReset)
			{
				if (num >= 10)
				{
					if (ElectricElement.IsSignalHigh(m_voltage))
					{
						CellFace cellFace = base.CellFaces[0];
						int data = Terrain.ExtractData(base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
						base.SubsystemElectricity.Project.FindSubsystem<SubsystemSpikesBlockBehavior>(throwOnError: true).RetractExtendSpikes(cellFace.X, cellFace.Y, cellFace.Z, !SpikedPlankBlock.GetSpikesState(data));
					}
				}
				else
				{
					base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + 10 - num);
				}
			}
			return false;
		}
	}
}
