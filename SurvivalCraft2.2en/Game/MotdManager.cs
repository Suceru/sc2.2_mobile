using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class MotdManager
	{
		public class Message
		{
			public List<Line> Lines = new List<Line>();
		}

		public class Line
		{
			public float Time;

			public XElement Node;

			public string Text;
		}

		public static Message m_message;

		public static Message MessageOfTheDay
		{
			get
			{
				return m_message;
			}
			set
			{
				m_message = value;
				if (MotdManager.MessageOfTheDayUpdated != null)
				{
					MotdManager.MessageOfTheDayUpdated();
				}
			}
		}

		public static event Action MessageOfTheDayUpdated;

		public static void ForceRedownload()
		{
			SettingsManager.MotdLastUpdateTime = DateTime.MinValue;
		}

		public static void Initialize()
		{
			if (VersionsManager.Version != VersionsManager.LastLaunchedVersion)
			{
				ForceRedownload();
			}
		}

		public static void Update()
		{
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				TimeSpan t = TimeSpan.FromHours(SettingsManager.MotdUpdatePeriodHours);
				DateTime now = DateTime.Now;
				if (now >= SettingsManager.MotdLastUpdateTime + t)
				{
					SettingsManager.MotdLastUpdateTime = now;
					Log.Information("Downloading MOTD");
					AnalyticsManager.LogEvent("[MotdManager] Downloading MOTD", new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff")));
					string url = GetMotdUrl();
					WebManager.Get(url, null, null, null, delegate(byte[] result)
					{
						try
						{
							string motdLastDownloadedData = UnpackMotd(result);
							MessageOfTheDay = null;
							SettingsManager.MotdLastDownloadedData = motdLastDownloadedData;
							Log.Information("Downloaded MOTD");
							AnalyticsManager.LogEvent("[MotdManager] Downloaded MOTD", new AnalyticsParameter("Time", DateTime.Now.ToString("HH:mm:ss.fff")), new AnalyticsParameter("Url", url));
							SettingsManager.MotdUseBackupUrl = false;
						}
						catch (Exception ex)
						{
							Log.Error("Failed processing MOTD string. Reason: " + ex.Message);
							SettingsManager.MotdUseBackupUrl = !SettingsManager.MotdUseBackupUrl;
						}
					}, delegate(Exception error)
					{
						Log.Error("Failed downloading MOTD. Reason: {0}", error.Message);
						SettingsManager.MotdUseBackupUrl = !SettingsManager.MotdUseBackupUrl;
					});
				}
			}
			if (MessageOfTheDay == null && !string.IsNullOrEmpty(SettingsManager.MotdLastDownloadedData))
			{
				MessageOfTheDay = ParseMotd(SettingsManager.MotdLastDownloadedData);
				if (MessageOfTheDay == null)
				{
					SettingsManager.MotdLastDownloadedData = string.Empty;
				}
			}
		}

		public static string UnpackMotd(byte[] data)
		{
			string text = "motd.xml";
			using (MemoryStream stream = new MemoryStream(data))
			{
				using (ZipArchive zipArchive = ZipArchive.Open(stream))
				{
					foreach (ZipArchiveEntry item in zipArchive.ReadCentralDir())
					{
						if (item.FilenameInZip.ToLower() == text)
						{
							MemoryStream memoryStream = new MemoryStream();
							zipArchive.ExtractFile(item, memoryStream);
							memoryStream.Position = 0L;
							using (StreamReader streamReader = new StreamReader(memoryStream))
							{
								return streamReader.ReadToEnd();
							}
						}
					}
				}
			}
			throw new InvalidOperationException($"\"{text}\" file not found in Motd zip archive.");
		}

		public static Message ParseMotd(string dataString)
		{
			try
			{
				int num = dataString.IndexOf("<Motd");
				if (num < 0)
				{
					throw new InvalidOperationException("Invalid MOTD data string.");
				}
				int num2 = dataString.IndexOf("</Motd>");
				if (num2 >= 0 && num2 > num)
				{
					num2 += 7;
				}
				XElement xElement = XmlUtils.LoadXmlFromString(dataString.Substring(num, num2 - num), throwOnError: true);
				SettingsManager.MotdUpdatePeriodHours = XmlUtils.GetAttributeValue(xElement, "UpdatePeriodHours", 24);
				SettingsManager.MotdUpdateUrl = XmlUtils.GetAttributeValue(xElement, "UpdateUrl", SettingsManager.MotdUpdateUrl);
				SettingsManager.MotdBackupUpdateUrl = XmlUtils.GetAttributeValue(xElement, "BackupUpdateUrl", SettingsManager.MotdBackupUpdateUrl);
				Message message = new Message();
				foreach (XElement item2 in xElement.Elements())
				{
					if (Widget.IsNodeIncludedOnCurrentPlatform(item2))
					{
						Line item = new Line
						{
							Time = XmlUtils.GetAttributeValue<float>(item2, "Time"),
							Node = item2.Elements().FirstOrDefault(),
							Text = item2.Value
						};
						message.Lines.Add(item);
					}
				}
				return message;
			}
			catch (Exception ex)
			{
				Log.Warning("Failed extracting MOTD string. Reason: " + ex.Message);
			}
			return null;
		}

		public static string GetMotdUrl()
		{
			if (SettingsManager.MotdUseBackupUrl)
			{
				return string.Format(SettingsManager.MotdBackupUpdateUrl, VersionsManager.SerializationVersion);
			}
			return string.Format(SettingsManager.MotdUpdateUrl, VersionsManager.SerializationVersion);
		}
	}
}
