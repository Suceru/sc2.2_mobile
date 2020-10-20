using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentGui : Component, IUpdateable
	{
		public class ModalPanelAnimationData
		{
			public Widget NewWidget;

			public Widget OldWidget;

			public float Factor;
		}

		public class Message
		{
			public string LargeText;

			public string SmallText;

			public double StartTime;

			public float Duration;
		}

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemTimeOfDay m_subsystemTimeOfDay;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public ComponentPlayer m_componentPlayer;

		public ContainerWidget m_leftControlsContainerWidget;

		public ContainerWidget m_rightControlsContainerWidget;

		public ContainerWidget m_moveContainerWidget;

		public ContainerWidget m_lookContainerWidget;

		public RectangleWidget m_moveRectangleWidget;

		public RectangleWidget m_lookRectangleWidget;

		public ContainerWidget m_moveRectangleContainerWidget;

		public ContainerWidget m_lookRectangleContainerWidget;

		public ContainerWidget m_movePadContainerWidget;

		public ContainerWidget m_lookPadContainerWidget;

		public ContainerWidget m_moveButtonsContainerWidget;

		public ContainerWidget m_modalPanelContainerWidget;

		public ContainerWidget m_largeMessageWidget;

		public MessageWidget m_messageWidget;

		public ButtonWidget m_backButtonWidget;

		public ButtonWidget m_inventoryButtonWidget;

		public ButtonWidget m_clothingButtonWidget;

		public ButtonWidget m_moreButtonWidget;

		public Widget m_moreContentsWidget;

		public ButtonWidget m_lightningButtonWidget;

		public ButtonWidget m_photoButtonWidget;

		public ButtonWidget m_helpButtonWidget;

		public ButtonWidget m_timeOfDayButtonWidget;

		public ButtonWidget m_cameraButtonWidget;

		public ButtonWidget m_creativeFlyButtonWidget;

		public ButtonWidget m_sneakButtonWidget;

		public ButtonWidget m_mountButtonWidget;

		public ButtonWidget m_editItemButton;

		public float m_sidePanelsFactor;

		public ModalPanelAnimationData m_modalPanelAnimationData;

		public Message m_message;

		public KeyboardHelpDialog m_keyboardHelpDialog;

		public GamepadHelpDialog m_gamepadHelpDialog;

		public double m_lastMountableCreatureSearchTime;

		public bool m_keyboardHelpMessageShown;

		public bool m_gamepadHelpMessageShown;

		public ContainerWidget ControlsContainerWidget
		{
			get;
			set;
		}

		public TouchInputWidget ViewWidget
		{
			get;
			set;
		}

		public TouchInputWidget MoveWidget
		{
			get;
			set;
		}

		public MoveRoseWidget MoveRoseWidget
		{
			get;
			set;
		}

		public TouchInputWidget LookWidget
		{
			get;
			set;
		}

		public ShortInventoryWidget ShortInventoryWidget
		{
			get;
			set;
		}

		public ValueBarWidget HealthBarWidget
		{
			get;
			set;
		}

		public ValueBarWidget FoodBarWidget
		{
			get;
			set;
		}

		public ValueBarWidget TemperatureBarWidget
		{
			get;
			set;
		}

		public LabelWidget LevelLabelWidget
		{
			get;
			set;
		}

		public Widget ModalPanelWidget
		{
			get
			{
				if (m_modalPanelContainerWidget.Children.Count <= 0)
				{
					return null;
				}
				return m_modalPanelContainerWidget.Children[0];
			}
			set
			{
				if (value != ModalPanelWidget)
				{
					if (m_modalPanelAnimationData != null)
					{
						EndModalPanelAnimation();
					}
					m_modalPanelAnimationData = new ModalPanelAnimationData
					{
						OldWidget = ModalPanelWidget,
						NewWidget = value
					};
					if (value != null)
					{
						value.HorizontalAlignment = WidgetAlignment.Center;
						m_modalPanelContainerWidget.Children.Insert(0, value);
					}
					UpdateModalPanelAnimation();
					m_componentPlayer.GameWidget.Input.Clear();
					m_componentPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
				}
			}
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void DisplayLargeMessage(string largeText, string smallText, float duration, float delay)
		{
			m_message = new Message
			{
				LargeText = largeText,
				SmallText = smallText,
				Duration = duration,
				StartTime = Time.RealTime + (double)delay
			};
		}

		public void DisplaySmallMessage(string text, Color color, bool blinking, bool playNotificationSound)
		{
			m_messageWidget.DisplayMessage(text, color, blinking);
			if (playNotificationSound)
			{
				m_subsystemAudio.PlaySound("Audio/UI/Message", 1f, 0f, 0f, 0f);
			}
		}

		public bool IsGameMenuDialogVisible()
		{
			foreach (Dialog dialog in DialogsManager.Dialogs)
			{
				if (dialog.ParentWidget == m_componentPlayer.GuiWidget && dialog is GameMenuDialog)
				{
					return true;
				}
			}
			return false;
		}

		public void Update(float dt)
		{
			HandleInput();
			UpdateWidgets();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			ContainerWidget guiWidget = m_componentPlayer.GuiWidget;
			m_backButtonWidget = guiWidget.Children.Find<ButtonWidget>("BackButton");
			m_inventoryButtonWidget = guiWidget.Children.Find<ButtonWidget>("InventoryButton");
			m_clothingButtonWidget = guiWidget.Children.Find<ButtonWidget>("ClothingButton");
			m_moreButtonWidget = guiWidget.Children.Find<ButtonWidget>("MoreButton");
			m_moreContentsWidget = guiWidget.Children.Find<Widget>("MoreContents");
			m_helpButtonWidget = guiWidget.Children.Find<ButtonWidget>("HelpButton");
			m_photoButtonWidget = guiWidget.Children.Find<ButtonWidget>("PhotoButton");
			m_lightningButtonWidget = guiWidget.Children.Find<ButtonWidget>("LightningButton");
			m_timeOfDayButtonWidget = guiWidget.Children.Find<ButtonWidget>("TimeOfDayButton");
			m_cameraButtonWidget = guiWidget.Children.Find<ButtonWidget>("CameraButton");
			m_creativeFlyButtonWidget = guiWidget.Children.Find<ButtonWidget>("CreativeFlyButton");
			m_sneakButtonWidget = guiWidget.Children.Find<ButtonWidget>("SneakButton");
			m_mountButtonWidget = guiWidget.Children.Find<ButtonWidget>("MountButton");
			m_editItemButton = guiWidget.Children.Find<ButtonWidget>("EditItemButton");
			MoveWidget = guiWidget.Children.Find<TouchInputWidget>("Move");
			MoveRoseWidget = guiWidget.Children.Find<MoveRoseWidget>("MoveRose");
			LookWidget = guiWidget.Children.Find<TouchInputWidget>("Look");
			ViewWidget = m_componentPlayer.ViewWidget;
			HealthBarWidget = guiWidget.Children.Find<ValueBarWidget>("HealthBar");
			FoodBarWidget = guiWidget.Children.Find<ValueBarWidget>("FoodBar");
			TemperatureBarWidget = guiWidget.Children.Find<ValueBarWidget>("TemperatureBar");
			LevelLabelWidget = guiWidget.Children.Find<LabelWidget>("LevelLabel");
			m_modalPanelContainerWidget = guiWidget.Children.Find<ContainerWidget>("ModalPanelContainer");
			ControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("ControlsContainer");
			m_leftControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("LeftControlsContainer");
			m_rightControlsContainerWidget = guiWidget.Children.Find<ContainerWidget>("RightControlsContainer");
			m_moveContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveContainer");
			m_lookContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookContainer");
			m_moveRectangleWidget = guiWidget.Children.Find<RectangleWidget>("MoveRectangle");
			m_lookRectangleWidget = guiWidget.Children.Find<RectangleWidget>("LookRectangle");
			m_moveRectangleContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveRectangleContainer");
			m_lookRectangleContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookRectangleContainer");
			m_moveRectangleWidget = guiWidget.Children.Find<RectangleWidget>("MoveRectangle");
			m_lookRectangleWidget = guiWidget.Children.Find<RectangleWidget>("LookRectangle");
			m_movePadContainerWidget = guiWidget.Children.Find<ContainerWidget>("MovePadContainer");
			m_lookPadContainerWidget = guiWidget.Children.Find<ContainerWidget>("LookPadContainer");
			m_moveButtonsContainerWidget = guiWidget.Children.Find<ContainerWidget>("MoveButtonsContainer");
			ShortInventoryWidget = guiWidget.Children.Find<ShortInventoryWidget>("ShortInventory");
			m_largeMessageWidget = guiWidget.Children.Find<ContainerWidget>("LargeMessage");
			m_messageWidget = guiWidget.Children.Find<MessageWidget>("Message");
			m_keyboardHelpMessageShown = valuesDictionary.GetValue<bool>("KeyboardHelpMessageShown");
			m_gamepadHelpMessageShown = valuesDictionary.GetValue<bool>("GamepadHelpMessageShown");
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("KeyboardHelpMessageShown", m_keyboardHelpMessageShown);
			valuesDictionary.SetValue("GamepadHelpMessageShown", m_gamepadHelpMessageShown);
		}

		public override void OnEntityAdded()
		{
			ShortInventoryWidget.AssignComponents(m_componentPlayer.ComponentMiner.Inventory);
		}

		public override void OnEntityRemoved()
		{
			ShortInventoryWidget.AssignComponents(null);
			m_message = null;
		}

		public override void Dispose()
		{
			ModalPanelWidget = null;
			m_keyboardHelpDialog = null;
			if (ShortInventoryWidget != null)
			{
				ShortInventoryWidget.AssignComponents(null);
			}
		}

		public void UpdateSidePanelsAnimation()
		{
			float num = MathUtils.Min(Time.FrameDuration, 0.1f);
			bool flag = ModalPanelWidget != null && (m_modalPanelAnimationData == null || m_modalPanelAnimationData.NewWidget != null);
			float num2 = (!(m_componentPlayer.ComponentInput.IsControlledByTouch | flag)) ? 1 : 0;
			float x = num2 - m_sidePanelsFactor;
			if (MathUtils.Abs(x) > 0.01f)
			{
				m_sidePanelsFactor += MathUtils.Clamp(12f * MathUtils.PowSign(x, 0.75f) * num, 0f - MathUtils.Abs(x), MathUtils.Abs(x));
			}
			else
			{
				m_sidePanelsFactor = num2;
			}
			m_leftControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(m_leftControlsContainerWidget.ActualSize.X * (0f - m_sidePanelsFactor), 0f, 0f);
			m_rightControlsContainerWidget.RenderTransform = Matrix.CreateTranslation(m_rightControlsContainerWidget.ActualSize.X * m_sidePanelsFactor, 0f, 0f);
		}

		public void UpdateModalPanelAnimation()
		{
			m_modalPanelAnimationData.Factor += 6f * MathUtils.Min(Time.FrameDuration, 0.1f);
			if (m_modalPanelAnimationData.Factor < 1f)
			{
				float factor = m_modalPanelAnimationData.Factor;
				float num = 0.5f + 0.5f * MathUtils.Pow(1f - factor, 0.1f);
				float num2 = 0.5f + 0.5f * MathUtils.Pow(factor, 0.1f);
				float s = 1f - factor;
				float s2 = factor;
				if (m_modalPanelAnimationData.OldWidget != null)
				{
					Vector2 actualSize = m_modalPanelAnimationData.OldWidget.ActualSize;
					m_modalPanelAnimationData.OldWidget.ColorTransform = Color.White * s;
					m_modalPanelAnimationData.OldWidget.RenderTransform = Matrix.CreateTranslation((0f - actualSize.X) / 2f, (0f - actualSize.Y) / 2f, 0f) * Matrix.CreateScale(num, num, 1f) * Matrix.CreateTranslation(actualSize.X / 2f, actualSize.Y / 2f, 0f);
				}
				if (m_modalPanelAnimationData.NewWidget != null)
				{
					Vector2 actualSize2 = m_modalPanelAnimationData.NewWidget.ActualSize;
					m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White * s2;
					m_modalPanelAnimationData.NewWidget.RenderTransform = Matrix.CreateTranslation((0f - actualSize2.X) / 2f, (0f - actualSize2.Y) / 2f, 0f) * Matrix.CreateScale(num2, num2, 1f) * Matrix.CreateTranslation(actualSize2.X / 2f, actualSize2.Y / 2f, 0f);
				}
			}
			else
			{
				EndModalPanelAnimation();
			}
		}

		public void EndModalPanelAnimation()
		{
			if (m_modalPanelAnimationData.OldWidget != null)
			{
				m_modalPanelContainerWidget.Children.Remove(m_modalPanelAnimationData.OldWidget);
			}
			if (m_modalPanelAnimationData.NewWidget != null)
			{
				m_modalPanelAnimationData.NewWidget.ColorTransform = Color.White;
				m_modalPanelAnimationData.NewWidget.RenderTransform = Matrix.Identity;
			}
			m_modalPanelAnimationData = null;
		}

		public void UpdateWidgets()
		{
			ComponentRider componentRider = m_componentPlayer.ComponentRider;
			ComponentSleep componentSleep = m_componentPlayer.ComponentSleep;
			ComponentInput componentInput = m_componentPlayer.ComponentInput;
			WorldSettings worldSettings = m_subsystemGameInfo.WorldSettings;
			GameMode gameMode = worldSettings.GameMode;
			UpdateSidePanelsAnimation();
			if (m_modalPanelAnimationData != null)
			{
				UpdateModalPanelAnimation();
			}
			if (m_message != null)
			{
				double realTime = Time.RealTime;
				m_largeMessageWidget.IsVisible = true;
				LabelWidget labelWidget = m_largeMessageWidget.Children.Find<LabelWidget>("LargeLabel");
				LabelWidget labelWidget2 = m_largeMessageWidget.Children.Find<LabelWidget>("SmallLabel");
				labelWidget.Text = m_message.LargeText;
				labelWidget2.Text = m_message.SmallText;
				labelWidget.IsVisible = !string.IsNullOrEmpty(m_message.LargeText);
				labelWidget2.IsVisible = !string.IsNullOrEmpty(m_message.SmallText);
				float num = (float)MathUtils.Min(MathUtils.Saturate(2.0 * (realTime - m_message.StartTime)), MathUtils.Saturate(2.0 * (m_message.StartTime + (double)m_message.Duration - realTime)));
				labelWidget.Color = new Color(num, num, num, num);
				labelWidget2.Color = new Color(num, num, num, num);
				if (Time.RealTime > m_message.StartTime + (double)m_message.Duration)
				{
					m_message = null;
				}
			}
			else
			{
				m_largeMessageWidget.IsVisible = false;
			}
			ControlsContainerWidget.IsVisible = (m_componentPlayer.PlayerData.IsReadyForPlaying && m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled && componentSleep.SleepFactor <= 0f);
			m_moveRectangleContainerWidget.IsVisible = (!SettingsManager.HideMoveLookPads && componentInput.IsControlledByTouch);
			m_lookRectangleContainerWidget.IsVisible = (!SettingsManager.HideMoveLookPads && componentInput.IsControlledByTouch && (SettingsManager.LookControlMode != LookControlMode.EntireScreen || SettingsManager.MoveControlMode != MoveControlMode.Buttons));
			m_lookPadContainerWidget.IsVisible = (SettingsManager.LookControlMode != LookControlMode.SplitTouch);
			MoveRoseWidget.IsVisible = componentInput.IsControlledByTouch;
			m_moreContentsWidget.IsVisible = m_moreButtonWidget.IsChecked;
			HealthBarWidget.IsVisible = (gameMode != GameMode.Creative);
			FoodBarWidget.IsVisible = (gameMode != 0 && worldSettings.AreAdventureSurvivalMechanicsEnabled);
			TemperatureBarWidget.IsVisible = (gameMode != 0 && worldSettings.AreAdventureSurvivalMechanicsEnabled);
			LevelLabelWidget.IsVisible = (gameMode != 0 && worldSettings.AreAdventureSurvivalMechanicsEnabled);
			m_creativeFlyButtonWidget.IsVisible = (gameMode == GameMode.Creative);
			m_timeOfDayButtonWidget.IsVisible = (gameMode == GameMode.Creative);
			m_lightningButtonWidget.IsVisible = (gameMode == GameMode.Creative);
			m_moveButtonsContainerWidget.IsVisible = (SettingsManager.MoveControlMode == MoveControlMode.Buttons);
			m_movePadContainerWidget.IsVisible = (SettingsManager.MoveControlMode == MoveControlMode.Pad);
			if (SettingsManager.LeftHandedLayout)
			{
				m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
				m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
				m_moveRectangleWidget.FlipHorizontal = true;
				m_lookRectangleWidget.FlipHorizontal = false;
			}
			else
			{
				m_moveContainerWidget.HorizontalAlignment = WidgetAlignment.Near;
				m_lookContainerWidget.HorizontalAlignment = WidgetAlignment.Far;
				m_moveRectangleWidget.FlipHorizontal = false;
				m_lookRectangleWidget.FlipHorizontal = true;
			}
			m_sneakButtonWidget.IsChecked = m_componentPlayer.ComponentBody.IsSneaking;
			m_creativeFlyButtonWidget.IsChecked = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
			m_inventoryButtonWidget.IsChecked = IsInventoryVisible();
			m_clothingButtonWidget.IsChecked = IsClothingVisible();
			if (IsActiveSlotEditable() || m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue)
			{
				m_sneakButtonWidget.IsVisible = false;
				m_mountButtonWidget.IsVisible = false;
				m_editItemButton.IsVisible = true;
			}
			else if (componentRider != null && componentRider.Mount != null)
			{
				m_sneakButtonWidget.IsVisible = false;
				m_mountButtonWidget.IsChecked = true;
				m_mountButtonWidget.IsVisible = true;
				m_editItemButton.IsVisible = false;
			}
			else
			{
				m_mountButtonWidget.IsChecked = false;
				if (componentRider != null && Time.FrameStartTime - m_lastMountableCreatureSearchTime > 0.5)
				{
					m_lastMountableCreatureSearchTime = Time.FrameStartTime;
					if (componentRider.FindNearestMount() != null)
					{
						m_sneakButtonWidget.IsVisible = false;
						m_mountButtonWidget.IsVisible = true;
						m_editItemButton.IsVisible = false;
					}
					else
					{
						m_sneakButtonWidget.IsVisible = true;
						m_mountButtonWidget.IsVisible = false;
						m_editItemButton.IsVisible = false;
					}
				}
			}
			if (!m_componentPlayer.IsAddedToProject || m_componentPlayer.ComponentHealth.Health == 0f || componentSleep.IsSleeping || m_componentPlayer.ComponentSickness.IsPuking)
			{
				ModalPanelWidget = null;
			}
			if (m_componentPlayer.ComponentSickness.IsSick)
			{
				m_componentPlayer.ComponentGui.HealthBarWidget.LitBarColor = new Color(166, 175, 103);
			}
			else if (m_componentPlayer.ComponentFlu.HasFlu)
			{
				m_componentPlayer.ComponentGui.HealthBarWidget.LitBarColor = new Color(0, 48, 255);
			}
			else
			{
				m_componentPlayer.ComponentGui.HealthBarWidget.LitBarColor = new Color(224, 24, 0);
			}
		}

		public void HandleInput()
		{
			WidgetInput input = m_componentPlayer.GameWidget.Input;
			PlayerInput playerInput = m_componentPlayer.ComponentInput.PlayerInput;
			ComponentRider componentRider = m_componentPlayer.ComponentRider;
			if (m_componentPlayer.GameWidget.ActiveCamera.IsEntityControlEnabled)
			{
				if (!m_keyboardHelpMessageShown && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Keyboard) != 0 && Time.PeriodicEvent(7.0, 0.0))
				{
					m_keyboardHelpMessageShown = true;
					DisplaySmallMessage("Press H for keyboard controls\n(or see HELP)", Color.White, blinking: true, playNotificationSound: true);
				}
				else if (!m_gamepadHelpMessageShown && (m_componentPlayer.PlayerData.InputDevice & WidgetInputDevice.Gamepads) != 0 && Time.PeriodicEvent(7.0, 0.0))
				{
					m_gamepadHelpMessageShown = true;
					DisplaySmallMessage("Press START/PAUSE for gamepad controls\n(or see HELP)", Color.White, blinking: true, playNotificationSound: true);
				}
			}
			if (playerInput.KeyboardHelp)
			{
				if (m_keyboardHelpDialog == null)
				{
					m_keyboardHelpDialog = new KeyboardHelpDialog();
				}
				if (m_keyboardHelpDialog.ParentWidget != null)
				{
					DialogsManager.HideDialog(m_keyboardHelpDialog);
				}
				else
				{
					DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, m_keyboardHelpDialog);
				}
			}
			if (playerInput.GamepadHelp)
			{
				if (m_gamepadHelpDialog == null)
				{
					m_gamepadHelpDialog = new GamepadHelpDialog();
				}
				if (m_gamepadHelpDialog.ParentWidget != null)
				{
					DialogsManager.HideDialog(m_gamepadHelpDialog);
				}
				else
				{
					DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, m_gamepadHelpDialog);
				}
			}
			if (m_helpButtonWidget.IsClicked)
			{
				ScreensManager.SwitchScreen("Help");
			}
			if (playerInput.ToggleInventory || m_inventoryButtonWidget.IsClicked)
			{
				if (IsInventoryVisible())
				{
					ModalPanelWidget = null;
				}
				else if (m_componentPlayer.ComponentMiner.Inventory is ComponentCreativeInventory)
				{
					ModalPanelWidget = new CreativeInventoryWidget(m_componentPlayer.Entity);
				}
				else
				{
					ModalPanelWidget = new FullInventoryWidget(m_componentPlayer.ComponentMiner.Inventory, m_componentPlayer.Entity.FindComponent<ComponentCraftingTable>(throwOnError: true));
				}
			}
			if (playerInput.ToggleClothing || m_clothingButtonWidget.IsClicked)
			{
				if (IsClothingVisible())
				{
					ModalPanelWidget = null;
				}
				else
				{
					ModalPanelWidget = new ClothingWidget(m_componentPlayer);
				}
			}
			if (m_sneakButtonWidget.IsClicked || playerInput.ToggleSneak)
			{
				bool isSneaking = m_componentPlayer.ComponentBody.IsSneaking;
				m_componentPlayer.ComponentBody.IsSneaking = !isSneaking;
				if (m_componentPlayer.ComponentBody.IsSneaking != isSneaking)
				{
					if (m_componentPlayer.ComponentBody.IsSneaking)
					{
						DisplaySmallMessage("Sneak mode on", Color.White, blinking: false, playNotificationSound: false);
					}
					else
					{
						DisplaySmallMessage("Sneak mode off", Color.White, blinking: false, playNotificationSound: false);
					}
				}
			}
			if (componentRider != null && (m_mountButtonWidget.IsClicked || playerInput.ToggleMount))
			{
				bool flag = componentRider.Mount != null;
				if (flag)
				{
					componentRider.StartDismounting();
				}
				else
				{
					ComponentMount componentMount = componentRider.FindNearestMount();
					if (componentMount != null)
					{
						componentRider.StartMounting(componentMount);
					}
				}
				if (componentRider.Mount != null != flag)
				{
					if (componentRider.Mount != null)
					{
						DisplaySmallMessage("Mounted", Color.White, blinking: false, playNotificationSound: false);
					}
					else
					{
						DisplaySmallMessage("Dismounted", Color.White, blinking: false, playNotificationSound: false);
					}
				}
			}
			if ((m_editItemButton.IsClicked || playerInput.EditItem) && m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.HasValue)
			{
				Point3 value = m_componentPlayer.ComponentBlockHighlight.NearbyEditableCell.Value;
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(value.X, value.Y, value.Z);
				int contents = Terrain.ExtractContents(cellValue);
				SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(contents);
				for (int i = 0; i < blockBehaviors.Length && !blockBehaviors[i].OnEditBlock(value.X, value.Y, value.Z, cellValue, m_componentPlayer); i++)
				{
				}
			}
			else if ((m_editItemButton.IsClicked || playerInput.EditItem) && IsActiveSlotEditable())
			{
				IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
				if (inventory != null)
				{
					int activeSlotIndex = inventory.ActiveSlotIndex;
					int num = Terrain.ExtractContents(inventory.GetSlotValue(activeSlotIndex));
					if (BlocksManager.Blocks[num].IsEditable)
					{
						SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num);
						for (int i = 0; i < blockBehaviors.Length && !blockBehaviors[i].OnEditInventoryItem(inventory, activeSlotIndex, m_componentPlayer); i++)
						{
						}
					}
				}
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && (m_creativeFlyButtonWidget.IsClicked || playerInput.ToggleCreativeFly) && componentRider.Mount == null)
			{
				bool isCreativeFlyEnabled = m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled;
				m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled = !isCreativeFlyEnabled;
				if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled != isCreativeFlyEnabled)
				{
					if (m_componentPlayer.ComponentLocomotion.IsCreativeFlyEnabled)
					{
						m_componentPlayer.ComponentLocomotion.JumpOrder = 1f;
						DisplaySmallMessage("Fly mode on", Color.White, blinking: false, playNotificationSound: false);
					}
					else
					{
						DisplaySmallMessage("Fly mode off", Color.White, blinking: false, playNotificationSound: false);
					}
				}
			}
			if (!m_componentPlayer.ComponentInput.IsControlledByVr && (m_cameraButtonWidget.IsClicked || playerInput.SwitchCameraMode))
			{
				GameWidget gameWidget = m_componentPlayer.GameWidget;
				if (gameWidget.ActiveCamera.GetType() == typeof(FppCamera))
				{
					gameWidget.ActiveCamera = gameWidget.FindCamera<TppCamera>();
					DisplaySmallMessage("Third person camera", Color.White, blinking: false, playNotificationSound: false);
				}
				else if (gameWidget.ActiveCamera.GetType() == typeof(TppCamera))
				{
					gameWidget.ActiveCamera = gameWidget.FindCamera<OrbitCamera>();
					DisplaySmallMessage("Orbit camera", Color.White, blinking: false, playNotificationSound: false);
				}
				else if (gameWidget.ActiveCamera.GetType() == typeof(OrbitCamera))
				{
					gameWidget.ActiveCamera = gameWidget.FindCamera<FixedCamera>();
					DisplaySmallMessage("Fixed camera", Color.White, blinking: false, playNotificationSound: false);
				}
				else
				{
					gameWidget.ActiveCamera = gameWidget.FindCamera<FppCamera>();
					DisplaySmallMessage("First person camera", Color.White, blinking: false, playNotificationSound: false);
				}
			}
			if (m_photoButtonWidget.IsClicked || playerInput.TakeScreenshot)
			{
				ScreenCaptureManager.CapturePhoto(delegate
				{
					DisplaySmallMessage("Photo saved in pictures library", Color.White, blinking: false, playNotificationSound: false);
				}, delegate
				{
					DisplaySmallMessage("Error capturing photo, check game log", Color.White, blinking: false, playNotificationSound: false);
				});
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && (m_lightningButtonWidget.IsClicked || playerInput.Lighting))
			{
				Matrix matrix = Matrix.CreateFromQuaternion(m_componentPlayer.ComponentCreatureModel.EyeRotation);
				base.Project.FindSubsystem<SubsystemWeather>(throwOnError: true).ManualLightingStrike(m_componentPlayer.ComponentCreatureModel.EyePosition, matrix.Forward);
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative && (m_timeOfDayButtonWidget.IsClicked || playerInput.TimeOfDay))
			{
				float num2 = MathUtils.Remainder(0.25f, 1f);
				float num3 = MathUtils.Remainder(0.5f, 1f);
				float num4 = MathUtils.Remainder(0.75f, 1f);
				float num5 = MathUtils.Remainder(1f, 1f);
				float num6 = MathUtils.Remainder(num2 - m_subsystemTimeOfDay.TimeOfDay, 1f);
				float num7 = MathUtils.Remainder(num3 - m_subsystemTimeOfDay.TimeOfDay, 1f);
				float num8 = MathUtils.Remainder(num4 - m_subsystemTimeOfDay.TimeOfDay, 1f);
				float num9 = MathUtils.Remainder(num5 - m_subsystemTimeOfDay.TimeOfDay, 1f);
				float num10 = MathUtils.Min(num6, num7, num8, num9);
				if (num6 == num10)
				{
					m_subsystemTimeOfDay.TimeOfDayOffset += num6;
					DisplaySmallMessage("Dawn", Color.White, blinking: false, playNotificationSound: false);
				}
				else if (num7 == num10)
				{
					m_subsystemTimeOfDay.TimeOfDayOffset += num7;
					DisplaySmallMessage("Noon", Color.White, blinking: false, playNotificationSound: false);
				}
				else if (num8 == num10)
				{
					m_subsystemTimeOfDay.TimeOfDayOffset += num8;
					DisplaySmallMessage("Dusk", Color.White, blinking: false, playNotificationSound: false);
				}
				else if (num9 == num10)
				{
					m_subsystemTimeOfDay.TimeOfDayOffset += num9;
					DisplaySmallMessage("Midnight", Color.White, blinking: false, playNotificationSound: false);
				}
			}
			if (ModalPanelWidget != null)
			{
				if (input.Cancel || input.Back || m_backButtonWidget.IsClicked)
				{
					ModalPanelWidget = null;
				}
			}
			else if (input.Back || m_backButtonWidget.IsClicked)
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new GameMenuDialog(m_componentPlayer));
			}
		}

		public bool IsClothingVisible()
		{
			return ModalPanelWidget is ClothingWidget;
		}

		public bool IsInventoryVisible()
		{
			if (ModalPanelWidget != null)
			{
				return !IsClothingVisible();
			}
			return false;
		}

		public bool IsActiveSlotEditable()
		{
			IInventory inventory = m_componentPlayer.ComponentMiner.Inventory;
			if (inventory != null)
			{
				int activeSlotIndex = inventory.ActiveSlotIndex;
				int num = Terrain.ExtractContents(inventory.GetSlotValue(activeSlotIndex));
				if (BlocksManager.Blocks[num].IsEditable)
				{
					return true;
				}
			}
			return false;
		}
	}
}
