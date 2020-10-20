using Engine;
using GameEntitySystem;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentMiner : Component, IUpdateable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemMovingBlocks m_subsystemMovingBlocks;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTime m_subsystemTime;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemSoundMaterials m_subsystemSoundMaterials;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public Random m_random = new Random();

		public static Random s_random = new Random();

		public double m_digStartTime;

		public float m_digProgress;

		public double m_lastHitTime;

		public int m_lastDigFrameIndex;

		public float m_lastPokingPhase;

		public ComponentCreature ComponentCreature
		{
			get;
			set;
		}

		public ComponentPlayer ComponentPlayer
		{
			get;
			set;
		}

		public IInventory Inventory
		{
			get;
			set;
		}

		public int ActiveBlockValue
		{
			get
			{
				if (Inventory == null)
				{
					return 0;
				}
				return Inventory.GetSlotValue(Inventory.ActiveSlotIndex);
			}
		}

		public float AttackPower
		{
			get;
			set;
		}

		public float PokingPhase
		{
			get;
			set;
		}

		public CellFace? DigCellFace
		{
			get;
			set;
		}

		public float DigTime
		{
			get
			{
				if (!DigCellFace.HasValue)
				{
					return 0f;
				}
				return (float)(m_subsystemTime.GameTime - m_digStartTime);
			}
		}

		public float DigProgress
		{
			get
			{
				if (!DigCellFace.HasValue)
				{
					return 0f;
				}
				return m_digProgress;
			}
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void Poke(bool forceRestart)
		{
			if (forceRestart)
			{
				PokingPhase = 0.0001f;
			}
			else
			{
				PokingPhase = MathUtils.Max(0.0001f, PokingPhase);
			}
		}

		public bool Dig(TerrainRaycastResult raycastResult)
		{
			bool result = false;
			m_lastDigFrameIndex = Time.FrameIndex;
			CellFace cellFace = raycastResult.CellFace;
			int cellValue = m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
			int num = Terrain.ExtractContents(cellValue);
			Block block = BlocksManager.Blocks[num];
			int activeBlockValue = ActiveBlockValue;
			int num2 = Terrain.ExtractContents(activeBlockValue);
			Block block2 = BlocksManager.Blocks[num2];
			if (!DigCellFace.HasValue || DigCellFace.Value.X != cellFace.X || DigCellFace.Value.Y != cellFace.Y || DigCellFace.Value.Z != cellFace.Z)
			{
				m_digStartTime = m_subsystemTime.GameTime;
				DigCellFace = cellFace;
			}
			float num3 = CalculateDigTime(cellValue, num2);
			m_digProgress = ((num3 > 0f) ? MathUtils.Saturate((float)(m_subsystemTime.GameTime - m_digStartTime) / num3) : 1f);
			if (!CanUseTool(activeBlockValue))
			{
				m_digProgress = 0f;
				if (m_subsystemTime.PeriodicGameTimeEvent(5.0, m_digStartTime + 1.0))
				{
					ComponentPlayer?.ComponentGui.DisplaySmallMessage($"Must be level {block2.PlayerLevelRequired} to use {block2.GetDisplayName(m_subsystemTerrain, activeBlockValue)}", Color.White, blinking: true, playNotificationSound: true);
				}
			}
			bool flag = ComponentPlayer != null && !ComponentPlayer.ComponentInput.IsControlledByTouch && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative;
			if (flag || (m_lastPokingPhase <= 0.5f && PokingPhase > 0.5f))
			{
				if (m_digProgress >= 1f)
				{
					DigCellFace = null;
					if (flag)
					{
						Poke(forceRestart: true);
					}
					BlockPlacementData digValue = block.GetDigValue(m_subsystemTerrain, this, cellValue, activeBlockValue, raycastResult);
					m_subsystemTerrain.DestroyCell(block2.ToolLevel, digValue.CellFace.X, digValue.CellFace.Y, digValue.CellFace.Z, digValue.Value, noDrop: false, noParticleSystem: false);
					m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f);
					DamageActiveTool(1);
					if (ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksDug++;
					}
					result = true;
				}
				else
				{
					m_subsystemSoundMaterials.PlayImpactSound(cellValue, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 1f);
					BlockDebrisParticleSystem particleSystem = block.CreateDebrisParticleSystem(m_subsystemTerrain, raycastResult.HitPoint(0.1f), cellValue, 0.35f);
					base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
				}
			}
			return result;
		}

		public bool Place(TerrainRaycastResult raycastResult)
		{
			if (Place(raycastResult, ActiveBlockValue))
			{
				if (Inventory != null)
				{
					Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
				}
				return true;
			}
			return false;
		}

		public bool Place(TerrainRaycastResult raycastResult, int value)
		{
			int num = Terrain.ExtractContents(value);
			if (BlocksManager.Blocks[num].IsPlaceable)
			{
				Block block = BlocksManager.Blocks[num];
				BlockPlacementData placementData = block.GetPlacementValue(m_subsystemTerrain, this, value, raycastResult);
				if (placementData.Value != 0)
				{
					Point3 point = CellFace.FaceToPoint3(placementData.CellFace.Face);
					int num2 = placementData.CellFace.X + point.X;
					int num3 = placementData.CellFace.Y + point.Y;
					int num4 = placementData.CellFace.Z + point.Z;
					if (num3 > 0 && num3 < 255 && (IsBlockPlacingAllowed(ComponentCreature.ComponentBody) || m_subsystemGameInfo.WorldSettings.GameMode <= GameMode.Harmless))
					{
						bool flag = false;
						if (block.IsCollidable)
						{
							BoundingBox boundingBox = ComponentCreature.ComponentBody.BoundingBox;
							boundingBox.Min += new Vector3(0.2f);
							boundingBox.Max -= new Vector3(0.2f);
							BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, placementData.Value);
							for (int i = 0; i < customCollisionBoxes.Length; i++)
							{
								BoundingBox box = customCollisionBoxes[i];
								box.Min += new Vector3(num2, num3, num4);
								box.Max += new Vector3(num2, num3, num4);
								if (boundingBox.Intersection(box))
								{
									flag = true;
									break;
								}
							}
						}
						if (!flag)
						{
							SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(Terrain.ExtractContents(placementData.Value));
							for (int i = 0; i < blockBehaviors.Length; i++)
							{
								blockBehaviors[i].OnItemPlaced(num2, num3, num4, ref placementData, value);
							}
							m_subsystemTerrain.DestroyCell(0, num2, num3, num4, placementData.Value, noDrop: false, noParticleSystem: false);
							m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(placementData.CellFace.X, placementData.CellFace.Y, placementData.CellFace.Z), 5f, autoDelay: false);
							Poke(forceRestart: false);
							if (ComponentCreature.PlayerStats != null)
							{
								ComponentCreature.PlayerStats.BlocksPlaced++;
							}
							return true;
						}
					}
				}
			}
			return false;
		}

		public bool Use(Ray3 ray)
		{
			int num = Terrain.ExtractContents(ActiveBlockValue);
			Block block = BlocksManager.Blocks[num];
			if (!CanUseTool(ActiveBlockValue))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage($"Must be level {block.PlayerLevelRequired} to use {block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)}", Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				return false;
			}
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i].OnUse(ray, this))
				{
					Poke(forceRestart: false);
					return true;
				}
			}
			return false;
		}

		public bool Interact(TerrainRaycastResult raycastResult)
		{
			int cellContents = m_subsystemTerrain.Terrain.GetCellContents(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
			SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(cellContents);
			for (int i = 0; i < blockBehaviors.Length; i++)
			{
				if (blockBehaviors[i].OnInteract(raycastResult, this))
				{
					if (ComponentCreature.PlayerStats != null)
					{
						ComponentCreature.PlayerStats.BlocksInteracted++;
					}
					Poke(forceRestart: false);
					return true;
				}
			}
			return false;
		}

		public void Hit(ComponentBody componentBody, Vector3 hitPoint, Vector3 hitDirection)
		{
			if (!(m_subsystemTime.GameTime - m_lastHitTime > 0.6600000262260437))
			{
				return;
			}
			m_lastHitTime = m_subsystemTime.GameTime;
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(ActiveBlockValue)];
			if (!CanUseTool(ActiveBlockValue))
			{
				ComponentPlayer?.ComponentGui.DisplaySmallMessage($"Must be level {block.PlayerLevelRequired} to use {block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)}", Color.White, blinking: true, playNotificationSound: true);
				Poke(forceRestart: false);
				return;
			}
			float num = 0f;
			float num2 = 1f;
			if (ActiveBlockValue != 0)
			{
				num = block.GetMeleePower(ActiveBlockValue) * AttackPower * m_random.Float(0.8f, 1.2f);
				num2 = block.GetMeleeHitProbability(ActiveBlockValue);
			}
			else
			{
				num = AttackPower * m_random.Float(0.8f, 1.2f);
				num2 = 0.66f;
			}
			bool flag;
			if (ComponentPlayer != null)
			{
				m_subsystemAudio.PlaySound("Audio/Swoosh", 1f, m_random.Float(-0.2f, 0.2f), componentBody.Position, 3f, autoDelay: false);
				flag = m_random.Bool(num2);
				num *= ComponentPlayer.ComponentLevel.StrengthFactor;
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				AttackBody(componentBody, ComponentCreature, hitPoint, hitDirection, num, isMeleeAttack: true);
				DamageActiveTool(1);
			}
			else if (ComponentCreature is ComponentPlayer)
			{
				HitValueParticleSystem particleSystem = new HitValueParticleSystem(hitPoint + 0.75f * hitDirection, 1f * hitDirection + ComponentCreature.ComponentBody.Velocity, Color.White, "Miss");
				base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
			}
			if (ComponentCreature.PlayerStats != null)
			{
				ComponentCreature.PlayerStats.MeleeAttacks++;
				if (flag)
				{
					ComponentCreature.PlayerStats.MeleeHits++;
				}
			}
			Poke(forceRestart: false);
		}

		public bool Aim(Ray3 aim, AimState state)
		{
			int num = Terrain.ExtractContents(ActiveBlockValue);
			Block block = BlocksManager.Blocks[num];
			if (block.IsAimable)
			{
				if (!CanUseTool(ActiveBlockValue))
				{
					ComponentPlayer?.ComponentGui.DisplaySmallMessage($"Must be level {block.PlayerLevelRequired} to use {block.GetDisplayName(m_subsystemTerrain, ActiveBlockValue)}", Color.White, blinking: true, playNotificationSound: true);
					Poke(forceRestart: false);
					return true;
				}
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					if (blockBehaviors[i].OnAim(aim, this, state))
					{
						return true;
					}
				}
			}
			return false;
		}

		public object Raycast(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true)
		{
			float reach = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? SettingsManager.CreativeReach : 5f;
			Vector3 creaturePosition = ComponentCreature.ComponentCreatureModel.EyePosition;
			Vector3 start = ray.Position;
			Vector3 direction = Vector3.Normalize(ray.Direction);
			Vector3 end = ray.Position + direction * 15f;
			Point3 startCell = Terrain.ToCell(start);
			BodyRaycastResult? bodyRaycastResult = m_subsystemBodies.Raycast(start, end, 0.35f, (ComponentBody body, float distance) => (Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach && body.Entity != base.Entity && !body.IsChildOfBody(ComponentCreature.ComponentBody) && !ComponentCreature.ComponentBody.IsChildOfBody(body) && Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f) ? true : false);
			MovingBlocksRaycastResult? movingBlocksRaycastResult = m_subsystemMovingBlocks.Raycast(start, end, extendToFillCells: true);
			TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(start, end, useInteractionBoxes: true, skipAirBlocks: true, delegate(int value, float distance)
			{
				if (Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach)
				{
					Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
					if (distance == 0f && block is CrossBlock && Vector3.Dot(direction, new Vector3(startCell) + new Vector3(0.5f) - start) < 0f)
					{
						return false;
					}
					if (mode == RaycastMode.Digging)
					{
						return !block.IsDiggingTransparent;
					}
					if (mode == RaycastMode.Interaction)
					{
						if (block.IsPlacementTransparent)
						{
							return block.IsInteractive(m_subsystemTerrain, value);
						}
						return true;
					}
					if (mode == RaycastMode.Gathering)
					{
						return block.IsGatherable;
					}
				}
				return false;
			});
			float num = bodyRaycastResult.HasValue ? bodyRaycastResult.Value.Distance : (1f / 0f);
			float num2 = movingBlocksRaycastResult.HasValue ? movingBlocksRaycastResult.Value.Distance : (1f / 0f);
			float num3 = terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : (1f / 0f);
			if (num < num2 && num < num3)
			{
				return bodyRaycastResult.Value;
			}
			if (num2 < num && num2 < num3)
			{
				return movingBlocksRaycastResult.Value;
			}
			if (num3 < num && num3 < num2)
			{
				return terrainRaycastResult.Value;
			}
			return new Ray3(start, direction);
		}

		public T? Raycast<T>(Ray3 ray, RaycastMode mode, bool raycastTerrain = true, bool raycastBodies = true, bool raycastMovingBlocks = true) where T : struct
		{
			object obj = Raycast(ray, mode, raycastTerrain, raycastBodies, raycastMovingBlocks);
			if (!(obj is T))
			{
				return null;
			}
			return (T)obj;
		}

		public void RemoveActiveTool(int removeCount)
		{
			if (Inventory != null)
			{
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, removeCount);
			}
		}

		public void DamageActiveTool(int damageCount)
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative || Inventory == null)
			{
				return;
			}
			int num = BlocksManager.DamageItem(ActiveBlockValue, damageCount);
			if (num != 0)
			{
				int slotCount = Inventory.GetSlotCount(Inventory.ActiveSlotIndex);
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, slotCount);
				if (Inventory.GetSlotCount(Inventory.ActiveSlotIndex) == 0)
				{
					Inventory.AddSlotItems(Inventory.ActiveSlotIndex, num, slotCount);
				}
			}
			else
			{
				Inventory.RemoveSlotItems(Inventory.ActiveSlotIndex, 1);
			}
		}

		public static void AttackBody(ComponentBody target, ComponentCreature attacker, Vector3 hitPoint, Vector3 hitDirection, float attackPower, bool isMeleeAttack)
		{
			if (attacker != null && attacker is ComponentPlayer && target.Entity.FindComponent<ComponentPlayer>() != null && !target.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true).WorldSettings.IsFriendlyFireEnabled)
			{
				attacker.Entity.FindComponent<ComponentGui>(throwOnError: true).DisplaySmallMessage("Player-on-player attacks are disabled", Color.White, blinking: true, playNotificationSound: true);
				return;
			}
			if (attackPower > 0f)
			{
				ComponentClothing componentClothing = target.Entity.FindComponent<ComponentClothing>();
				if (componentClothing != null)
				{
					attackPower = componentClothing.ApplyArmorProtection(attackPower);
				}
				ComponentLevel componentLevel = target.Entity.FindComponent<ComponentLevel>();
				if (componentLevel != null)
				{
					attackPower /= componentLevel.ResilienceFactor;
				}
				ComponentHealth componentHealth = target.Entity.FindComponent<ComponentHealth>();
				if (componentHealth != null)
				{
					float num = attackPower / componentHealth.AttackResilience;
					string cause;
					if (attacker != null)
					{
						string str = attacker.KillVerbs[s_random.Int(0, attacker.KillVerbs.Count - 1)];
						string text = attacker.DisplayName.ToLower();
						string str2 = (text.Length > 0 && "aeiouy".Contains(text.Remove(1))) ? " by an " : " by a ";
						cause = str + str2 + text;
					}
					else
					{
						switch (s_random.Int(0, 5))
						{
						case 0:
							cause = "force majeure";
							break;
						case 1:
							cause = "accident";
							break;
						case 2:
							cause = "bad luck";
							break;
						case 3:
							cause = "unknown attacker";
							break;
						case 4:
							cause = "wrong place wrong time";
							break;
						default:
							cause = "unlucky";
							break;
						}
					}
					float health = componentHealth.Health;
					componentHealth.Injure(num, attacker, ignoreInvulnerability: false, cause);
					if (num > 0f)
					{
						target.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound("Audio/Impacts/Body", 1f, s_random.Float(-0.3f, 0.3f), target.Position, 4f, autoDelay: false);
						float num2 = (health - componentHealth.Health) * componentHealth.AttackResilience;
						if (attacker is ComponentPlayer && num2 > 0f)
						{
							string text2 = (0f - num2).ToString("0", CultureInfo.InvariantCulture);
							HitValueParticleSystem particleSystem = new HitValueParticleSystem(hitPoint + 0.75f * hitDirection, 1f * hitDirection + attacker.ComponentBody.Velocity, Color.White, text2);
							target.Project.FindSubsystem<SubsystemParticles>(throwOnError: true).AddParticleSystem(particleSystem);
						}
					}
				}
				ComponentDamage componentDamage = target.Entity.FindComponent<ComponentDamage>();
				if (componentDamage != null)
				{
					float num3 = attackPower / componentDamage.AttackResilience;
					componentDamage.Damage(num3);
					if (num3 > 0f)
					{
						target.Project.FindSubsystem<SubsystemAudio>(throwOnError: true).PlayRandomSound(componentDamage.DamageSoundName, 1f, s_random.Float(-0.3f, 0.3f), target.Position, 4f, autoDelay: false);
					}
				}
			}
			float num4 = 0f;
			float x = 0f;
			if (isMeleeAttack && attacker != null)
			{
				float num5 = (attackPower >= 2f) ? 1.25f : 1f;
				float num6 = MathUtils.Pow(attacker.ComponentBody.Mass / target.Mass, 0.5f);
				float x2 = num5 * num6;
				num4 = 5.5f * MathUtils.Saturate(x2);
				x = 0.25f * MathUtils.Saturate(x2);
			}
			else if (attackPower > 0f)
			{
				num4 = 2f;
				x = 0.2f;
			}
			if (num4 > 0f)
			{
				target.ApplyImpulse(num4 * Vector3.Normalize(hitDirection + s_random.Vector3(0.1f) + 0.2f * Vector3.UnitY));
				ComponentLocomotion componentLocomotion = target.Entity.FindComponent<ComponentLocomotion>();
				if (componentLocomotion != null)
				{
					componentLocomotion.StunTime = MathUtils.Max(componentLocomotion.StunTime, x);
				}
			}
		}

		public void Update(float dt)
		{
			float num = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? (0.5f / SettingsManager.CreativeDigTime) : 4f;
			m_lastPokingPhase = PokingPhase;
			if (DigCellFace.HasValue || PokingPhase > 0f)
			{
				PokingPhase += num * m_subsystemTime.GameTimeDelta;
				if (PokingPhase > 1f)
				{
					if (DigCellFace.HasValue)
					{
						PokingPhase = MathUtils.Remainder(PokingPhase, 1f);
					}
					else
					{
						PokingPhase = 0f;
					}
				}
			}
			if (DigCellFace.HasValue && Time.FrameIndex - m_lastDigFrameIndex > 1)
			{
				DigCellFace = null;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemSoundMaterials = base.Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			ComponentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			ComponentPlayer = base.Entity.FindComponent<ComponentPlayer>();
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && ComponentPlayer != null)
			{
				Inventory = base.Entity.FindComponent<ComponentCreativeInventory>();
			}
			else
			{
				Inventory = base.Entity.FindComponent<ComponentInventory>();
			}
			AttackPower = valuesDictionary.GetValue<float>("AttackPower");
		}

		public static bool IsBlockPlacingAllowed(ComponentBody componentBody)
		{
			if (componentBody.StandingOnBody != null || componentBody.StandingOnValue.HasValue)
			{
				return true;
			}
			if (componentBody.ImmersionFactor > 0.01f)
			{
				return true;
			}
			if (componentBody.ParentBody != null && IsBlockPlacingAllowed(componentBody.ParentBody))
			{
				return true;
			}
			ComponentLocomotion componentLocomotion = componentBody.Entity.FindComponent<ComponentLocomotion>();
			if (componentLocomotion != null && componentLocomotion.LadderValue.HasValue)
			{
				return true;
			}
			return false;
		}

		public float CalculateDigTime(int digValue, int toolContents)
		{
			Block block = BlocksManager.Blocks[toolContents];
			Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(digValue)];
			float digResilience = block2.DigResilience;
			if (ComponentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				if (digResilience < 1f / 0f)
				{
					return 0f;
				}
				return 1f / 0f;
			}
			if (ComponentPlayer != null && m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Adventure)
			{
				float num = 0f;
				if (block2.DigMethod == BlockDigMethod.Shovel && block.ShovelPower >= 2f)
				{
					num = block.ShovelPower;
				}
				else if (block2.DigMethod == BlockDigMethod.Quarry && block.QuarryPower >= 2f)
				{
					num = block.QuarryPower;
				}
				else if (block2.DigMethod == BlockDigMethod.Hack && block.HackPower >= 2f)
				{
					num = block.HackPower;
				}
				num *= ComponentPlayer.ComponentLevel.StrengthFactor;
				if (!(num > 0f))
				{
					return 1f / 0f;
				}
				return MathUtils.Max(digResilience / num, 0f);
			}
			float num2 = 0f;
			if (block2.DigMethod == BlockDigMethod.Shovel)
			{
				num2 = block.ShovelPower;
			}
			else if (block2.DigMethod == BlockDigMethod.Quarry)
			{
				num2 = block.QuarryPower;
			}
			else if (block2.DigMethod == BlockDigMethod.Hack)
			{
				num2 = block.HackPower;
			}
			if (ComponentPlayer != null)
			{
				num2 *= ComponentPlayer.ComponentLevel.StrengthFactor;
			}
			if (!(num2 > 0f))
			{
				return 1f / 0f;
			}
			return MathUtils.Max(digResilience / num2, 0f);
		}

		public bool CanUseTool(int toolValue)
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(toolValue)];
				if (ComponentPlayer != null && ComponentPlayer.PlayerData.Level < (float)block.PlayerLevelRequired)
				{
					return false;
				}
			}
			return true;
		}
	}
}
