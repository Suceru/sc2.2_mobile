using System.Collections.Generic;

namespace Game
{
	public abstract class BaseDelayGateElectricElement : RotateableElectricElement
	{
		public float m_voltage;

		public float m_lastStoredVoltage;

		public Dictionary<int, float> m_voltagesHistory = new Dictionary<int, float>();

		public abstract int DelaySteps
		{
			get;
		}

		protected BaseDelayGateElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public override float GetOutputVoltage(int face)
		{
			return m_voltage;
		}

		public override bool Simulate()
		{
			float voltage = m_voltage;
			int delaySteps = DelaySteps;
			float num = 0f;
			foreach (ElectricConnection connection in base.Connections)
			{
				if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
				{
					num = connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace);
					break;
				}
			}
			if (delaySteps > 0)
			{
				if (m_voltagesHistory.TryGetValue(base.SubsystemElectricity.CircuitStep, out float value))
				{
					m_voltage = value;
					m_voltagesHistory.Remove(base.SubsystemElectricity.CircuitStep);
				}
				if (num != m_lastStoredVoltage)
				{
					m_lastStoredVoltage = num;
					if (m_voltagesHistory.Count < 300)
					{
						m_voltagesHistory[base.SubsystemElectricity.CircuitStep + DelaySteps] = num;
						base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + DelaySteps);
					}
				}
			}
			else
			{
				m_voltage = num;
			}
			return m_voltage != voltage;
		}
	}
}
