namespace Game
{
	public class HygrometerElectricElement : ElectricElement
	{
		public float m_voltage;

		public HygrometerElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
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
			CellFace cellFace = base.CellFaces[0];
			int humidity = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetHumidity(cellFace.X, cellFace.Z);
			m_voltage = (float)humidity / 15f;
			return m_voltage != voltage;
		}
	}
}
