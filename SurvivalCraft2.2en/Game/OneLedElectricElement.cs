using Engine;

namespace Game
{
	public class OneLedElectricElement : MountedElectricElement
	{
		public SubsystemGlow m_subsystemGlow;

		public float m_voltage;

		public Color m_color;

		public GlowPoint m_glowPoint;

		public OneLedElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
			m_subsystemGlow = subsystemElectricity.Project.FindSubsystem<SubsystemGlow>(throwOnError: true);
		}

		public override void OnAdded()
		{
			CellFace cellFace = base.CellFaces[0];
			int data = Terrain.ExtractData(base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z));
			int mountingFace = FourLedBlock.GetMountingFace(data);
			m_color = LedBlock.LedColors[FourLedBlock.GetColor(data)];
			Vector3 v = new Vector3((float)cellFace.X + 0.5f, (float)cellFace.Y + 0.5f, (float)cellFace.Z + 0.5f);
			Vector3 vector = CellFace.FaceToVector3(mountingFace);
			Vector3 vector2 = (mountingFace < 4) ? Vector3.UnitY : Vector3.UnitX;
			Vector3 right = Vector3.Cross(vector, vector2);
			m_glowPoint = m_subsystemGlow.AddGlowPoint();
			m_glowPoint.Position = v - 0.4375f * CellFace.FaceToVector3(mountingFace);
			m_glowPoint.Forward = vector;
			m_glowPoint.Up = vector2;
			m_glowPoint.Right = right;
			m_glowPoint.Color = Color.Transparent;
			m_glowPoint.Size = 0.52f;
			m_glowPoint.FarSize = 0.52f;
			m_glowPoint.FarDistance = 1f;
			m_glowPoint.Type = GlowPointType.Square;
		}

		public override void OnRemoved()
		{
			m_subsystemGlow.RemoveGlowPoint(m_glowPoint);
		}

		public override bool Simulate()
		{
			float voltage = m_voltage;
			m_voltage = 0f;
			foreach (ElectricConnection connection in base.Connections)
			{
				if (connection.ConnectorType != ElectricConnectorType.Output && connection.NeighborConnectorType != 0)
				{
					m_voltage = MathUtils.Max(m_voltage, connection.NeighborElectricElement.GetOutputVoltage(connection.NeighborConnectorFace));
				}
			}
			if (m_voltage != voltage)
			{
				int num = (int)MathUtils.Round(m_voltage * 15f);
				if (num >= 8)
				{
					m_glowPoint.Color = LedBlock.LedColors[MathUtils.Clamp(num - 8, 0, 7)];
				}
				else
				{
					m_glowPoint.Color = Color.Transparent;
				}
			}
			return false;
		}
	}
}
