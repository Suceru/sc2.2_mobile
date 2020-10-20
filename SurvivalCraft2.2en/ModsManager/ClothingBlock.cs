using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using XmlUtilities;

namespace Game
{
	public class ClothingBlock : Block
	{
		public const int Index = 203;

		public static ClothingData[] m_clothingData;

		public BlockMesh m_innerMesh;

		public BlockMesh m_outerMesh;
		public static Action Initialize1;
		public static Matrix[] m_slotTransforms = new Matrix[4]
		{
			Matrix.CreateTranslation(0f, -1.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -1.1f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.5f, 0f) * Matrix.CreateScale(2.7f),
			Matrix.CreateTranslation(0f, -0.1f, 0f) * Matrix.CreateScale(2.7f)
		};

		public override void Initialize()
		{
			if (Initialize1 != null) {
				Initialize1();
				return;
			}
			int num = 0;
			Dictionary<int, ClothingData> dictionary = new Dictionary<int, ClothingData>();
			IEnumerator<XElement> enumerator = ModsManager.CombineXml(ContentManager.Get<XElement>("Clothes"), ModsManager.GetEntries(".clo"), "Index", "DisplayName", "Clothes").Elements().GetEnumerator();
			while (enumerator.MoveNext())
			{
				XElement item = enumerator.Current;
				ClothingData clothingData = new ClothingData
				{
					Index = XmlUtils.GetAttributeValue<int>(item, "Index"),
					DisplayIndex = num++,
					DisplayName = XmlUtils.GetAttributeValue<string>(item, "DisplayName"),
					Slot = XmlUtils.GetAttributeValue<ClothingSlot>(item, "Slot"),
					ArmorProtection = XmlUtils.GetAttributeValue<float>(item, "ArmorProtection"),
					Sturdiness = XmlUtils.GetAttributeValue<float>(item, "Sturdiness"),
					Insulation = XmlUtils.GetAttributeValue<float>(item, "Insulation"),
					MovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "MovementSpeedFactor"),
					SteedMovementSpeedFactor = XmlUtils.GetAttributeValue<float>(item, "SteedMovementSpeedFactor"),
					DensityModifier = XmlUtils.GetAttributeValue<float>(item, "DensityModifier"),
					IsOuter = XmlUtils.GetAttributeValue<bool>(item, "IsOuter"),
					CanBeDyed = XmlUtils.GetAttributeValue<bool>(item, "CanBeDyed"),
					Layer = XmlUtils.GetAttributeValue<int>(item, "Layer"),
					PlayerLevelRequired = XmlUtils.GetAttributeValue<int>(item, "PlayerLevelRequired"),
					Texture = ContentManager.Get<Texture2D>(XmlUtils.GetAttributeValue<string>(item, "TextureName")),
					ImpactSoundsFolder = XmlUtils.GetAttributeValue<string>(item, "ImpactSoundsFolder"),
					Description = XmlUtils.GetAttributeValue<string>(item, "Description")
				};
				dictionary.Add(clothingData.Index, clothingData);
			}
			m_clothingData = new ClothingData[dictionary.Count];
			for (int i = 0; i < dictionary.Count; i++)
			{
				m_clothingData[i] = dictionary[i];
			}
			Model playerModel = CharacterSkinsManager.GetPlayerModel(PlayerClass.Male);
			Matrix[] array = new Matrix[playerModel.Bones.Count];
			playerModel.CopyAbsoluteBoneTransformsTo(array);
			int index = playerModel.FindBone("Hand1").Index;
			int index2 = playerModel.FindBone("Hand2").Index;
			array[index] = Matrix.CreateRotationY(0.1f) * array[index];
			array[index2] = Matrix.CreateRotationY(-0.1f) * array[index2];
			m_innerMesh = new BlockMesh();
			foreach (ModelMesh mesh in playerModel.Meshes)
			{
				Matrix matrix = array[mesh.ParentBone.Index];
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					Color color = Color.White * 0.8f;
					color.A = byte.MaxValue;
					m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_innerMesh.AppendModelMeshPart(meshPart, matrix, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color);
				}
			}
			Model outerClothingModel = CharacterSkinsManager.GetOuterClothingModel(PlayerClass.Male);
			Matrix[] array2 = new Matrix[outerClothingModel.Bones.Count];
			outerClothingModel.CopyAbsoluteBoneTransformsTo(array2);
			int index3 = outerClothingModel.FindBone("Leg1").Index;
			int index4 = outerClothingModel.FindBone("Leg2").Index;
			array2[index3] = Matrix.CreateTranslation(-0.02f, 0f, 0f) * array2[index3];
			array2[index4] = Matrix.CreateTranslation(0.02f, 0f, 0f) * array2[index4];
			m_outerMesh = new BlockMesh();
			foreach (ModelMesh mesh2 in outerClothingModel.Meshes)
			{
				Matrix matrix2 = array2[mesh2.ParentBone.Index];
				foreach (ModelMeshPart meshPart2 in mesh2.MeshParts)
				{
					Color color2 = Color.White * 0.8f;
					color2.A = byte.MaxValue;
					m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, Color.White);
					m_outerMesh.AppendModelMeshPart(meshPart2, matrix2, makeEmissive: false, flipWindingOrder: true, doubleSided: false, flipNormals: true, color2);
				}
			}
			base.Initialize();
		}

		public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			int data = Terrain.ExtractData(value);
			ClothingData clothingData = GetClothingData(data);
			int clothingColor = GetClothingColor(data);
			if (clothingColor != 0)
			{
				return SubsystemPalette.GetName(subsystemTerrain, clothingColor, clothingData.DisplayName);
			}
			return clothingData.DisplayName;
		}

		public override string GetDescription(int value)
		{
			return GetClothingData(Terrain.ExtractData(value)).Description;
		}

		public override string GetCategory(int value)
		{
			if (GetClothingColor(Terrain.ExtractData(value)) == 0)
			{
				return base.GetCategory(value);
			}
			return "Dyed";
		}

		public override int GetDamage(int value)
		{
			return (Terrain.ExtractData(value) >> 8) & 0xF;
		}

		public override int SetDamage(int value, int damage)
		{
			int num = Terrain.ExtractData(value);
			num = ((num & -3841) | ((damage & 0xF) << 8));
			return Terrain.ReplaceData(value, num);
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			IEnumerable<ClothingData> enumerable = m_clothingData.OrderBy((ClothingData cd) => cd.DisplayIndex);
			foreach (ClothingData clothingData in enumerable)
			{
				int colorsCount = (!clothingData.CanBeDyed) ? 1 : 16;
				int color = 0;
				while (color < colorsCount)
				{
					int data = SetClothingColor(SetClothingIndex(0, clothingData.Index), color);
					yield return Terrain.MakeBlockValue(203, 0, data);
					int num = color + 1;
					color = num;
				}
			}
		}

		public override CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain terrain, string[] ingredients, float heatLevel, float playerLevel)
		{
			if (heatLevel < 1f)
			{
				return null;
			}
			List<string> list = ingredients.Where((string i) => !string.IsNullOrEmpty(i)).ToList();
			if (list.Count == 2)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				foreach (string item in list)
				{
					CraftingRecipesManager.DecodeIngredient(item, out string craftingId, out int? data);
					if (craftingId == BlocksManager.Blocks[203].CraftingId)
					{
						num3 = Terrain.MakeBlockValue(203, 0, data.HasValue ? data.Value : 0);
					}
					else if (craftingId == BlocksManager.Blocks[129].CraftingId)
					{
						num = Terrain.MakeBlockValue(129, 0, data.HasValue ? data.Value : 0);
					}
					else if (craftingId == BlocksManager.Blocks[128].CraftingId)
					{
						num2 = Terrain.MakeBlockValue(128, 0, data.HasValue ? data.Value : 0);
					}
				}
				if (num != 0 && num3 != 0)
				{
					int data2 = Terrain.ExtractData(num3);
					int clothingColor = GetClothingColor(data2);
					int clothingIndex = GetClothingIndex(data2);
					bool canBeDyed = GetClothingData(data2).CanBeDyed;
					int damage = BlocksManager.Blocks[203].GetDamage(num3);
					int color = PaintBucketBlock.GetColor(Terrain.ExtractData(num));
					int damage2 = BlocksManager.Blocks[129].GetDamage(num);
					Block block = BlocksManager.Blocks[129];
					Block block2 = BlocksManager.Blocks[203];
					if (!canBeDyed)
					{
						return null;
					}
					int num4 = PaintBucketBlock.CombineColors(clothingColor, color);
					if (num4 != clothingColor)
					{
						return new CraftingRecipe
						{
							ResultCount = 1,
							ResultValue = block2.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, num4), clothingIndex)), damage),
							RemainsCount = 1,
							RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(129, 0, color), damage2 + MathUtils.Max(block.Durability / 4, 1)),
							RequiredHeatLevel = 1f,
							Description = $"Dye clothing {SubsystemPalette.GetName(terrain, color, null)}",
							Ingredients = (string[])ingredients.Clone()
						};
					}
				}
				if (num2 != 0 && num3 != 0)
				{
					int data3 = Terrain.ExtractData(num3);
					int clothingColor2 = GetClothingColor(data3);
					int clothingIndex2 = GetClothingIndex(data3);
					bool canBeDyed2 = GetClothingData(data3).CanBeDyed;
					int damage3 = BlocksManager.Blocks[203].GetDamage(num3);
					int damage4 = BlocksManager.Blocks[128].GetDamage(num2);
					Block block3 = BlocksManager.Blocks[128];
					Block block4 = BlocksManager.Blocks[203];
					if (!canBeDyed2)
					{
						return null;
					}
					if (clothingColor2 != 0)
					{
						return new CraftingRecipe
						{
							ResultCount = 1,
							ResultValue = block4.SetDamage(Terrain.MakeBlockValue(203, 0, SetClothingIndex(SetClothingColor(0, 0), clothingIndex2)), damage3),
							RemainsCount = 1,
							RemainsValue = BlocksManager.DamageItem(Terrain.MakeBlockValue(128, 0, 0), damage4 + MathUtils.Max(block3.Durability / 4, 1)),
							RequiredHeatLevel = 1f,
							Description = "Undye clothing",
							Ingredients = (string[])ingredients.Clone()
						};
					}
				}
			}
			return null;
		}

		public static int GetClothingIndex(int data)
		{
			return data & 0xFF;
		}

		public static int SetClothingIndex(int data, int clothingIndex)
		{
			return (data & -256) | (clothingIndex & 0xFF);
		}

		public static ClothingData GetClothingData(int data)
		{
			return m_clothingData[GetClothingIndex(data)];
		}

		public static int GetClothingColor(int data)
		{
			return (data >> 12) & 0xF;
		}

		public static int SetClothingColor(int data, int color)
		{
			return (data & -61441) | ((color & 0xF) << 12);
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int data = Terrain.ExtractData(value);
			int clothingColor = GetClothingColor(data);
			ClothingData clothingData = GetClothingData(data);
			Matrix matrix2 = m_slotTransforms[(int)clothingData.Slot] * Matrix.CreateScale(size) * matrix;
			if (clothingData.IsOuter)
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_outerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
			}
			else
			{
				BlocksManager.DrawMeshBlock(primitivesRenderer, m_innerMesh, clothingData.Texture, color * SubsystemPalette.GetFabricColor(environmentData, clothingColor), 1f, ref matrix2, environmentData);
			}
		}
	}
}
