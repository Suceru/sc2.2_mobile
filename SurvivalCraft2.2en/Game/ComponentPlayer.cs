using Engine;
using GameEntitySystem;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ComponentPlayer : ComponentCreature, IUpdateable
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTime m_subsystemTime;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemTerrain m_subsystemTerrain;

		public bool m_aimHintIssued;

		public double m_lastActionTime;

		public bool m_speedOrderBlocked;

		public Ray3? m_aim;

		public bool m_isAimBlocked;

		public bool m_isDigBlocked;

		public PlayerData PlayerData
		{
			get;
			set;
		}

		public GameWidget GameWidget => PlayerData.GameWidget;

		public ContainerWidget GuiWidget => PlayerData.GameWidget.GuiWidget;

		public ViewWidget ViewWidget => PlayerData.GameWidget.ViewWidget;

		public ComponentGui ComponentGui
		{
			get;
			set;
		}

		public ComponentInput ComponentInput
		{
			get;
			set;
		}

		public ComponentBlockHighlight ComponentBlockHighlight
		{
			get;
			set;
		}

		public ComponentScreenOverlays ComponentScreenOverlays
		{
			get;
			set;
		}

		public ComponentAimingSights ComponentAimingSights
		{
			get;
			set;
		}

		public ComponentMiner ComponentMiner
		{
			get;
			set;
		}

		public ComponentRider ComponentRider
		{
			get;
			set;
		}

		public ComponentSleep ComponentSleep
		{
			get;
			set;
		}

		public ComponentVitalStats ComponentVitalStats
		{
			get;
			set;
		}

		public ComponentSickness ComponentSickness
		{
			get;
			set;
		}

		public ComponentFlu ComponentFlu
		{
			get;
			set;
		}

		public ComponentLevel ComponentLevel
		{
			get;
			set;
		}

		public ComponentClothing ComponentClothing
		{
			get;
			set;
		}

		public ComponentOuterClothingModel ComponentOuterClothingModel
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void Update(float dt)
		{
			PlayerInput playerInput = ComponentInput.PlayerInput;
			if (ComponentInput.IsControlledByTouch && m_aim.HasValue)
			{
				playerInput.Look = Vector2.Zero;
			}
			if (ComponentMiner.Inventory != null)
			{
				ComponentMiner.Inventory.ActiveSlotIndex += playerInput.ScrollInventory;
				if (playerInput.SelectInventorySlot.HasValue)
				{
					ComponentMiner.Inventory.ActiveSlotIndex = MathUtils.Clamp(playerInput.SelectInventorySlot.Value, 0, 9);
				}
			}
			ComponentSteedBehavior componentSteedBehavior = null;
			ComponentBoat componentBoat = null;
			ComponentMount mount = ComponentRider.Mount;
			if (mount != null)
			{
				componentSteedBehavior = mount.Entity.FindComponent<ComponentSteedBehavior>();
				componentBoat = mount.Entity.FindComponent<ComponentBoat>();
			}
			if (componentSteedBehavior != null)
			{
				if (playerInput.Move.Z > 0.5f && !m_speedOrderBlocked)
				{
					if (PlayerData.PlayerClass == PlayerClass.Male)
					{
						m_subsystemAudio.PlayRandomSound("Audio/Creatures/MaleYellFast", 0.75f, 0f, base.ComponentBody.Position, 2f, autoDelay: false);
					}
					else
					{
						m_subsystemAudio.PlayRandomSound("Audio/Creatures/FemaleYellFast", 0.75f, 0f, base.ComponentBody.Position, 2f, autoDelay: false);
					}
					componentSteedBehavior.SpeedOrder = 1;
					m_speedOrderBlocked = true;
				}
				else if (playerInput.Move.Z < -0.5f && !m_speedOrderBlocked)
				{
					if (PlayerData.PlayerClass == PlayerClass.Male)
					{
						m_subsystemAudio.PlayRandomSound("Audio/Creatures/MaleYellSlow", 0.75f, 0f, base.ComponentBody.Position, 2f, autoDelay: false);
					}
					else
					{
						m_subsystemAudio.PlayRandomSound("Audio/Creatures/FemaleYellSlow", 0.75f, 0f, base.ComponentBody.Position, 2f, autoDelay: false);
					}
					componentSteedBehavior.SpeedOrder = -1;
					m_speedOrderBlocked = true;
				}
				else if (MathUtils.Abs(playerInput.Move.Z) <= 0.25f)
				{
					m_speedOrderBlocked = false;
				}
				componentSteedBehavior.TurnOrder = playerInput.Move.X;
				componentSteedBehavior.JumpOrder = (playerInput.Jump ? 1 : 0);
				base.ComponentLocomotion.LookOrder = new Vector2(playerInput.Look.X, 0f);
			}
			else if (componentBoat != null)
			{
				componentBoat.TurnOrder = playerInput.Move.X;
				componentBoat.MoveOrder = playerInput.Move.Z;
				base.ComponentLocomotion.LookOrder = new Vector2(playerInput.Look.X, 0f);
				base.ComponentCreatureModel.RowLeftOrder = (playerInput.Move.X < -0.2f || playerInput.Move.Z > 0.2f);
				base.ComponentCreatureModel.RowRightOrder = (playerInput.Move.X > 0.2f || playerInput.Move.Z > 0.2f);
			}
			else
			{
				base.ComponentLocomotion.WalkOrder = (base.ComponentBody.IsSneaking ? (0.66f * new Vector2(playerInput.SneakMove.X, playerInput.SneakMove.Z)) : new Vector2(playerInput.Move.X, playerInput.Move.Z));
				base.ComponentLocomotion.FlyOrder = new Vector3(0f, playerInput.Move.Y, 0f);
				base.ComponentLocomotion.TurnOrder = playerInput.Look * new Vector2(1f, 0f);
				base.ComponentLocomotion.JumpOrder = MathUtils.Max(playerInput.Jump ? 1 : 0, base.ComponentLocomotion.JumpOrder);
			}
			base.ComponentLocomotion.LookOrder += playerInput.Look * (SettingsManager.FlipVerticalAxis ? new Vector2(0f, -1f) : new Vector2(0f, 1f));
			base.ComponentLocomotion.VrLookOrder = playerInput.VrLook;
			base.ComponentLocomotion.VrMoveOrder = playerInput.VrMove;
			int num = Terrain.ExtractContents(ComponentMiner.ActiveBlockValue);
			Block block = BlocksManager.Blocks[num];
			bool flag = false;
			if (playerInput.Interact.HasValue && !flag && m_subsystemTime.GameTime - m_lastActionTime > 0.33000001311302185)
			{
				if (!ComponentMiner.Use(playerInput.Interact.Value))
				{
					TerrainRaycastResult? terrainRaycastResult = ComponentMiner.Raycast<TerrainRaycastResult>(playerInput.Interact.Value, RaycastMode.Interaction);
					if (terrainRaycastResult.HasValue)
					{
						if (!ComponentMiner.Interact(terrainRaycastResult.Value))
						{
							if (ComponentMiner.Place(terrainRaycastResult.Value))
							{
								m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
								flag = true;
								m_isAimBlocked = true;
							}
						}
						else
						{
							m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
							flag = true;
							m_isAimBlocked = true;
						}
					}
				}
				else
				{
					m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
					flag = true;
					m_isAimBlocked = true;
				}
			}
			float num2 = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative) ? 0.1f : 1.4f;
			if (playerInput.Aim.HasValue && block.IsAimable && m_subsystemTime.GameTime - m_lastActionTime > (double)num2)
			{
				if (!m_isAimBlocked)
				{
					Ray3 value = playerInput.Aim.Value;
					Vector3 vector = GameWidget.ActiveCamera.WorldToScreen(value.Position + value.Direction, Matrix.Identity);
					Point2 size = Window.Size;
					if (ComponentInput.IsControlledByVr || (vector.X >= (float)size.X * 0.02f && vector.X < (float)size.X * 0.98f && vector.Y >= (float)size.Y * 0.02f && vector.Y < (float)size.Y * 0.98f))
					{
						m_aim = value;
						if (ComponentMiner.Aim(value, AimState.InProgress))
						{
							ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
							m_aim = null;
							m_isAimBlocked = true;
						}
						else if (!m_aimHintIssued && Time.PeriodicEvent(1.0, 0.0))
						{
							Time.QueueTimeDelayedExecution(Time.RealTime + 3.0, delegate
							{
								if (!m_aimHintIssued && m_aim.HasValue && !base.ComponentBody.IsSneaking)
								{
									m_aimHintIssued = true;
									ComponentGui.DisplaySmallMessage("Sneak to improve your aim", Color.White, blinking: true, playNotificationSound: true);
								}
							});
						}
					}
					else if (m_aim.HasValue)
					{
						ComponentMiner.Aim(m_aim.Value, AimState.Cancelled);
						m_aim = null;
						m_isAimBlocked = true;
					}
				}
			}
			else
			{
				m_isAimBlocked = false;
				if (m_aim.HasValue)
				{
					ComponentMiner.Aim(m_aim.Value, AimState.Completed);
					m_aim = null;
					m_lastActionTime = m_subsystemTime.GameTime;
				}
			}
			flag |= m_aim.HasValue;
			if (playerInput.Hit.HasValue && !flag && m_subsystemTime.GameTime - m_lastActionTime > 0.33000001311302185)
			{
				BodyRaycastResult? bodyRaycastResult = ComponentMiner.Raycast<BodyRaycastResult>(playerInput.Hit.Value, RaycastMode.Digging);
				if (bodyRaycastResult.HasValue)
				{
					flag = true;
					m_isDigBlocked = true;
					if (Vector3.Distance(bodyRaycastResult.Value.HitPoint(), base.ComponentCreatureModel.EyePosition) <= 2f)
					{
						ComponentMiner.Hit(bodyRaycastResult.Value.ComponentBody, bodyRaycastResult.Value.HitPoint(), playerInput.Hit.Value.Direction);
					}
				}
			}
			if (playerInput.Dig.HasValue && !flag && !m_isDigBlocked && m_subsystemTime.GameTime - m_lastActionTime > 0.33000001311302185)
			{
				TerrainRaycastResult? terrainRaycastResult2 = ComponentMiner.Raycast<TerrainRaycastResult>(playerInput.Dig.Value, RaycastMode.Digging);
				if (terrainRaycastResult2.HasValue && ComponentMiner.Dig(terrainRaycastResult2.Value))
				{
					m_lastActionTime = m_subsystemTime.GameTime;
					m_subsystemTerrain.TerrainUpdater.RequestSynchronousUpdate();
				}
			}
			if (!playerInput.Dig.HasValue)
			{
				m_isDigBlocked = false;
			}
			if (playerInput.Drop && ComponentMiner.Inventory != null)
			{
				IInventory inventory = ComponentMiner.Inventory;
				int slotValue = inventory.GetSlotValue(inventory.ActiveSlotIndex);
				int num3 = inventory.RemoveSlotItems(count: inventory.GetSlotCount(inventory.ActiveSlotIndex), slotIndex: inventory.ActiveSlotIndex);
				if (slotValue != 0 && num3 != 0)
				{
					Vector3 position = base.ComponentBody.Position + new Vector3(0f, base.ComponentBody.BoxSize.Y * 0.66f, 0f) + 0.25f * base.ComponentBody.Matrix.Forward;
					Vector3 value2 = 8f * Matrix.CreateFromQuaternion(base.ComponentCreatureModel.EyeRotation).Forward;
					m_subsystemPickables.AddPickable(slotValue, num3, position, value2, null);
				}
			}
			if (!playerInput.PickBlockType.HasValue || flag)
			{
				return;
			}
			ComponentCreativeInventory componentCreativeInventory = ComponentMiner.Inventory as ComponentCreativeInventory;
			if (componentCreativeInventory == null)
			{
				return;
			}
			TerrainRaycastResult? terrainRaycastResult3 = ComponentMiner.Raycast<TerrainRaycastResult>(playerInput.PickBlockType.Value, RaycastMode.Digging, raycastTerrain: true, raycastBodies: false, raycastMovingBlocks: false);
			if (!terrainRaycastResult3.HasValue)
			{
				return;
			}
			int value3 = terrainRaycastResult3.Value.Value;
			value3 = Terrain.ReplaceLight(value3, 0);
			int num4 = Terrain.ExtractContents(value3);
			Block block2 = BlocksManager.Blocks[num4];
			int num5 = 0;
			IEnumerable<int> creativeValues = block2.GetCreativeValues();
			if (block2.GetCreativeValues().Contains(value3))
			{
				num5 = value3;
			}
			if (num5 == 0 && !block2.IsNonDuplicable)
			{
				List<BlockDropValue> list = new List<BlockDropValue>();
				block2.GetDropValues(m_subsystemTerrain, value3, 0, int.MaxValue, list, out bool _);
				if (list.Count > 0 && list[0].Count > 0)
				{
					num5 = list[0].Value;
				}
			}
			if (num5 == 0)
			{
				num5 = creativeValues.FirstOrDefault();
			}
			if (num5 == 0)
			{
				return;
			}
			int num6 = -1;
			for (int i = 0; i < 10; i++)
			{
				if (componentCreativeInventory.GetSlotCapacity(i, num5) > 0 && componentCreativeInventory.GetSlotCount(i) > 0 && componentCreativeInventory.GetSlotValue(i) == num5)
				{
					num6 = i;
					break;
				}
			}
			if (num6 < 0)
			{
				for (int j = 0; j < 10; j++)
				{
					if (componentCreativeInventory.GetSlotCapacity(j, num5) > 0 && (componentCreativeInventory.GetSlotCount(j) == 0 || componentCreativeInventory.GetSlotValue(j) == 0))
					{
						num6 = j;
						break;
					}
				}
			}
			if (num6 < 0)
			{
				num6 = componentCreativeInventory.ActiveSlotIndex;
			}
			componentCreativeInventory.RemoveSlotItems(num6, int.MaxValue);
			componentCreativeInventory.AddSlotItems(num6, num5, 1);
			componentCreativeInventory.ActiveSlotIndex = num6;
			ComponentGui.DisplaySmallMessage(block2.GetDisplayName(m_subsystemTerrain, value3), Color.White, blinking: false, playNotificationSound: false);
			m_subsystemAudio.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f, 0f);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			ComponentGui = base.Entity.FindComponent<ComponentGui>(throwOnError: true);
			ComponentInput = base.Entity.FindComponent<ComponentInput>(throwOnError: true);
			ComponentScreenOverlays = base.Entity.FindComponent<ComponentScreenOverlays>(throwOnError: true);
			ComponentBlockHighlight = base.Entity.FindComponent<ComponentBlockHighlight>(throwOnError: true);
			ComponentAimingSights = base.Entity.FindComponent<ComponentAimingSights>(throwOnError: true);
			ComponentMiner = base.Entity.FindComponent<ComponentMiner>(throwOnError: true);
			ComponentRider = base.Entity.FindComponent<ComponentRider>(throwOnError: true);
			ComponentSleep = base.Entity.FindComponent<ComponentSleep>(throwOnError: true);
			ComponentVitalStats = base.Entity.FindComponent<ComponentVitalStats>(throwOnError: true);
			ComponentSickness = base.Entity.FindComponent<ComponentSickness>(throwOnError: true);
			ComponentFlu = base.Entity.FindComponent<ComponentFlu>(throwOnError: true);
			ComponentLevel = base.Entity.FindComponent<ComponentLevel>(throwOnError: true);
			ComponentClothing = base.Entity.FindComponent<ComponentClothing>(throwOnError: true);
			ComponentOuterClothingModel = base.Entity.FindComponent<ComponentOuterClothingModel>(throwOnError: true);
			int playerIndex = valuesDictionary.GetValue<int>("PlayerIndex");
			PlayerData = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).PlayersData.First((PlayerData d) => d.PlayerIndex == playerIndex);
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			base.Save(valuesDictionary, entityToIdMap);
			valuesDictionary.SetValue("PlayerIndex", PlayerData.PlayerIndex);
		}
	}
}
