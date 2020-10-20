using System.Xml.Linq;

namespace Game
{
	public class SettingsUiScreen : Screen
	{
		public ContainerWidget m_windowModeContainer;

		public ButtonWidget m_windowModeButton;

		public ButtonWidget m_uiSizeButton;

		public ButtonWidget m_upsideDownButton;

		public ButtonWidget m_hideMoveLookPadsButton;

		public ButtonWidget m_showGuiInScreenshotsButton;

		public ButtonWidget m_showLogoInScreenshotsButton;

		public ButtonWidget m_screenshotSizeButton;

		public ButtonWidget m_communityContentModeButton;

		public SettingsUiScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsUiScreen");
			LoadContents(this, node);
			m_windowModeContainer = Children.Find<ContainerWidget>("WindowModeContainer");
			m_windowModeButton = Children.Find<ButtonWidget>("WindowModeButton");
			m_uiSizeButton = Children.Find<ButtonWidget>("UiSizeButton");
			m_upsideDownButton = Children.Find<ButtonWidget>("UpsideDownButton");
			m_hideMoveLookPadsButton = Children.Find<ButtonWidget>("HideMoveLookPads");
			m_showGuiInScreenshotsButton = Children.Find<ButtonWidget>("ShowGuiInScreenshotsButton");
			m_showLogoInScreenshotsButton = Children.Find<ButtonWidget>("ShowLogoInScreenshotsButton");
			m_screenshotSizeButton = Children.Find<ButtonWidget>("ScreenshotSizeButton");
			m_communityContentModeButton = Children.Find<ButtonWidget>("CommunityContentModeButton");
		}

		public override void Enter(object[] parameters)
		{
			m_windowModeContainer.IsVisible = false;
		}

		public override void Update()
		{
			if (m_windowModeButton.IsClicked)
			{
				SettingsManager.WindowMode = (WindowMode)((int)(SettingsManager.WindowMode + 1) % EnumUtils.GetEnumValues(typeof(WindowMode)).Count);
			}
			if (m_uiSizeButton.IsClicked)
			{
				SettingsManager.GuiSize = (GuiSize)((int)(SettingsManager.GuiSize + 1) % EnumUtils.GetEnumValues(typeof(GuiSize)).Count);
			}
			if (m_upsideDownButton.IsClicked)
			{
				SettingsManager.UpsideDownLayout = !SettingsManager.UpsideDownLayout;
			}
			if (m_hideMoveLookPadsButton.IsClicked)
			{
				SettingsManager.HideMoveLookPads = !SettingsManager.HideMoveLookPads;
			}
			if (m_showGuiInScreenshotsButton.IsClicked)
			{
				SettingsManager.ShowGuiInScreenshots = !SettingsManager.ShowGuiInScreenshots;
			}
			if (m_showLogoInScreenshotsButton.IsClicked)
			{
				SettingsManager.ShowLogoInScreenshots = !SettingsManager.ShowLogoInScreenshots;
			}
			if (m_screenshotSizeButton.IsClicked)
			{
				SettingsManager.ScreenshotSize = (ScreenshotSize)((int)(SettingsManager.ScreenshotSize + 1) % EnumUtils.GetEnumValues(typeof(ScreenshotSize)).Count);
			}
			if (m_communityContentModeButton.IsClicked)
			{
				SettingsManager.CommunityContentMode = (CommunityContentMode)((int)(SettingsManager.CommunityContentMode + 1) % EnumUtils.GetEnumValues(typeof(CommunityContentMode)).Count);
			}
			m_windowModeButton.Text = SettingsManager.WindowMode.ToString();
			m_uiSizeButton.Text = SettingsManager.GuiSize.ToString();
			m_upsideDownButton.Text = (SettingsManager.UpsideDownLayout ? "Yes" : "No");
			m_hideMoveLookPadsButton.Text = (SettingsManager.HideMoveLookPads ? "Yes" : "No");
			m_showGuiInScreenshotsButton.Text = (SettingsManager.ShowGuiInScreenshots ? "Yes" : "No");
			m_showLogoInScreenshotsButton.Text = (SettingsManager.ShowLogoInScreenshots ? "Yes" : "No");
			m_screenshotSizeButton.Text = SettingsManager.ScreenshotSize.ToString();
			m_communityContentModeButton.Text = SettingsManager.CommunityContentMode.ToString();
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
	}
}
