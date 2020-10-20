using Engine;

namespace Game
{
	public class ButtonElectricElement : MountedElectricElement
	{
		public float m_voltage;

		public bool m_wasPressed;

		public ButtonElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public void Press()
		{
			if (!m_wasPressed && !ElectricElement.IsSignalHigh(m_voltage))
			{
				m_wasPressed = true;
				CellFace cellFace = base.CellFaces[0];
				base.SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, autoDelay: true);
				base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + 1);
			}
		}

		public override float GetOutputVoltage(int face)
		{
			return m_voltage;
		}

		public override bool Simulate()
		{
			float voltage = m_voltage;
			if (m_wasPressed)
			{
				m_wasPressed = false;
				m_voltage = 1f;
				base.SubsystemElectricity.QueueElectricElementForSimulation(this, base.SubsystemElectricity.CircuitStep + 10);
			}
			else
			{
				m_voltage = 0f;
			}
			return m_voltage != voltage;
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			Press();
			return true;
		}

		public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
		{
			Press();
		}
	}
}
