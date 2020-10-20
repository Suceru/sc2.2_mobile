using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class InventorySlotWidget : CanvasWidget, IDragTargetWidget
	{
		public BevelledRectangleWidget m_rectangleWidget;

		public RectangleWidget m_highlightWidget;

		public BlockIconWidget m_blockIconWidget;

		public LabelWidget m_countWidget;

		public ValueBarWidget m_healthBarWidget;

		public RectangleWidget m_editOverlayWidget;

		public RectangleWidget m_interactiveOverlayWidget;

		public RectangleWidget m_foodOverlayWidget;

		public LabelWidget m_splitLabelWidget;

		public GameWidget m_gameWidget;

		public DragHostWidget m_dragHostWidget;

		public IInventory m_inventory;

		public int m_slotIndex;

		public DragMode? m_dragMode;

		public bool m_focus;

		public int m_lastCount = -1;

		public InventoryDragData m_inventoryDragData;

		public SubsystemTerrain m_subsystemTerrain;

		public ComponentPlayer m_componentPlayer;

		public bool HideBlockIcon
		{
			get;
			set;
		}

		public bool HideEditOverlay
		{
			get;
			set;
		}

		public bool HideInteractiveOverlay
		{
			get;
			set;
		}

		public bool HideFoodOverlay
		{
			get;
			set;
		}

		public bool HideHighlightRectangle
		{
			get;
			set;
		}

		public bool HideHealthBar
		{
			get;
			set;
		}

		public bool ProcessingOnly
		{
			get;
			set;
		}

		public Color CenterColor
		{
			get
			{
				return m_rectangleWidget.CenterColor;
			}
			set
			{
				m_rectangleWidget.CenterColor = value;
			}
		}

		public Color BevelColor
		{
			get
			{
				return m_rectangleWidget.BevelColor;
			}
			set
			{
				m_rectangleWidget.BevelColor = value;
			}
		}

		public Matrix? CustomViewMatrix
		{
			get
			{
				return m_blockIconWidget.CustomViewMatrix;
			}
			set
			{
				m_blockIconWidget.CustomViewMatrix = value;
			}
		}

		public GameWidget GameWidget
		{
			get
			{
				if (m_gameWidget == null)
				{
					for (ContainerWidget parentWidget = base.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
					{
						GameWidget gameWidget = parentWidget as GameWidget;
						if (gameWidget != null)
						{
							m_gameWidget = gameWidget;
							break;
						}
					}
				}
				return m_gameWidget;
			}
		}

		public DragHostWidget DragHostWidget
		{
			get
			{
				if (m_dragHostWidget == null)
				{
					m_dragHostWidget = ((GameWidget != null) ? GameWidget.Children.Find<DragHostWidget>(throwIfNotFound: false) : null);
				}
				return m_dragHostWidget;
			}
		}

		public InventorySlotWidget()
		{
			base.Size = new Vector2(72f, 72f);
			WidgetsList children = Children;
			Widget[] array = new Widget[7];
			BevelledRectangleWidget obj = new BevelledRectangleWidget
			{
				BevelSize = -2f,
				DirectionalLight = 0.15f,
				CenterColor = Color.Transparent
			};
			BevelledRectangleWidget bevelledRectangleWidget = obj;
			m_rectangleWidget = obj;
			array[0] = bevelledRectangleWidget;
			RectangleWidget obj2 = new RectangleWidget
			{
				FillColor = Color.Transparent,
				OutlineColor = Color.Transparent
			};
			RectangleWidget rectangleWidget = obj2;
			m_highlightWidget = obj2;
			array[1] = rectangleWidget;
			BlockIconWidget obj3 = new BlockIconWidget
			{
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center,
				Margin = new Vector2(2f, 2f)
			};
			BlockIconWidget blockIconWidget = obj3;
			m_blockIconWidget = obj3;
			array[2] = blockIconWidget;
			LabelWidget obj4 = new LabelWidget
			{
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
				FontScale = 1f,
				HorizontalAlignment = WidgetAlignment.Far,
				VerticalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(6f, 2f)
			};
			LabelWidget labelWidget = obj4;
			m_countWidget = obj4;
			array[3] = labelWidget;
			ValueBarWidget obj5 = new ValueBarWidget
			{
				LayoutDirection = LayoutDirection.Vertical,
				HorizontalAlignment = WidgetAlignment.Near,
				VerticalAlignment = WidgetAlignment.Far,
				BarsCount = 3,
				FlipDirection = true,
				LitBarColor = new Color(32, 128, 0),
				UnlitBarColor = new Color(24, 24, 24, 64),
				BarSize = new Vector2(12f, 12f),
				BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/ProgressBar"),
				Margin = new Vector2(4f, 4f)
			};
			ValueBarWidget valueBarWidget = obj5;
			m_healthBarWidget = obj5;
			array[4] = valueBarWidget;
			StackPanelWidget obj6 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(3f, 3f)
			};
			WidgetsList children2 = obj6.Children;
			RectangleWidget obj7 = new RectangleWidget
			{
				Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/InteractiveItemOverlay"),
				Size = new Vector2(13f, 14f),
				FillColor = new Color(160, 160, 160),
				OutlineColor = Color.Transparent
			};
			rectangleWidget = obj7;
			m_interactiveOverlayWidget = obj7;
			children2.Add(rectangleWidget);
			WidgetsList children3 = obj6.Children;
			RectangleWidget obj8 = new RectangleWidget
			{
				Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/EditItemOverlay"),
				Size = new Vector2(12f, 14f),
				FillColor = new Color(160, 160, 160),
				OutlineColor = Color.Transparent
			};
			rectangleWidget = obj8;
			m_editOverlayWidget = obj8;
			children3.Add(rectangleWidget);
			WidgetsList children4 = obj6.Children;
			RectangleWidget obj9 = new RectangleWidget
			{
				Subtexture = ContentManager.Get<Subtexture>("Textures/Atlas/FoodItemOverlay"),
				Size = new Vector2(11f, 14f),
				FillColor = new Color(160, 160, 160),
				OutlineColor = Color.Transparent
			};
			rectangleWidget = obj9;
			m_foodOverlayWidget = obj9;
			children4.Add(rectangleWidget);
			array[5] = obj6;
			LabelWidget obj10 = new LabelWidget
			{
				Text = "Split",
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles"),
				Color = new Color(255, 64, 0),
				HorizontalAlignment = WidgetAlignment.Near,
				VerticalAlignment = WidgetAlignment.Near,
				Margin = new Vector2(2f, 0f)
			};
			labelWidget = obj10;
			m_splitLabelWidget = obj10;
			array[6] = labelWidget;
			children.Add(array);
		}

		public void AssignInventorySlot(IInventory inventory, int slotIndex)
		{
			m_inventory = inventory;
			m_slotIndex = slotIndex;
			m_subsystemTerrain = inventory?.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			if (inventory is Component)
			{
				m_componentPlayer = ((Component)inventory).Entity.FindComponent<ComponentPlayer>();
			}
			else
			{
				m_componentPlayer = null;
			}
			m_blockIconWidget.DrawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
		}

		public override void Update()
		{
			if (m_inventory == null || DragHostWidget == null)
			{
				return;
			}
			WidgetInput input = base.Input;
			ComponentPlayer viewPlayer = GetViewPlayer();
			int slotValue = m_inventory.GetSlotValue(m_slotIndex);
			int num = Terrain.ExtractContents(slotValue);
			Block block = BlocksManager.Blocks[num];
			if (m_componentPlayer != null)
			{
				m_blockIconWidget.DrawBlockEnvironmentData.InWorldMatrix = m_componentPlayer.ComponentBody.Matrix;
			}
			if (m_focus && !input.Press.HasValue)
			{
				m_focus = false;
			}
			else if (input.Tap.HasValue && HitTestGlobal(input.Tap.Value) == this)
			{
				m_focus = true;
			}
			if (input.SpecialClick.HasValue && HitTestGlobal(input.SpecialClick.Value.Start) == this && HitTestGlobal(input.SpecialClick.Value.End) == this)
			{
				IInventory inventory = null;
				foreach (InventorySlotWidget item in ((ContainerWidget)base.RootWidget).AllChildren.OfType<InventorySlotWidget>())
				{
					if (item.m_inventory != null && item.m_inventory != m_inventory && item.Input == base.Input && item.IsEnabledGlobal && item.IsVisibleGlobal)
					{
						inventory = item.m_inventory;
						break;
					}
				}
				if (inventory != null)
				{
					int num2 = ComponentInventoryBase.FindAcquireSlotForItem(inventory, slotValue);
					if (num2 >= 0)
					{
						HandleMoveItem(m_inventory, m_slotIndex, inventory, num2, m_inventory.GetSlotCount(m_slotIndex));
					}
				}
			}
			if (input.Click.HasValue && HitTestGlobal(input.Click.Value.Start) == this && HitTestGlobal(input.Click.Value.End) == this)
			{
				bool flag = false;
				if (viewPlayer != null)
				{
					if (viewPlayer.ComponentInput.SplitSourceInventory == m_inventory && viewPlayer.ComponentInput.SplitSourceSlotIndex == m_slotIndex)
					{
						viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(null, -1);
						flag = true;
					}
					else if (viewPlayer.ComponentInput.SplitSourceInventory != null)
					{
						flag = HandleMoveItem(viewPlayer.ComponentInput.SplitSourceInventory, viewPlayer.ComponentInput.SplitSourceSlotIndex, m_inventory, m_slotIndex, 1);
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
					}
				}
				if (!flag && m_inventory.ActiveSlotIndex != m_slotIndex && m_slotIndex < 10)
				{
					m_inventory.ActiveSlotIndex = m_slotIndex;
					if (m_inventory.ActiveSlotIndex == m_slotIndex)
					{
						AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
					}
				}
			}
			if (!m_focus || ProcessingOnly || viewPlayer == null)
			{
				return;
			}
			Vector2? hold = input.Hold;
			if (hold.HasValue && HitTestGlobal(hold.Value) == this && !DragHostWidget.IsDragInProgress && m_inventory.GetSlotCount(m_slotIndex) > 0 && (viewPlayer.ComponentInput.SplitSourceInventory != m_inventory || viewPlayer.ComponentInput.SplitSourceSlotIndex != m_slotIndex))
			{
				input.Clear();
				viewPlayer.ComponentInput.SetSplitSourceInventoryAndSlot(m_inventory, m_slotIndex);
				AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
			}
			Vector2? drag = input.Drag;
			if (!drag.HasValue || HitTestGlobal(drag.Value) != this || DragHostWidget.IsDragInProgress)
			{
				return;
			}
			int slotCount = m_inventory.GetSlotCount(m_slotIndex);
			if (slotCount > 0)
			{
				DragMode dragMode = input.DragMode;
				if (viewPlayer.ComponentInput.SplitSourceInventory == m_inventory && viewPlayer.ComponentInput.SplitSourceSlotIndex == m_slotIndex)
				{
					dragMode = DragMode.SingleItem;
				}
				int num3 = (dragMode != 0) ? 1 : slotCount;
				SubsystemTerrain subsystemTerrain = m_inventory.Project.FindSubsystem<SubsystemTerrain>();
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(null, ContentManager.Get<XElement>("Widgets/InventoryDragWidget"), null);
				containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").Value = Terrain.ReplaceLight(slotValue, 15);
				containerWidget.Children.Find<BlockIconWidget>("InventoryDragWidget.Icon").DrawBlockEnvironmentData.SubsystemTerrain = subsystemTerrain;
				containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Name").Text = block.GetDisplayName(subsystemTerrain, slotValue);
				containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").Text = num3.ToString();
				containerWidget.Children.Find<LabelWidget>("InventoryDragWidget.Count").IsVisible = (!(m_inventory is ComponentCreativeInventory) && !(m_inventory is ComponentFurnitureInventory));
				DragHostWidget.BeginDrag(containerWidget, new InventoryDragData
				{
					Inventory = m_inventory,
					SlotIndex = m_slotIndex,
					DragMode = dragMode
				}, delegate
				{
					m_dragMode = null;
				});
				m_dragMode = dragMode;
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			if (m_inventory != null)
			{
				bool flag = m_inventory is ComponentCreativeInventory || m_inventory is ComponentFurnitureInventory;
				int num = m_inventory.GetSlotCount(m_slotIndex);
				if (!flag && m_dragMode.HasValue)
				{
					num = ((m_dragMode.Value != 0) ? MathUtils.Max(num - 1, 0) : 0);
				}
				m_rectangleWidget.IsVisible = true;
				if (num > 0)
				{
					int slotValue = m_inventory.GetSlotValue(m_slotIndex);
					int num2 = Terrain.ExtractContents(slotValue);
					Block block = BlocksManager.Blocks[num2];
					bool flag2 = block.GetRotPeriod(slotValue) > 0 && block.GetDamage(slotValue) > 0;
					m_blockIconWidget.Value = Terrain.ReplaceLight(slotValue, 15);
					m_blockIconWidget.IsVisible = !HideBlockIcon;
					if (num != m_lastCount)
					{
						m_countWidget.Text = num.ToString();
						m_lastCount = num;
					}
					m_countWidget.IsVisible = (num > 1 && !flag);
					m_editOverlayWidget.IsVisible = (!HideEditOverlay && block.IsEditable);
					m_interactiveOverlayWidget.IsVisible = (!HideInteractiveOverlay && ((m_subsystemTerrain != null) ? block.IsInteractive(m_subsystemTerrain, slotValue) : block.DefaultIsInteractive));
					m_foodOverlayWidget.IsVisible = (!HideFoodOverlay && block.GetRotPeriod(slotValue) > 0);
					m_foodOverlayWidget.FillColor = (flag2 ? new Color(128, 64, 0) : new Color(160, 160, 160));
					if (!flag && !HideHealthBar && block.Durability >= 0)
					{
						int damage = block.GetDamage(slotValue);
						m_healthBarWidget.IsVisible = true;
						m_healthBarWidget.Value = (float)(block.Durability - damage) / (float)block.Durability;
					}
					else
					{
						m_healthBarWidget.IsVisible = false;
					}
				}
				else
				{
					m_blockIconWidget.IsVisible = false;
					m_countWidget.IsVisible = false;
					m_healthBarWidget.IsVisible = false;
					m_editOverlayWidget.IsVisible = false;
					m_interactiveOverlayWidget.IsVisible = false;
					m_foodOverlayWidget.IsVisible = false;
				}
				m_highlightWidget.IsVisible = !HideHighlightRectangle;
				m_highlightWidget.OutlineColor = Color.Transparent;
				m_highlightWidget.FillColor = Color.Transparent;
				m_splitLabelWidget.IsVisible = false;
				if (m_slotIndex == m_inventory.ActiveSlotIndex)
				{
					m_highlightWidget.OutlineColor = new Color(0, 0, 0);
					m_highlightWidget.FillColor = new Color(0, 0, 0, 80);
				}
				if (IsSplitMode())
				{
					m_highlightWidget.OutlineColor = new Color(255, 64, 0);
					m_splitLabelWidget.IsVisible = true;
				}
			}
			else
			{
				m_rectangleWidget.IsVisible = false;
				m_highlightWidget.IsVisible = false;
				m_blockIconWidget.IsVisible = false;
				m_countWidget.IsVisible = false;
				m_healthBarWidget.IsVisible = false;
				m_editOverlayWidget.IsVisible = false;
				m_interactiveOverlayWidget.IsVisible = false;
				m_foodOverlayWidget.IsVisible = false;
				m_splitLabelWidget.IsVisible = false;
			}
			base.IsDrawRequired = (m_inventoryDragData != null);
			base.MeasureOverride(parentAvailableSize);
		}

		public override void Draw(DrawContext dc)
		{
			if (m_inventory != null && m_inventoryDragData != null)
			{
				int slotValue = m_inventoryDragData.Inventory.GetSlotValue(m_inventoryDragData.SlotIndex);
				if (m_inventory.GetSlotProcessCapacity(m_slotIndex, slotValue) >= 0 || m_inventory.GetSlotCapacity(m_slotIndex, slotValue) > 0)
				{
					float num = 80f * base.GlobalTransform.Right.Length();
					Vector2 center = Vector2.Transform(base.ActualSize / 2f, base.GlobalTransform);
					FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(100);
					flatBatch2D.QueueEllipse(center, new Vector2(num), 0f, new Color(0, 0, 0, 96) * base.GlobalColorTransform, 64);
					flatBatch2D.QueueEllipse(center, new Vector2(num - 0.5f), 0f, new Color(0, 0, 0, 64) * base.GlobalColorTransform, 64);
					flatBatch2D.QueueEllipse(center, new Vector2(num + 0.5f), 0f, new Color(0, 0, 0, 48) * base.GlobalColorTransform, 64);
					flatBatch2D.QueueDisc(center, new Vector2(num), 0f, new Color(0, 0, 0, 48) * base.GlobalColorTransform, 64);
				}
			}
			m_inventoryDragData = null;
		}

		public void DragOver(Widget dragWidget, object data)
		{
			m_inventoryDragData = (data as InventoryDragData);
		}

		public void DragDrop(Widget dragWidget, object data)
		{
			InventoryDragData inventoryDragData = data as InventoryDragData;
			if (m_inventory != null && inventoryDragData != null)
			{
				HandleDragDrop(inventoryDragData.Inventory, inventoryDragData.SlotIndex, inventoryDragData.DragMode, m_inventory, m_slotIndex);
			}
		}

		public ComponentPlayer GetViewPlayer()
		{
			if (GameWidget == null)
			{
				return null;
			}
			return GameWidget.PlayerData.ComponentPlayer;
		}

		public bool IsSplitMode()
		{
			ComponentPlayer viewPlayer = GetViewPlayer();
			if (viewPlayer != null)
			{
				if (m_inventory != null && m_inventory == viewPlayer.ComponentInput.SplitSourceInventory)
				{
					return m_slotIndex == viewPlayer.ComponentInput.SplitSourceSlotIndex;
				}
				return false;
			}
			return false;
		}

		public bool HandleMoveItem(IInventory sourceInventory, int sourceSlotIndex, IInventory targetInventory, int targetSlotIndex, int count)
		{
			int slotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
			int slotValue2 = targetInventory.GetSlotValue(targetSlotIndex);
			int slotCount = sourceInventory.GetSlotCount(sourceSlotIndex);
			int slotCount2 = targetInventory.GetSlotCount(targetSlotIndex);
			if (slotCount2 == 0 || slotValue == slotValue2)
			{
				int num = MathUtils.Min(targetInventory.GetSlotCapacity(targetSlotIndex, slotValue) - slotCount2, slotCount, count);
				if (num > 0)
				{
					int count2 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num);
					targetInventory.AddSlotItems(targetSlotIndex, slotValue, count2);
					return true;
				}
			}
			return false;
		}

		public bool HandleDragDrop(IInventory sourceInventory, int sourceSlotIndex, DragMode dragMode, IInventory targetInventory, int targetSlotIndex)
		{
			int slotValue = sourceInventory.GetSlotValue(sourceSlotIndex);
			int slotValue2 = targetInventory.GetSlotValue(targetSlotIndex);
			int num = sourceInventory.GetSlotCount(sourceSlotIndex);
			int slotCount = targetInventory.GetSlotCount(targetSlotIndex);
			int slotCapacity = targetInventory.GetSlotCapacity(targetSlotIndex, slotValue);
			int slotProcessCapacity = targetInventory.GetSlotProcessCapacity(targetSlotIndex, slotValue);
			if (dragMode == DragMode.SingleItem)
			{
				num = MathUtils.Min(num, 1);
			}
			bool flag = false;
			if (slotProcessCapacity > 0)
			{
				int processCount = sourceInventory.RemoveSlotItems(sourceSlotIndex, MathUtils.Min(num, slotProcessCapacity));
				targetInventory.ProcessSlotItems(targetSlotIndex, slotValue, num, processCount, out int processedValue, out int processedCount);
				if (processedValue != 0 && processedCount != 0)
				{
					int count = MathUtils.Min(sourceInventory.GetSlotCapacity(sourceSlotIndex, processedValue), processedCount);
					sourceInventory.AddSlotItems(sourceSlotIndex, processedValue, count);
				}
				flag = true;
			}
			else if (!ProcessingOnly && (slotCount == 0 || slotValue == slotValue2) && slotCount < slotCapacity)
			{
				int num2 = MathUtils.Min(slotCapacity - slotCount, num);
				if (num2 > 0)
				{
					int count2 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num2);
					targetInventory.AddSlotItems(targetSlotIndex, slotValue, count2);
					flag = true;
				}
			}
			else if (!ProcessingOnly && targetInventory.GetSlotCapacity(targetSlotIndex, slotValue) >= num && sourceInventory.GetSlotCapacity(sourceSlotIndex, slotValue2) >= slotCount && sourceInventory.GetSlotCount(sourceSlotIndex) == num)
			{
				int count3 = targetInventory.RemoveSlotItems(targetSlotIndex, slotCount);
				int count4 = sourceInventory.RemoveSlotItems(sourceSlotIndex, num);
				targetInventory.AddSlotItems(targetSlotIndex, slotValue, count4);
				sourceInventory.AddSlotItems(sourceSlotIndex, slotValue2, count3);
				flag = true;
			}
			if (flag)
			{
				AudioManager.PlaySound("Audio/UI/ItemMoved", 1f, 0f, 0f);
			}
			return flag;
		}
	}
}
