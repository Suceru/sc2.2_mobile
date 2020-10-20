using Engine;
using Engine.Input;
using System.Xml.Linq;

namespace Game
{
	public class MainMenuScreen : Screen
	{
		public string m_versionString = string.Empty;

		public bool m_versionStringTrial;

		public MainMenuScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/MainMenuScreen");
			LoadContents(this, node);
		}

		public override void Enter(object[] parameters)
		{
			Children.Find<MotdWidget>().Restart();
			if (SettingsManager.IsolatedStorageMigrationCounter < 3)
			{
				SettingsManager.IsolatedStorageMigrationCounter++;
				VersionConverter126To127.MigrateDataFromIsolatedStorageWithDialog();
			}
		}

		public override void Leave()
		{
			Keyboard.BackButtonQuitsApp = false;
		}

		public override void Update()
		{
			Keyboard.BackButtonQuitsApp = !MarketplaceManager.IsTrialMode;
			if (string.IsNullOrEmpty(m_versionString) || MarketplaceManager.IsTrialMode != m_versionStringTrial)
			{
				m_versionString = string.Format("Version {0}{1}", VersionsManager.Version, MarketplaceManager.IsTrialMode ? " (Day One)" : string.Empty);
				m_versionStringTrial = MarketplaceManager.IsTrialMode;
			}
			Children.Find("Buy").IsVisible = MarketplaceManager.IsTrialMode;
			Children.Find<LabelWidget>("Version").Text = m_versionString;
			RectangleWidget rectangleWidget = Children.Find<RectangleWidget>("Logo");
			float num = 1f + 0.02f * MathUtils.Sin(1.5f * (float)MathUtils.Remainder(Time.FrameStartTime, 10000.0));
			rectangleWidget.RenderTransform = Matrix.CreateTranslation((0f - rectangleWidget.ActualSize.X) / 2f, (0f - rectangleWidget.ActualSize.Y) / 2f, 0f) * Matrix.CreateScale(num, num, 1f) * Matrix.CreateTranslation(rectangleWidget.ActualSize.X / 2f, rectangleWidget.ActualSize.Y / 2f, 0f);
			if (Children.Find<ButtonWidget>("Play").IsClicked)
			{
				ScreensManager.SwitchScreen("Play");
			}
			if (Children.Find<ButtonWidget>("Help").IsClicked)
			{
				ScreensManager.SwitchScreen("Help");
			}
			if (Children.Find<ButtonWidget>("Content").IsClicked)
			{
				ScreensManager.SwitchScreen("Content");
			}
			if (Children.Find<ButtonWidget>("Settings").IsClicked)
			{
				ScreensManager.SwitchScreen("Settings");
			}
			if (Children.Find<ButtonWidget>("Buy").IsClicked)
			{
				AnalyticsManager.LogEvent("[MainMenuScreen] Clicked buy button");
				MarketplaceManager.ShowMarketplace();
			}
			if ((base.Input.Back && !Keyboard.BackButtonQuitsApp) || base.Input.IsKeyDownOnce(Key.Escape))
			{
				if (MarketplaceManager.IsTrialMode)
				{
					ScreensManager.SwitchScreen("Nag");
				}
				else
				{
					Window.Close();
				}
			}
		}
	}
}
