using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game
{
	public static class TextureAtlasManager
	{
		public static Dictionary<string, Subtexture> m_subtextures = new Dictionary<string, Subtexture>();

		public static void LoadAtlases()
		{
			LoadTextureAtlas(ContentManager.Get<Texture2D>("Atlases/AtlasTexture"), ContentManager.Get<string>("Atlases/Atlas"), "Textures/Atlas/");
		}

		public static Subtexture GetSubtexture(string name)
		{
			if (!m_subtextures.TryGetValue(name, out Subtexture value))
			{
				try
				{
					value = new Subtexture(ContentManager.Get<Texture2D>(name), Vector2.Zero, Vector2.One);
					m_subtextures.Add(name, value);
					return value;
				}
				catch (Exception innerException)
				{
					throw new InvalidOperationException($"Required subtexture {name} not found in TextureAtlasManager.", innerException);
				}
			}
			return value;
		}

		public static void LoadTextureAtlas(Texture2D texture, string atlasDefinition, string prefix)
		{
			string[] array = atlasDefinition.Split(new char[2]
			{
				'\n',
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			int num = 0;
			while (true)
			{
				if (num < array.Length)
				{
					string[] array2 = array[num].Split(new char[1]
					{
						' '
					}, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length < 5)
					{
						break;
					}
					string key = prefix + array2[0];
					int num2 = int.Parse(array2[1], CultureInfo.InvariantCulture);
					int num3 = int.Parse(array2[2], CultureInfo.InvariantCulture);
					int num4 = int.Parse(array2[3], CultureInfo.InvariantCulture);
					int num5 = int.Parse(array2[4], CultureInfo.InvariantCulture);
					Vector2 topLeft = new Vector2((float)num2 / (float)texture.Width, (float)num3 / (float)texture.Height);
					Vector2 bottomRight = new Vector2((float)(num2 + num4) / (float)texture.Width, (float)(num3 + num5) / (float)texture.Height);
					Subtexture value = new Subtexture(texture, topLeft, bottomRight);
					m_subtextures.Add(key, value);
					num++;
					continue;
				}
				return;
			}
			throw new InvalidOperationException("Invalid texture atlas definition.");
		}
	}
}
