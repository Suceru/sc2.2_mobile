using Engine;

namespace Game
{
	public class ThermometerElectricElement : ElectricElement
	{
		public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

		public float m_voltage;

		public const float m_pollingPeriod = 0.5f;

		public ThermometerElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
			m_subsystemMetersBlockBehavior = base.SubsystemElectricity.Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
		}

		public override float GetOutputVoltage(int face)
		{
			return m_voltage;
		}

		public override bool Simulate()
		{
			float voltage = m_voltage;
			CellFace cellFace = base.CellFaces[0];
			m_voltage = MathUtils.Saturate((float)m_subsystemMetersBlockBehavior.GetThermometerReading(cellFace.X, cellFace.Y, cellFace.Z) / 15f);
			float num = 0.5f * (0.9f + 0.000200000009f * (float)(GetHashCode() % 1000));
			base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + MathUtils.Max((int)(num / 0.01f), 1));
			return m_voltage != voltage;
		}
	}
}
