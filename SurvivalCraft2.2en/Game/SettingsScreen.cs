using System.Xml.Linq;

namespace Game
{
	public class SettingsScreen : Screen
	{
		public Screen m_previousScreen;

		public ButtonWidget m_performanceButton;

		public ButtonWidget m_graphicsButton;

		public ButtonWidget m_uiButton;

		public ButtonWidget m_compatibilityButton;

		public ButtonWidget m_audioButton;

		public ButtonWidget m_controlsButton;

		public SettingsScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsScreen");
			LoadContents(this, node);
			m_performanceButton = Children.Find<ButtonWidget>("Performance");
			m_graphicsButton = Children.Find<ButtonWidget>("Graphics");
			m_uiButton = Children.Find<ButtonWidget>("Ui");
			m_compatibilityButton = Children.Find<ButtonWidget>("Compatibility");
			m_audioButton = Children.Find<ButtonWidget>("Audio");
			m_controlsButton = Children.Find<ButtonWidget>("Controls");
		}

		public override void Enter(object[] parameters)
		{
			if (m_previousScreen == null)
			{
				m_previousScreen = ScreensManager.PreviousScreen;
			}
		}

		public override void Update()
		{
			if (m_performanceButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsPerformance");
			}
			if (m_graphicsButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsGraphics");
			}
			if (m_uiButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsUi");
			}
			if (m_compatibilityButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsCompatibility");
			}
			if (m_audioButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsAudio");
			}
			if (m_controlsButton.IsClicked)
			{
				ScreensManager.SwitchScreen("SettingsControls");
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(m_previousScreen);
				m_previousScreen = null;
			}
		}
	}
}
