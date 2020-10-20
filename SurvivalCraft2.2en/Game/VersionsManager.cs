using Engine;
using Engine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class VersionsManager
	{
		public static List<VersionConverter> m_versionConverters;

		public static Platform Platform => Platform.Android;

		public static BuildConfiguration BuildConfiguration => BuildConfiguration.Release;

		public static string Version
		{
			get;
			set;
		}

		public static string SerializationVersion
		{
			get;
			set;
		}

		public static string LastLaunchedVersion
		{
			get;
			set;
		}

		static VersionsManager()
		{
			m_versionConverters = new List<VersionConverter>();
			AssemblyName assemblyName = new AssemblyName(typeof(VersionsManager).GetTypeInfo().Assembly.FullName);
			Version = $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}.{assemblyName.Version.Revision}";
			SerializationVersion = $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}";
			Assembly[] array = TypeCache.LoadedAssemblies.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				foreach (TypeInfo definedType in array[i].DefinedTypes)
				{
					if (!definedType.IsAbstract && !definedType.IsInterface && typeof(VersionConverter).GetTypeInfo().IsAssignableFrom(definedType))
					{
						VersionConverter item = (VersionConverter)Activator.CreateInstance(definedType.AsType());
						m_versionConverters.Add(item);
					}
				}
			}
		}

		public static void Initialize()
		{
			LastLaunchedVersion = SettingsManager.LastLaunchedVersion;
			SettingsManager.LastLaunchedVersion = Version;
			if (Version != LastLaunchedVersion)
			{
				AnalyticsManager.LogEvent("[VersionsManager] Upgrade game", new AnalyticsParameter("LastVersion", LastLaunchedVersion), new AnalyticsParameter("CurrentVersion", Version));
			}
		}

		public static void UpgradeProjectXml(XElement projectNode)
		{
			string attributeValue = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
			if (attributeValue != SerializationVersion)
			{
				foreach (VersionConverter item in FindTransform(attributeValue, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException($"Cannot find conversion path from version \"{attributeValue}\" to version \"{SerializationVersion}\""))
				{
					Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
					item.ConvertProjectXml(projectNode);
				}
				string attributeValue2 = XmlUtils.GetAttributeValue(projectNode, "Version", "1.0");
				if (attributeValue2 != SerializationVersion)
				{
					throw new InvalidOperationException($"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{attributeValue2}\".");
				}
			}
		}

		public static void UpgradeWorld(string directoryName)
		{
			WorldInfo worldInfo = WorldsManager.GetWorldInfo(directoryName);
			if (worldInfo == null)
			{
				throw new InvalidOperationException($"Cannot determine version of world at \"{directoryName}\"");
			}
			if (worldInfo.SerializationVersion != SerializationVersion)
			{
				ProgressManager.UpdateProgress($"Upgrading World To {SerializationVersion}", 0f);
				foreach (VersionConverter item in FindTransform(worldInfo.SerializationVersion, SerializationVersion, m_versionConverters, 0) ?? throw new InvalidOperationException($"Cannot find conversion path from version \"{worldInfo.SerializationVersion}\" to version \"{SerializationVersion}\""))
				{
					Log.Information($"Upgrading world version \"{item.SourceVersion}\" to \"{item.TargetVersion}\".");
					item.ConvertWorld(directoryName);
				}
				WorldInfo worldInfo2 = WorldsManager.GetWorldInfo(directoryName);
				if (worldInfo2.SerializationVersion != SerializationVersion)
				{
					throw new InvalidOperationException($"Upgrade produced invalid project version. Expected \"{SerializationVersion}\", found \"{worldInfo2.SerializationVersion}\".");
				}
				AnalyticsManager.LogEvent("[VersionConverter] Upgrade world", new AnalyticsParameter("SourceVersion", worldInfo.SerializationVersion), new AnalyticsParameter("TargetVersion", SerializationVersion));
			}
		}

		public static int CompareVersions(string v1, string v2)
		{
			string[] array = v1.Split('.');
			string[] array2 = v2.Split('.');
			for (int i = 0; i < MathUtils.Min(array.Length, array2.Length); i++)
			{
				int result;
				int result2;
				int num = (!int.TryParse(array[i], out result) || !int.TryParse(array2[i], out result2)) ? string.CompareOrdinal(array[i], array2[i]) : (result - result2);
				if (num != 0)
				{
					return num;
				}
			}
			return array.Length - array2.Length;
		}

		public static List<VersionConverter> FindTransform(string sourceVersion, string targetVersion, IEnumerable<VersionConverter> converters, int depth)
		{
			if (depth > 100)
			{
				throw new InvalidOperationException("Too deep recursion when searching for version converters. Check for possible loops in transforms.");
			}
			if (sourceVersion == targetVersion)
			{
				return new List<VersionConverter>();
			}
			List<VersionConverter> result = null;
			int num = 2147483647;
			foreach (VersionConverter converter in converters)
			{
				if (converter.SourceVersion == sourceVersion)
				{
					List<VersionConverter> list = FindTransform(converter.TargetVersion, targetVersion, converters, depth + 1);
					if (list != null && list.Count < num)
					{
						num = list.Count;
						list.Insert(0, converter);
						result = list;
					}
				}
			}
			return result;
		}
	}
}
