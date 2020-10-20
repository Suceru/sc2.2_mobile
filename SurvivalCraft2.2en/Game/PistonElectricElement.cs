using Engine;
using System.Collections.Generic;

namespace Game
{
	public class PistonElectricElement : ElectricElement
	{
		public int m_lastLength = -1;

		public PistonElectricElement(SubsystemElectricity subsystemElectricity, Point3 point)
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
		}

		public override bool Simulate()
		{
			float num = 0f;
			foreach (ElectricConnection connection in base.Connections)
			{
				if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
				{
					num = MathUtils.Max(num, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
			}
			int num2 = MathUtils.Max((int)(num * 15.999f) - 7, 0);
			if (num2 != m_lastLength)
			{
				m_lastLength = num2;
				base.SubsystemElectricity.Project.FindSubsystem<SubsystemPistonBlockBehavior>(throwOnError: true).AdjustPiston(base.CellFaces[0].Point, num2);
			}
			return false;
		}
	}
}
