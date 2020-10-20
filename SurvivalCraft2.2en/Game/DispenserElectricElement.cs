using Engine;
using System.Collections.Generic;

namespace Game
{
	public class DispenserElectricElement : ElectricElement
	{
		public bool m_isDispenseAllowed = true;

		public double? m_lastDispenseTime;

		public SubsystemBlockEntities m_subsystemBlockEntities;

		public DispenserElectricElement(SubsystemElectricity subsystemElectricity, Point3 point)
			: base(subsystemElectricity, new List<CellFace>
			{
				new CellFace(point.X, point.Y, point.Z, 0),
				new CellFace(point.X, point.Y, point.Z, 1),
				new CellFace(point.X, point.Y, point.Z, 2),
				new CellFace(point.X, point.Y, point.Z, 3),
				new CellFace(point.X, point.Y, point.Z, 4),
				new CellFace(point.X, point.Y, point.Z, 5)
			})
		{
			m_subsystemBlockEntities = base.SubsystemElectricity.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
		}

		public override bool Simulate()
		{
			if (CalculateHighInputsCount() > 0)
			{
				if (m_isDispenseAllowed && (!m_lastDispenseTime.HasValue || base.SubsystemElectricity.SubsystemTime.GameTime - m_lastDispenseTime > 0.1))
				{
					m_isDispenseAllowed = false;
					m_lastDispenseTime = base.SubsystemElectricity.SubsystemTime.GameTime;
					m_subsystemBlockEntities.GetBlockEntity(base.CellFaces[0].Point.X, base.CellFaces[0].Point.Y, base.CellFaces[0].Point.Z)?.Entity.FindComponent<ComponentDispenser>()?.Dispense();
				}
			}
			else
			{
				m_isDispenseAllowed = true;
			}
			return false;
		}
	}
}
