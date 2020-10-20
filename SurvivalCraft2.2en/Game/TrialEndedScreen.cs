using System.Xml.Linq;

namespace Game
{
	public class TrialEndedScreen : Screen
	{
		public ButtonWidget m_buyButton;

		public ButtonWidget m_quitButton;

		public ButtonWidget m_newWorldButton;

		public TrialEndedScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/TrialEndedScreen");
			LoadContents(this, node);
			m_buyButton = Children.Find<ButtonWidget>("Buy", throwIfNotFound: false);
			m_quitButton = Children.Find<ButtonWidget>("Quit", throwIfNotFound: false);
			m_newWorldButton = Children.Find<ButtonWidget>("NewWorld", throwIfNotFound: false);
		}

		public override void Update()
		{
			if (m_buyButton != null && m_buyButton.IsClicked)
			{
				AnalyticsManager.LogEvent("[TrialEndedScreen] Clicked buy button");
				MarketplaceManager.ShowMarketplace();
				ScreensManager.SwitchScreen("MainMenu");
			}
			if ((m_quitButton != null && m_quitButton.IsClicked) || base.Input.Back)
			{
				AnalyticsManager.LogEvent("[TrialEndedScreen] Clicked quit button");
				ScreensManager.SwitchScreen("MainMenu");
			}
			if (m_newWorldButton != null && m_newWorldButton.IsClicked)
			{
				AnalyticsManager.LogEvent("[TrialEndedScreen] Clicked newworld button");
				ScreensManager.SwitchScreen("NewWorld");
			}
		}
	}
}
