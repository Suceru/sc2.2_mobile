using Engine;

namespace Game
{
	public class MultistateFurnitureElectricElement : FurnitureElectricElement
	{
		public bool m_isActionAllowed;

		public double? m_lastActionTime;

		public MultistateFurnitureElectricElement(SubsystemElectricity subsystemElectricity, Point3 point)
			: base(subsystemElectricity, point)
		{
		}

		public override bool Simulate()
		{
			if (CalculateHighInputsCount() > 0)
			{
				if (m_isActionAllowed && (!m_lastActionTime.HasValue || base.SubsystemElectricity.SubsystemTime.GameTime - m_lastActionTime > 0.1))
				{
					m_isActionAllowed = false;
					m_lastActionTime = base.SubsystemElectricity.SubsystemTime.GameTime;
					base.SubsystemElectricity.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true).SwitchToNextState(base.CellFaces[0].X, base.CellFaces[0].Y, base.CellFaces[0].Z, playSound: false);
				}
			}
			else
			{
				m_isActionAllowed = true;
			}
			return false;
		}
	}
}
