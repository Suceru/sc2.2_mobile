using Engine;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class SettingsGraphicsScreen : Screen
	{
		public BevelledButtonWidget m_virtualRealityButton;

		public SliderWidget m_brightnessSlider;

		public ContainerWidget m_vrPanel;

		public ButtonWidget m_fullScreenModeButton;

		public SettingsGraphicsScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsGraphicsScreen");
			var ns = node.GetDefaultNamespace();
			var stackPanel = node.Element(ns + "StackPanelWidget").Elements(ns + "CanvasWidget").First(e => e.Attribute("Margin") != null).Element(ns + "ScrollPanelWidget").Element(ns + "StackPanelWidget");
			stackPanel.Add(XElement.Parse(@"<UniformSpacingPanelWidget xmlns=""runtime-namespace:Game"" Name=""FullScreenModePanel"" Direction=""Horizontal"">
              <LabelWidget Text=""Full Screen Mode:"" HorizontalAlignment=""Far"" VerticalAlignment=""Center"" Margin=""20, 0"" />
              <BevelledButtonWidget Name=""FullScreenModeButton"" Style=""{Styles/ButtonStyle_310x60}"" VerticalAlignment=""Center"" Margin=""20, 0"" />
            </UniformSpacingPanelWidget>"));

			LoadContents(this, node);
			m_virtualRealityButton = Children.Find<BevelledButtonWidget>("VirtualRealityButton");
			m_brightnessSlider = Children.Find<SliderWidget>("BrightnessSlider");
			m_vrPanel = Children.Find<ContainerWidget>("VrPanel");
			m_vrPanel.IsVisible = false;
			m_fullScreenModeButton = Children.Find<ButtonWidget>("FullScreenModeButton");
		}

		public override void Update()
		{
			if (m_virtualRealityButton.IsClicked)
			{
				if (SettingsManager.UseVr)
				{
					SettingsManager.UseVr = false;
					VrManager.StopVr();
				}
				else
				{
					SettingsManager.UseVr = true;
					VrManager.StartVr();
				}
			}
			if (m_brightnessSlider.IsSliding)
			{
				SettingsManager.Brightness = m_brightnessSlider.Value;
			}
			if (m_fullScreenModeButton.IsClicked)
			{
				SettingsManager.FullScreenMode = !SettingsManager.FullScreenMode;
			}
			m_virtualRealityButton.IsEnabled = VrManager.IsVrAvailable;
			m_virtualRealityButton.Text = (SettingsManager.UseVr ? "Enabled" : "Disabled");
			m_brightnessSlider.Value = SettingsManager.Brightness;
			m_brightnessSlider.Text = MathUtils.Round(SettingsManager.Brightness * 10f).ToString();
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
			m_fullScreenModeButton.Text = (SettingsManager.FullScreenMode ? "Enabled" : "Disabled");
		}
	}
}
