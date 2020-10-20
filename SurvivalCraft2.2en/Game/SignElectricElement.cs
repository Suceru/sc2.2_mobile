using Engine;

namespace Game
{
	public class SignElectricElement : ElectricElement
	{
		public bool m_isMessageAllowed = true;

		public double? m_lastMessageTime;

		public SignElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public override bool Simulate()
		{
			bool flag = CalculateHighInputsCount() > 0;
			if (flag && m_isMessageAllowed && (!m_lastMessageTime.HasValue || base.SubsystemElectricity.SubsystemTime.GameTime - m_lastMessageTime.Value > 0.5))
			{
				m_isMessageAllowed = false;
				m_lastMessageTime = base.SubsystemElectricity.SubsystemTime.GameTime;
				SignData signData = base.SubsystemElectricity.Project.FindSubsystem<SubsystemSignBlockBehavior>(throwOnError: true).GetSignData(new Point3(base.CellFaces[0].X, base.CellFaces[0].Y, base.CellFaces[0].Z));
				if (signData != null)
				{
					string text = string.Join("\n", signData.Lines);
					text = text.Trim('\n');
					text = text.Replace("\\\n", "");
					Color color = (signData.Colors[0] == Color.Black) ? Color.White : signData.Colors[0];
					color *= 255f / (float)MathUtils.Max(color.R, color.G, color.B);
					foreach (ComponentPlayer componentPlayer in base.SubsystemElectricity.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
					{
						componentPlayer.ComponentGui.DisplaySmallMessage(text, color, blinking: true, playNotificationSound: true);
					}
				}
			}
			if (!flag)
			{
				m_isMessageAllowed = true;
			}
			return false;
		}
	}
}
