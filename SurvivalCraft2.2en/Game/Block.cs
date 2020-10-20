using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;

namespace Game
{
	public abstract class Block
	{
		public int BlockIndex;

		public string DefaultDisplayName = string.Empty;

		public string DefaultDescription = string.Empty;

		public string DefaultCategory = string.Empty;

		public int DisplayOrder;

		public Vector3 DefaultIconBlockOffset = Vector3.Zero;

		public Vector3 DefaultIconViewOffset = new Vector3(1f);

		public float DefaultIconViewScale = 1f;

		public float FirstPersonScale = 1f;

		public Vector3 FirstPersonOffset = Vector3.Zero;

		public Vector3 FirstPersonRotation = Vector3.Zero;

		public float InHandScale = 1f;

		public Vector3 InHandOffset = Vector3.Zero;

		public Vector3 InHandRotation = Vector3.Zero;

		public string Behaviors = string.Empty;

		public string CraftingId = string.Empty;

		public int DefaultCreativeData;

		public bool IsCollidable = true;

		public bool IsPlaceable = true;

		public bool IsDiggingTransparent;

		public bool IsPlacementTransparent;

		public bool DefaultIsInteractive;

		public bool IsEditable;

		public bool IsNonDuplicable;

		public bool IsGatherable;

		public bool HasCollisionBehavior;

		public bool KillsWhenStuck;

		public bool IsFluidBlocker = true;

		public bool IsTransparent;

		public int DefaultShadowStrength;

		public int LightAttenuation;

		public int DefaultEmittedLightAmount;

		public float ObjectShadowStrength;

		public int DefaultDropContent;

		public float DefaultDropCount = 1f;

		public float DefaultExperienceCount;

		public int RequiredToolLevel;

		public int MaxStacking = 40;

		public float SleepSuitability;

		public float FrictionFactor = 1f;

		public float Density = 4f;

		public bool NoAutoJump;

		public bool NoSmoothRise;

		public int DefaultTextureSlot;

		public float DestructionDebrisScale = 1f;

		public float FuelHeatLevel;

		public float FuelFireDuration;

		public string DefaultSoundMaterialName;

		public float ShovelPower = 1f;

		public float QuarryPower = 1f;

		public float HackPower = 1f;

		public float DefaultMeleePower = 1f;

		public float DefaultMeleeHitProbability = 0.66f;

		public float DefaultProjectilePower = 1f;

		public int ToolLevel;

		public int PlayerLevelRequired = 1;

		public int Durability = -1;

		public BlockDigMethod DigMethod;

		public float DigResilience = 1f;

		public float ProjectileResilience = 1f;

		public bool IsAimable;

		public bool IsStickable;

		public bool AlignToVelocity;

		public float ProjectileSpeed = 15f;

		public float ProjectileDamping = 0.8f;

		public float ProjectileTipOffset;

		public bool DisintegratesOnHit;

		public float ProjectileStickProbability;

		public float DefaultHeat;

		public float FireDuration;

		public float ExplosionResilience;

		public float DefaultExplosionPressure;

		public bool DefaultExplosionIncendiary;

		public bool IsExplosionTransparent;

		public float DefaultNutritionalValue;

		public FoodType FoodType;

		public int DefaultRotPeriod;

		public float DefaultSicknessProbability;

		protected Random Random = new Random();

		public static BoundingBox[] m_defaultCollisionBoxes = new BoundingBox[1]
		{
			new BoundingBox(Vector3.Zero, Vector3.One)
		};

		public virtual void Initialize()
		{
			if (Durability < -1 || Durability > 65535)
			{
				throw new InvalidOperationException($"Block {DefaultDisplayName} has ToolDurability < -1 or > 65535.");
			}
		}

		public virtual string GetDisplayName(SubsystemTerrain subsystemTerrain, int value)
		{
			return DefaultDisplayName;
		}

		public virtual string GetDescription(int value)
		{
			return DefaultDescription;
		}

		public virtual string GetCategory(int value)
		{
			return DefaultCategory;
		}

		public virtual IEnumerable<int> GetCreativeValues()
		{
			if (DefaultCreativeData >= 0)
			{
				yield return Terrain.ReplaceContents(Terrain.ReplaceData(0, DefaultCreativeData), BlockIndex);
			}
		}

		public virtual bool IsInteractive(SubsystemTerrain subsystemTerrain, int value)
		{
			return DefaultIsInteractive;
		}

		public virtual IEnumerable<CraftingRecipe> GetProceduralCraftingRecipes()
		{
			yield break;
		}

		public virtual CraftingRecipe GetAdHocCraftingRecipe(SubsystemTerrain subsystemTerrain, string[] ingredients, float heatLevel, float playerLevel)
		{
			return null;
		}

		public virtual bool IsFaceTransparent(SubsystemTerrain subsystemTerrain, int face, int value)
		{
			return IsTransparent;
		}

		public virtual bool ShouldGenerateFace(SubsystemTerrain subsystemTerrain, int face, int value, int neighborValue)
		{
			int num = Terrain.ExtractContents(neighborValue);
			return BlocksManager.Blocks[num].IsFaceTransparent(subsystemTerrain, CellFace.OppositeFace(face), neighborValue);
		}

		public virtual int GetShadowStrength(int value)
		{
			return DefaultShadowStrength;
		}

		public virtual int GetFaceTextureSlot(int face, int value)
		{
			return DefaultTextureSlot;
		}

