using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class EggBlock : Block
	{
		public class EggType
		{
			public int EggTypeIndex;

			public bool ShowEgg;

			public string DisplayName;

			public string TemplateName;

			public float NutritionalValue;

			public int TextureSlot;

			public Color Color;

			public Vector2 ScaleUV;

			public bool SwapUV;

			public float Scale;

			public BlockMesh BlockMesh;
		}

		public const int Index = 118;

		public List<EggType> m_eggTypes = new List<EggType>();

		public ReadOnlyList<EggType> EggTypes => new ReadOnlyList<EggType>(m_eggTypes);

		public override void Initialize()
		{
			Dictionary<int, EggType> dictionary = new Dictionary<int, EggType>();
			DatabaseObjectType parameterSetType = DatabaseManager.GameDatabase.ParameterSetType;
			Guid eggParameterSetGuid = new Guid("300ff557-775f-4c7c-a88a-26655369f00b");
			foreach (DatabaseObject item in from o in DatabaseManager.GameDatabase.Database.Root.GetExplicitNestingChildren(parameterSetType, directChildrenOnly: false)
				where o.EffectiveInheritanceRoot.Guid == eggParameterSetGuid
				select o)
			{
				int nestedValue = item.GetNestedValue<int>("EggTypeIndex");
				if (nestedValue >= 0)
				{
					if (dictionary.ContainsKey(nestedValue))
					{
						throw new InvalidOperationException($"Duplicate creature egg data EggTypeIndex ({nestedValue}).");
					}
					dictionary.Add(nestedValue, new EggType
					{
						EggTypeIndex = nestedValue,
						ShowEgg = item.GetNestedValue<bool>("ShowEgg"),
						DisplayName = item.GetNestedValue<string>("DisplayName"),
						TemplateName = item.NestingParent.Name,
						NutritionalValue = item.GetNestedValue<float>("NutritionalValue"),
						Color = item.GetNestedValue<Color>("Color"),
						ScaleUV = item.GetNestedValue<Vector2>("ScaleUV"),
						SwapUV = item.GetNestedValue<bool>("SwapUV"),
						Scale = item.GetNestedValue<float>("Scale"),
						TextureSlot = item.GetNestedValue<int>("TextureSlot")
					});
				}
			}
			for (int i = 0; i < dictionary.Count; i++)
			{
				if (dictionary.TryGetValue(i, out EggType value))
				{
					m_eggTypes.Add(value);
					continue;
				}
				throw new InvalidOperationException($"Missing creature egg data EggTypeIndex value {i}.");
			}
			Model model = ContentManager.Get<Model>("Models/Egg");
			Matrix boneAbsoluteTransform = BlockMesh.GetBoneAbsoluteTransform(model.FindMesh("Egg").ParentBone);
			foreach (EggType eggType in m_eggTypes)
			{
				eggType.BlockMesh = new BlockMesh();
				eggType.BlockMesh.AppendModelMeshPart(model.FindMesh("Egg").MeshParts[0], boneAbsoluteTransform, makeEmissive: false, flipWindingOrder: false, doubleSided: false, flipNormals: false, eggType.Color);
				Matrix identity = Matrix.Identity;
				if (eggType.SwapUV)
				{
					identity.M11 = 0f;
					identity.M12 = 1f;
					identity.M21 = 1f;
					identity.M22 = 0f;
				}
				identity *= Matrix.CreateScale(0.0625f * eggType.ScaleUV.X, 0.0625f * eggType.ScaleUV.Y, 1f);
				identity *= Matrix.CreateTranslation((float)(eggType.TextureSlot % 16) / 16f, (float)(eggType.TextureSlot / 16) / 16f, 0f);
				eggType.BlockMesh.TransformTextureCoordinates(identity);
			}
			base.Initialize();
		}

		public override string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			EggType eggType = GetEggType(Terrain.ExtractData(value));
			int data = Terrain.ExtractData(value);
			bool isCooked = GetIsCooked(data);
			bool isLaid = GetIsLaid(data);
			if (isCooked)
			{
				return "Cooked " + eggType.DisplayName;
			}
			if (!isLaid)
			{
				return eggType.DisplayName;
			}
			return "Laid " + eggType.DisplayName;
		}

		public override float GetNutritionalValue(int value)
		{
			EggType eggType = GetEggType(Terrain.ExtractData(value));
			if (!GetIsCooked(Terrain.ExtractData(value)))
			{
				return eggType.NutritionalValue;
			}
			return 1.5f * eggType.NutritionalValue;
		}

		public override float GetSicknessProbability(int value)
		{
			if (!GetIsCooked(Terrain.ExtractData(value)))
			{
				return DefaultSicknessProbability;
			}
			return 0f;
		}

		public override int GetRotPeriod(int value)
		{
			if (GetNutritionalValue(value) > 0f)
			{
				return base.GetRotPeriod(value);
			}
			return 0;
		}

		public override int GetDamage(int value)
		{
			return (Terrain.ExtractData(value) >> 16) & 1;
		}

		public override int SetDamage(int value, int damage)
		{
			int num = Terrain.ExtractData(value);
			num = ((num & -65537) | ((damage & 1) << 16));
			return Terrain.ReplaceData(value, num);
		}

		public override int GetDamageDestructionValue(int value)
		{
			return 246;
		}

		public override IEnumerable<int> GetCreativeValues()
		{
			foreach (EggType eggType in m_eggTypes)
			{
				if (eggType.ShowEgg)
				{
					yield return Terrain.MakeBlockValue(118, 0, SetEggType(0, eggType.EggTypeIndex));
					if (eggType.NutritionalValue > 0f)
					{
						yield return Terrain.MakeBlockValue(118, 0, SetIsCooked(SetEggType(0, eggType.EggTypeIndex), isCooked: true));
					}
				}
			}
		}

		public override IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
		{
			foreach (EggType eggType in ((EggBlock)BlocksManager.Blocks[118]).EggTypes)
			{
				if (eggType.NutritionalValue > 0f)
				{
					int rot = 0;
					while (rot <= 1)
					{
						CraftingRecipe craftingRecipe = new CraftingRecipe
						{
							ResultCount = 1,
							ResultValue = Terrain.MakeBlockValue(118, 0, SetEggType(SetIsCooked(0, isCooked: true), eggType.EggTypeIndex)),
							RemainsCount = 1,
							RemainsValue = Terrain.MakeBlockValue(91),
							RequiredHeatLevel = 1f,
							Description = "Cook an egg to increase its nutritional value"
						};
						int data = SetEggType(SetIsLaid(0, isLaid: true), eggType.EggTypeIndex);
						int value = SetDamage(Terrain.MakeBlockValue(118, 0, data), rot);
						craftingRecipe.Ingredients[0] = "egg:" + Terrain.ExtractData(value).ToString(CultureInfo.InvariantCulture);
						craftingRecipe.Ingredients[1] = "waterbucket";
						yield return craftingRecipe;
						int num = rot + 1;
						rot = num;
					}
				}
			}
		}

		public override void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z)
		{
		}

		public override void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			int data = Terrain.ExtractData(value);
			EggType eggType = GetEggType(data);
			BlocksManager.DrawMeshBlock(primitivesRenderer, eggType.BlockMesh, color, eggType.Scale * size, ref matrix, environmentData);
		}

		public EggType GetEggType(int data)
		{
			int index = (data >> 4) & 0xFFF;
			return m_eggTypes[index];
		}

		public EggType GetEggTypeByCreatureTemplateName(string templateName)
		{
			return m_eggTypes.FirstOrDefault((EggType e) => e.TemplateName == templateName);
		}

		public static bool GetIsCooked(int data)
		{
			return (data & 1) != 0;
		}

		public static int SetIsCooked(int data, bool isCooked)
		{
			if (!isCooked)
			{
				return data & -2;
			}
			return data | 1;
		}

		public static bool GetIsLaid(int data)
		{
			return (data & 2) != 0;
		}

		public static int SetIsLaid(int data, bool isLaid)
		{
			if (!isLaid)
			{
				return data & -3;
			}
			return data | 2;
		}

		public static int SetEggType(int data, int eggTypeIndex)
		{
			data &= -65521;
			data |= (eggTypeIndex & 0xFFF) << 4;
			return data;
		}
	}
}
