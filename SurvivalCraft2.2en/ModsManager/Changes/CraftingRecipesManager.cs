using Engine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public static class CraftingRecipesManager
	{
		public static List<CraftingRecipe> m_recipes = new List<CraftingRecipe>();

		public static ReadOnlyList<CraftingRecipe> Recipes => new ReadOnlyList<CraftingRecipe>(m_recipes);
		public static Action Initialize1;

		public static void Initialize()
		{
			if (Initialize1 != null)
			{
				Initialize1(); return;
			}
			XElement source1 = ContentManager.Get<XElement>("CraftingRecipes");
			ModsManager.CombineXml(source1, ModsManager.GetEntries(".cr"), "Description", "Result", "Recipes");
			IEnumerable<XElement> source2=source1.Descendants("Recipe");
			foreach (XElement item in source2)
			{
				CraftingRecipe craftingRecipe = new CraftingRecipe();
				string attributeValue = XmlUtils.GetAttributeValue<string>(item, "Result");
				craftingRecipe.ResultValue = DecodeResult(attributeValue);
				craftingRecipe.ResultCount = XmlUtils.GetAttributeValue<int>(item, "ResultCount");
				string attributeValue2 = XmlUtils.GetAttributeValue(item, "Remains", string.Empty);
				if (!string.IsNullOrEmpty(attributeValue2))
				{
					craftingRecipe.RemainsValue = DecodeResult(attributeValue2);
					craftingRecipe.RemainsCount = XmlUtils.GetAttributeValue<int>(item, "RemainsCount");
				}
				craftingRecipe.RequiredHeatLevel = XmlUtils.GetAttributeValue<float>(item, "RequiredHeatLevel");
				craftingRecipe.RequiredPlayerLevel = XmlUtils.GetAttributeValue(item, "RequiredPlayerLevel", 1f);
				craftingRecipe.Description = XmlUtils.GetAttributeValue<string>(item, "Description");
				craftingRecipe.Message = XmlUtils.GetAttributeValue<string>(item, "Message", null);
				if (craftingRecipe.ResultCount > BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.ResultValue)].MaxStacking)
				{
					throw new InvalidOperationException($"In recipe for \"{attributeValue}\" ResultCount is larger than max stacking of result block.");
				}
				if (craftingRecipe.RemainsValue != 0 && craftingRecipe.RemainsCount > BlocksManager.Blocks[Terrain.ExtractContents(craftingRecipe.RemainsValue)].MaxStacking)
				{
					throw new InvalidOperationException($"In Recipe for \"{attributeValue2}\" RemainsCount is larger than max stacking of remains block.");
				}
				Dictionary<char, string> dictionary = new Dictionary<char, string>();
				foreach (XAttribute item2 in from a in item.Attributes()
					where a.Name.LocalName.Length == 1 && char.IsLower(a.Name.LocalName[0])
					select a)
				{
					DecodeIngredient(item2.Value, out string craftingId, out int? data);
					if (BlocksManager.FindBlocksByCraftingId(craftingId).Length == 0)
					{
						throw new InvalidOperationException($"Block with craftingId \"{item2.Value}\" not found.");
					}
					if (data.HasValue && (data.Value < 0 || data.Value > 262143))
					{
						throw new InvalidOperationException($"Data in recipe ingredient \"{item2.Value}\" must be between 0 and 0x3FFFF.");
					}
					dictionary.Add(item2.Name.LocalName[0], item2.Value);
				}
				string[] array = item.Value.Trim().Split(new string[] { "\n" }, StringSplitOptions.None);
				for (int i = 0; i < array.Length; i++)
				{
					int num = array[i].IndexOf('"');
					int num2 = array[i].LastIndexOf('"');
					if (num < 0 || num2 < 0 || num2 <= num)
					{
						throw new InvalidOperationException("Invalid recipe line.");
					}
					string text = array[i].Substring(num + 1, num2 - num - 1);
					for (int j = 0; j < text.Length; j++)
					{
						char c = text[j];
						if (char.IsLower(c))
						{
							string text2 = dictionary[c];
							craftingRecipe.Ingredients[j + i * 3] = text2;
						}
					}
				}
				m_recipes.Add(craftingRecipe);
			}
			Block[] blocks = BlocksManager.Blocks;
			foreach (Block block in blocks)
			{
				m_recipes.AddRange(block.GetProceduralCraftingRecipes());
			}
			m_recipes.Sort(delegate(CraftingRecipe r1, CraftingRecipe r2)
			{
				int y = r1.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
				int x = r2.Ingredients.Count((string s) => !string.IsNullOrEmpty(s));
				return Comparer<int>.Default.Compare(x, y);
			});
		}

		public static CraftingRecipe FindMatchingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel)
		{
			CraftingRecipe craftingRecipe = null;
			Block[] blocks = BlocksManager.Blocks;
			for (int i = 0; i < blocks.Length; i++)
			{
				CraftingRecipe adHocCraftingRecipe = blocks[i].GetAdHocCraftingRecipe(terrain, ingredients, heatLevel, playerLevel);
				if (adHocCraftingRecipe != null && MatchRecipe(adHocCraftingRecipe.Ingredients, ingredients))
				{
					craftingRecipe = adHocCraftingRecipe;
					break;
				}
			}
			if (craftingRecipe == null)
			{
				foreach (CraftingRecipe recipe in Recipes)
				{
					if (MatchRecipe(recipe.Ingredients, ingredients))
					{
						craftingRecipe = recipe;
						break;
					}
				}
			}
			if (craftingRecipe != null)
			{
				if (heatLevel < craftingRecipe.RequiredHeatLevel)
				{
					craftingRecipe = ((!(heatLevel > 0f)) ? new CraftingRecipe
					{
						Message = "Use a furnace to smelt this item"
					} : new CraftingRecipe
					{
						Message = "Need more heat to smelt this item, use better fuel"
					});
				}
				else if (playerLevel < craftingRecipe.RequiredPlayerLevel)
				{
					craftingRecipe = ((!(craftingRecipe.RequiredHeatLevel > 0f)) ? new CraftingRecipe
					{
						Message = $"You must be level {craftingRecipe.RequiredPlayerLevel} to craft this item"
					} : new CraftingRecipe
					{
						Message = $"You must be level {craftingRecipe.RequiredPlayerLevel} to smelt this item"
					});
				}
			}
			return craftingRecipe;
		}

		public static int DecodeResult(string result)
		{
			if (!string.IsNullOrEmpty(result))
			{
				string[] array = result.Split(new char[] { ':' }, StringSplitOptions.None);
				Block block = BlocksManager.FindBlockByTypeName(array[0], throwIfNotFound: true);
				return Terrain.MakeBlockValue(data: (array.Length >= 2) ? int.Parse(array[1], CultureInfo.InvariantCulture) : 0, contents: block.BlockIndex, light: 0);
			}
			return 0;
		}

		public static void DecodeIngredient(string ingredient, out string craftingId, out int? data)
		{
			string[] array = ingredient.Split(new char[] { ':' }, StringSplitOptions.None);
			craftingId = array[0];
			data = ((array.Length >= 2) ? new int?(int.Parse(array[1], CultureInfo.InvariantCulture)) : null);
		}

		public static bool MatchRecipe(string[] requiredIngredients, string[] actualIngredients)
		{
			string[] array = new string[9];
			for (int i = 0; i < 2; i++)
			{
				for (int j = -3; j <= 3; j++)
				{
					for (int k = -3; k <= 3; k++)
					{
						bool flip = (i != 0) ? true : false;
						if (!TransformRecipe(array, requiredIngredients, k, j, flip))
						{
							continue;
						}
						bool flag = true;
						for (int l = 0; l < 9; l++)
						{
							if (!CompareIngredients(array[l], actualIngredients[l]))
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		public static bool TransformRecipe(string[] transformedIngredients, string[] ingredients, int shiftX, int shiftY, bool flip)
		{
			for (int i = 0; i < 9; i++)
			{
				transformedIngredients[i] = null;
			}
			for (int j = 0; j < 3; j++)
			{
				for (int k = 0; k < 3; k++)
				{
					int num = (flip ? (3 - k - 1) : k) + shiftX;
					int num2 = j + shiftY;
					string text = ingredients[k + j * 3];
					if (num >= 0 && num2 >= 0 && num < 3 && num2 < 3)
					{
						transformedIngredients[num + num2 * 3] = text;
					}
					else if (!string.IsNullOrEmpty(text))
					{
						return false;
					}
				}
			}
			return true;
		}

		public static bool CompareIngredients(string requiredIngredient, string actualIngredient)
		{
			if (requiredIngredient == null)
			{
				return actualIngredient == null;
			}
			if (actualIngredient == null)
			{
				return requiredIngredient == null;
			}
			DecodeIngredient(requiredIngredient, out string craftingId, out int? data);
			DecodeIngredient(actualIngredient, out string craftingId2, out int? data2);
			if (!data2.HasValue)
			{
				throw new InvalidOperationException("Actual ingredient data not specified.");
			}
			if (craftingId == craftingId2)
			{
				if (!data.HasValue)
				{
					return true;
				}
				return data.Value == data2.Value;
			}
			return false;
		}
	}
}