		public virtual string GetSoundMaterialName(SubsystemTerrain subsystemTerrain, int value)
		{
			return DefaultSoundMaterialName;
		}

		public abstract void GenerateTerrainVertices(BlockGeometryGenerator generator, TerrainGeometry geometry, int value, int x, int y, int z);

		public abstract void DrawBlock(PrimitivesRenderer3D primitivesRenderer, int value, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData);

		public virtual BlockPlacementData GetPlacementValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default(BlockPlacementData);
			result.Value = value;
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public virtual BlockPlacementData GetDigValue(SubsystemTerrain subsystemTerrain, ComponentMiner componentMiner, int value, int toolValue, TerrainRaycastResult raycastResult)
		{
			BlockPlacementData result = default(BlockPlacementData);
			result.Value = 0;
			result.CellFace = raycastResult.CellFace;
			return result;
		}

		public virtual void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			showDebris = (DestructionDebrisScale > 0f);
			if (toolLevel < RequiredToolLevel)
			{
				return;
			}
			BlockDropValue item;
			if (DefaultDropContent != 0)
			{
				int num = (int)DefaultDropCount;
				if (Random.Bool(DefaultDropCount - (float)num))
				{
					num++;
				}
				for (int i = 0; i < num; i++)
				{
					item = new BlockDropValue
					{
						Value = Terrain.MakeBlockValue(DefaultDropContent),
						Count = 1
					};
					dropValues.Add(item);
				}
			}
			int num2 = (int)DefaultExperienceCount;
			if (Random.Bool(DefaultExperienceCount - (float)num2))
			{
				num2++;
			}
			for (int j = 0; j < num2; j++)
			{
				item = new BlockDropValue
				{
					Value = Terrain.MakeBlockValue(248),
					Count = 1
				};
				dropValues.Add(item);
			}
		}

		public virtual int GetDamage(int value)
		{
			return (Terrain.ExtractData(value) >> 4) & 0xFFF;
		}

		public virtual int SetDamage(int value, int damage)
		{
			int num = Terrain.ExtractData(value);
			num &= 0xF;
			num |= MathUtils.Clamp(damage, 0, 4095) << 4;
			return Terrain.ReplaceData(value, num);
		}

		public virtual int GetDamageDestructionValue(int value)
		{
			return 0;
		}

		public virtual int GetRotPeriod(int value)
		{
			return DefaultRotPeriod;
		}

		public virtual float GetSicknessProbability(int value)
		{
			return DefaultSicknessProbability;
		}

		public virtual float GetMeleePower(int value)
		{
			return DefaultMeleePower;
		}

		public virtual float GetMeleeHitProbability(int value)
		{
			return DefaultMeleeHitProbability;
		}

		public virtual float GetProjectilePower(int value)
		{
			return DefaultProjectilePower;
		}

		public virtual float GetHeat(int value)
		{
			return DefaultHeat;
		}

		public virtual float GetExplosionPressure(int value)
		{
			return DefaultExplosionPressure;
		}

		public virtual bool GetExplosionIncendiary(int value)
		{
			return DefaultExplosionIncendiary;
		}

		public virtual Vector3 GetIconBlockOffset(int value, DrawBlockEnvironmentData environmentData)
		{
			return DefaultIconBlockOffset;
		}

		public virtual Vector3 GetIconViewOffset(int value, DrawBlockEnvironmentData environmentData)
		{
			return DefaultIconViewOffset;
		}

		public virtual float GetIconViewScale(int value, DrawBlockEnvironmentData environmentData)
		{
			return DefaultIconViewScale;
		}

		public virtual BlockDebrisParticleSystem CreateDebrisParticleSystem(SubsystemTerrain subsystemTerrain, Vector3 position, int value, float strength)
		{
			return new BlockDebrisParticleSystem(subsystemTerrain, position, strength, DestructionDebrisScale, Color.White, GetFaceTextureSlot(4, value));
		}

		public virtual BoundingBox[] GetCustomCollisionBoxes(SubsystemTerrain terrain, int value)
		{
			return m_defaultCollisionBoxes;
		}

		public virtual BoundingBox[] GetCustomInteractionBoxes(SubsystemTerrain terrain, int value)
		{
			return GetCustomCollisionBoxes(terrain, value);
		}

		public virtual int GetEmittedLightAmount(int value)
		{
			return DefaultEmittedLightAmount;
		}

		public virtual float GetNutritionalValue(int value)
		{
			return DefaultNutritionalValue;
		}

		public virtual bool ShouldAvoid(int value)
		{
			return false;
		}

		public virtual bool IsSwapAnimationNeeded(int oldValue, int newValue)
		{
			return true;
		}

		public virtual bool IsHeatBlocker(int value)
		{
			return IsCollidable;
		}

		public float? Raycast(Ray3 ray, SubsystemTerrain subsystemTerrain, int value, bool useInteractionBoxes, out int nearestBoxIndex, out BoundingBox nearestBox)
		{
			float? result = null;
			nearestBoxIndex = 0;
			nearestBox = default(BoundingBox);
			BoundingBox[] array = useInteractionBoxes ? GetCustomInteractionBoxes(subsystemTerrain, value) : GetCustomCollisionBoxes(subsystemTerrain, value);
			for (int i = 0; i < array.Length; i++)
			{
				float? num = ray.Intersection(array[i]);
				if (num.HasValue && (!result.HasValue || num.Value < result.Value))
				{
					nearestBoxIndex = i;
					result = num;
				}
			}
			nearestBox = array[nearestBoxIndex];
			return result;
		}
	}
}
