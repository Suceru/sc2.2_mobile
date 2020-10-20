using System.Xml.Linq;

namespace Game
{
	public class PlayerWidget : CanvasWidget
	{
		public PlayerData m_playerData;

		public PlayerModelWidget m_playerModel;

		public LabelWidget m_nameLabel;

		public LabelWidget m_detailsLabel;

		public ButtonWidget m_editButton;

		public PlayerWidget(PlayerData playerData, CharacterSkinsCache characterSkinsCache)
		{
			XElement node = ContentManager.Get<XElement>("Widgets/PlayerWidget");
			LoadContents(this, node);
			m_playerModel = Children.Find<PlayerModelWidget>("PlayerModel");
			m_nameLabel = Children.Find<LabelWidget>("Name");
			m_detailsLabel = Children.Find<LabelWidget>("Details");
			m_editButton = Children.Find<ButtonWidget>("EditButton");
			m_playerModel.CharacterSkinsCache = characterSkinsCache;
			m_playerData = playerData;
		}

		public override void Update()
		{
			SubsystemGameInfo subsystemGameInfo = m_playerData.SubsystemPlayers.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_playerModel.PlayerClass = m_playerData.PlayerClass;
			m_playerModel.CharacterSkinName = m_playerData.CharacterSkinName;
			m_nameLabel.Text = m_playerData.Name;
			m_detailsLabel.Text = m_playerData.PlayerClass.ToString();
			m_detailsLabel.Text += "\n";
			LabelWidget detailsLabel = m_detailsLabel;
			detailsLabel.Text = detailsLabel.Text + "Controlled by " + PlayerScreen.GetDeviceDisplayName(m_playerData.InputDevice);
			m_detailsLabel.Text += "\n";
			m_detailsLabel.Text += ((m_playerData.LastSpawnTime >= 0.0) ? $"In game for {(subsystemGameInfo.TotalElapsedGameTime - m_playerData.LastSpawnTime) / 1200.0:N1} days" : "Never spawned yet");
			if (m_editButton.IsClicked)
			{
				ScreensManager.SwitchScreen("Player", PlayerScreen.Mode.Edit, m_playerData);
			}
		}
	}
}
