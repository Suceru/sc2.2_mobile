using Engine;
using System;
using System.Collections.Generic;

namespace Game
{
	public static class AnalyticsManagerUtils
	{
		public static string AbbreviateStackTrace(string stackTrace)
		{
			stackTrace = stackTrace.Replace("System.Collections.Generic.", "");
			stackTrace = stackTrace.Replace("System.Collections.", "");
			stackTrace = stackTrace.Replace("System.IO.", "");
			stackTrace = stackTrace.Replace("Engine.Audio.", "");
			stackTrace = stackTrace.Replace("Engine.Input.", "");
			stackTrace = stackTrace.Replace("Engine.Graphics.", "");
			stackTrace = stackTrace.Replace("Engine.", "");
			if (stackTrace.StartsWith("Engine."))
			{
				stackTrace = stackTrace.Substring("Engine.".Length);
			}
			if (stackTrace.StartsWith("Game."))
			{
				stackTrace = stackTrace.Substring("Game.".Length);
			}
			if (stackTrace.StartsWith("System."))
			{
				stackTrace = stackTrace.Substring("System.".Length);
			}
			return stackTrace;
		}

		public static string[] SplitStackTrace(string stackTrace)
		{
			List<string> list = new List<string>();
			do
			{
				string text = stackTrace.Substring(0, MathUtils.Min(stackTrace.Length, 254));
				list.Add(text);
				stackTrace = stackTrace.Remove(0, text.Length);
			}
			while (stackTrace.Length > 0 && list.Count < 4);
			return list.ToArray();
		}

		public static AnalyticsParameter[] CreateAnalyticsParametersForError(string message, Exception error)
		{
			string text = ExceptionManager.MakeFullErrorMessage(message, error);
			if (text.Length > 254)
			{
				text = text.Substring(0, 254);
			}
			string[] array = SplitStackTrace(AbbreviateStackTrace(error.StackTrace));
			return new AnalyticsParameter[6]
			{
				new AnalyticsParameter("FullMessage", text),
				new AnalyticsParameter("StackTrace1", (array.Length >= 1) ? array[0] : string.Empty),
				new AnalyticsParameter("StackTrace2", (array.Length >= 2) ? array[1] : string.Empty),
				new AnalyticsParameter("StackTrace3", (array.Length >= 3) ? array[2] : string.Empty),
				new AnalyticsParameter("StackTrace4", (array.Length >= 4) ? array[3] : string.Empty),
				new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff"))
			};
		}
	}
}
