namespace Game
{
	public class TruthTableCircuitElectricElement : RotateableElectricElement
	{
		public SubsystemTruthTableCircuitBlockBehavior m_subsystemTruthTableCircuitBlockBehavior;

		public float m_voltage;

		public TruthTableCircuitElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
			m_subsystemTruthTableCircuitBlockBehavior = subsystemElectricity.Project.FindSubsystem<SubsystemTruthTableCircuitBlockBehavior>(throwOnError: true);
		}

		public override float GetOutputVoltage(int face)
		{
			return m_voltage;
		}

		public override bool Simulate()
		{
			float voltage = m_voltage;
			int num = 0;
			int rotation = base.Rotation;
			foreach (ElectricConnection connection in base.Connections)
			{
				if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
				{
					ElectricConnectorDirection? connectorDirection = SubsystemElectricity.GetConnectorDirection(base.CellFaces[0].Face, rotation, connection.ConnectorFace);
					if (connectorDirection.HasValue)
					{
						if (connectorDirection == ElectricConnectorDirection.Top)
						{
							if (ElectricElement.IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace)))
							{
								num |= 1;
							}
						}
						else if (connectorDirection == ElectricConnectorDirection.Right)
						{
							if (ElectricElement.IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace)))
							{
								num |= 2;
							}
						}
						else if (connectorDirection == ElectricConnectorDirection.Bottom)
						{
							if (ElectricElement.IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace)))
							{
								num |= 4;
							}
						}
						else if (connectorDirection == ElectricConnectorDirection.Left && ElectricElement.IsSignalHigh(connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace)))
						{
							num |= 8;
						}
					}
				}
			}
			TruthTableData blockData = m_subsystemTruthTableCircuitBlockBehavior.GetBlockData(base.CellFaces[0].Point);
			m_voltage = ((blockData != null) ? ((float)(int)blockData.Data[num] / 15f) : 0f);
			return m_voltage != voltage;
		}
	}
}
