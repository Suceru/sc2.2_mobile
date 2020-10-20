using Engine;
using Engine.Serialization;
using System;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class WorldPalette
	{
		public const int MaxColors = 16;

		public const int MaxNameLength = 16;

		public static readonly Color[] DefaultColors = new Color[16]
		{
			new Color(255, 255, 255),
			new Color(181, 255, 255),
			new Color(255, 181, 255),
			new Color(160, 181, 255),
			new Color(255, 240, 160),
			new Color(181, 255, 181),
			new Color(255, 181, 160),
			new Color(181, 181, 181),
			new Color(112, 112, 112),
			new Color(32, 112, 112),
			new Color(112, 32, 112),
			new Color(26, 52, 128),
			new Color(87, 54, 31),
			new Color(24, 116, 24),
			new Color(136, 32, 32),
			new Color(24, 24, 24)
		};

		public static readonly string[] DefaultNames = new string[16]
		{
			"White",
			"Pale Cyan",
			"Pink",
			"Pale Blue",
			"Yellow",
			"Pale Green",
			"Salmon",
			"Light Gray",
			"Gray",
			"Cyan",
			"Purple",
			"Blue",
			"Brown",
			"Green",
			"Red",
			"Black"
		};

		public Color[] Colors;

		public string[] Names;

		public WorldPalette()
		{
			Colors = DefaultColors.ToArray();
			Names = DefaultNames.ToArray();
		}

		public WorldPalette(ValuesDictionary valuesDictionary)
		{
			string[] array = valuesDictionary.GetValue("Colors", new string(';', 15)).Split(';');
			if (array.Length != 16)
			{
				throw new InvalidOperationException("Invalid colors.");
			}
			Colors = array.Select((string s, int i) => (!string.IsNullOrEmpty(s)) ? HumanReadableConverter.ConvertFromString<Color>(s) : DefaultColors[i]).ToArray();
			string[] array2 = valuesDictionary.GetValue("Names", new string(';', 15)).Split(';');
			if (array2.Length != 16)
			{
				throw new InvalidOperationException("Invalid color names.");
			}
			Names = array2.Select((string s, int i) => (!string.IsNullOrEmpty(s)) ? s : DefaultNames[i]).ToArray();
			string[] names = Names;
			int num = 0;
			while (true)
			{
				if (num < names.Length)
				{
					if (!VerifyColorName(names[num]))
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			throw new InvalidOperationException("Invalid color name.");
		}

		public ValuesDictionary Save()
		{
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			string value = string.Join(";", Colors.Select((Color c, int i) => (!(c == DefaultColors[i])) ? HumanReadableConverter.ConvertToString(c) : string.Empty));
			string value2 = string.Join(";", Names.Select((string n, int i) => (!(n == DefaultNames[i])) ? n : string.Empty));
			valuesDictionary.SetValue("Colors", value);
			valuesDictionary.SetValue("Names", value2);
			return valuesDictionary;
		}

		public void CopyTo(WorldPalette palette)
		{
			palette.Colors = Colors.ToArray();
			palette.Names = Names.ToArray();
		}

		public static bool VerifyColorName(string name)
		{
			if (name.Length < 1 || name.Length > 16)
			{
				return false;
			}
			foreach (char c in name)
			{
				if (!char.IsLetterOrDigit(c) && c != '-' && c != ' ')
				{
					return false;
				}
			}
			return true;
		}
	}
}
