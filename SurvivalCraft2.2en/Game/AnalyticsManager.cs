using System;

namespace Game
{
	public static class AnalyticsManager
	{
		public static string AnalyticsVersion => string.Empty;

		public static void Initialize()
		{
		}

		public static void LogError(string message, Exception error)
		{
		}

		public static void LogEvent(string eventName, params AnalyticsParameter[] parameters)
		{
		}
	}
}
