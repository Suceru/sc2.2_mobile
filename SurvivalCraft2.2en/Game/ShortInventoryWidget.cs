using Engine;
using System.Xml.Linq;

namespace Game
{
	public class ShortInventoryWidget : CanvasWidget
	{
		public GridPanelWidget m_inventoryGrid;

		public IInventory m_inventory;

		public ShortInventoryWidget()
		{
			XElement node = ContentManager.Get<XElement>("Widgets/ShortInventoryWidget");
			LoadContents(this, node);
			m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
		}

		public void AssignComponents(IInventory inventory)
		{
			if (inventory != m_inventory)
			{
				m_inventory = inventory;
				m_inventoryGrid.Children.Clear();
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			int max = (m_inventory is ComponentCreativeInventory) ? 10 : 7;
			m_inventory.VisibleSlotsCount = MathUtils.Clamp((int)((parentAvailableSize.X - 320f - 25f) / 72f), 7, max);
			if (m_inventory.VisibleSlotsCount != m_inventoryGrid.Children.Count)
			{
				m_inventoryGrid.Children.Clear();
				m_inventoryGrid.RowsCount = 1;
				m_inventoryGrid.ColumnsCount = m_inventory.VisibleSlotsCount;
				for (int i = 0; i < m_inventoryGrid.ColumnsCount; i++)
				{
					InventorySlotWidget inventorySlotWidget = new InventorySlotWidget();
					inventorySlotWidget.AssignInventorySlot(m_inventory, i);
					inventorySlotWidget.BevelColor = new Color(181, 172, 154) * 0.6f;
					inventorySlotWidget.CenterColor = new Color(181, 172, 154) * 0.33f;
					m_inventoryGrid.Children.Add(inventorySlotWidget);
					m_inventoryGrid.SetWidgetCell(inventorySlotWidget, new Point2(i, 0));
				}
			}
			base.MeasureOverride(parentAvailableSize);
		}
	}
}
