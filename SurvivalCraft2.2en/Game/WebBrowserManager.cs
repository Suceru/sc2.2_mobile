using Engine;
using System;

namespace Game
{
	public static class WebBrowserManager
	{
		public static void LaunchBrowser(string url)
		{
			AnalyticsManager.LogEvent("[WebBrowserManager] Launching browser", new AnalyticsParameter("Url", url));
			if (!url.Contains("://"))
			{
				url = "http://" + url;
			}
			try
			{
				System.Diagnostics.Process.Start(url);
			}
			catch (Exception ex)
			{
				Log.Error(string.Format("Error launching web browser with URL \"{0}\". Reason: {1}", new object[2]
				{
					url,
					ex.Message
				}));
			}
		}
	}
}
