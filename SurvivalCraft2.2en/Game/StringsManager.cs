using System.Collections.Generic;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class StringsManager
	{
		public static Dictionary<string, string> m_strings = new Dictionary<string, string>();

		public static string GetString(string name)
		{
			if (!m_strings.TryGetValue(name, out string value))
			{
				return "<Plchldr>";
			}
			return value;
		}

		public static void LoadStrings()
		{
			foreach (XElement item in ContentManager.Get<XElement>("Strings").Elements())
			{
				string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Name");
				string value = item.Value;
				value = value.Replace("\\n", "\n");
				m_strings.Add(attributeValue, value);
			}
		}
	}
}
