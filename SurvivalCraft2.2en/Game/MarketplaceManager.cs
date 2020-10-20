using System;

namespace Game
{
	public static class MarketplaceManager
	{
		public static bool m_isInitialized;

		public static bool m_isTrialMode;

		public static bool IsTrialMode
		{
			get
			{
				if (!m_isInitialized)
				{
					throw new InvalidOperationException("MarketplaceManager not initialized.");
				}
				return m_isTrialMode;
			}
			set
			{
				m_isTrialMode = value;
			}
		}

		public static void Initialize()
		{
			m_isInitialized = true;
		}

		public static void ShowMarketplace()
		{
			AnalyticsManager.LogEvent("[MarketplaceManager] Show marketplace");
			WebBrowserManager.LaunchBrowser("http://play.google.com/store/apps/details?id=com.candyrufusgames.survivalcraft2");
		}
	}
}
