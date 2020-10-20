using Engine;
using GameEntitySystem;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class CreativeInventoryWidget : CanvasWidget
	{
		public class Category
		{
			public string Name;

			public Color Color = Color.White;

			public ContainerWidget Panel;
		}

		public List<Category> m_categories = new List<Category>();

		public int m_activeCategoryIndex = -1;

		public ComponentCreativeInventory m_componentCreativeInventory;

		public ButtonWidget m_pageUpButton;

		public ButtonWidget m_pageDownButton;

		public LabelWidget m_pageLabel;

		public ButtonWidget m_categoryLeftButton;

		public ButtonWidget m_categoryRightButton;

		public ButtonWidget m_categoryButton;

		public ContainerWidget m_panelContainer;

		public Entity Entity => m_componentCreativeInventory.Entity;

		public ButtonWidget PageDownButton => m_pageDownButton;

		public ButtonWidget PageUpButton => m_pageUpButton;

		public LabelWidget PageLabel => m_pageLabel;

		public CreativeInventoryWidget(Entity entity)
		{
			m_componentCreativeInventory = entity.FindComponent<ComponentCreativeInventory>(throwOnError: true);
			XElement node = ContentManager.Get<XElement>("Widgets/CreativeInventoryWidget");
			LoadContents(this, node);
			m_categoryLeftButton = Children.Find<ButtonWidget>("CategoryLeftButton");
			m_categoryRightButton = Children.Find<ButtonWidget>("CategoryRightButton");
			m_categoryButton = Children.Find<ButtonWidget>("CategoryButton");
			m_pageUpButton = Children.Find<ButtonWidget>("PageUpButton");
			m_pageDownButton = Children.Find<ButtonWidget>("PageDownButton");
			m_pageLabel = Children.Find<LabelWidget>("PageLabel");
			m_panelContainer = Children.Find<ContainerWidget>("PanelContainer");
			CreativeInventoryPanel creativeInventoryPanel = new CreativeInventoryPanel(this)
			{
				IsVisible = false
			};
			m_panelContainer.Children.Add(creativeInventoryPanel);
			FurnitureInventoryPanel furnitureInventoryPanel = new FurnitureInventoryPanel(this)
			{
				IsVisible = false
			};
			m_panelContainer.Children.Add(furnitureInventoryPanel);
			foreach (string category in BlocksManager.Categories)
			{
				m_categories.Add(new Category
				{
					Name = category,
					Panel = creativeInventoryPanel
				});
			}
			m_categories.Add(new Category
			{
				Name = "Furniture",
				Panel = furnitureInventoryPanel
			});
			m_categories.Add(new Category
			{
				Name = "User",
				Panel = creativeInventoryPanel
			});
			m_categories.First((Category c) => c.Name == "Electrics").Color = new Color(128, 140, 255);
			m_categories.First((Category c) => c.Name == "Plants").Color = new Color(64, 160, 64);
			m_categories.First((Category c) => c.Name == "Weapons").Color = new Color(255, 128, 112);
		}

		public string GetCategoryName(int index)
		{
			return m_categories[index].Name;
		}

		public override void Update()
		{
			if (m_categoryLeftButton.IsClicked || base.Input.Left)
			{
				int num = --m_componentCreativeInventory.CategoryIndex;
			}
			if (m_categoryRightButton.IsClicked || base.Input.Right)
			{
				int num = ++m_componentCreativeInventory.CategoryIndex;
			}
			if (m_categoryButton.IsClicked)
			{
				ComponentPlayer componentPlayer = Entity.FindComponent<ComponentPlayer>();
				if (componentPlayer != null)
				{
					DialogsManager.ShowDialog(componentPlayer.GuiWidget, new ListSelectionDialog(string.Empty, m_categories, 56f, (object c) => new LabelWidget
					{
						Text = ((Category)c).Name,
						Color = ((Category)c).Color,
						HorizontalAlignment = WidgetAlignment.Center,
						VerticalAlignment = WidgetAlignment.Center
					}, delegate(object c)
					{
						if (c != null)
						{
							m_componentCreativeInventory.CategoryIndex = m_categories.IndexOf((Category)c);
						}
					}));
				}
			}
			m_componentCreativeInventory.CategoryIndex = MathUtils.Clamp(m_componentCreativeInventory.CategoryIndex, 0, m_categories.Count - 1);
			m_categoryButton.Text = m_categories[m_componentCreativeInventory.CategoryIndex].Name;
			m_categoryLeftButton.IsEnabled = (m_componentCreativeInventory.CategoryIndex > 0);
			m_categoryRightButton.IsEnabled = (m_componentCreativeInventory.CategoryIndex < m_categories.Count - 1);
			if (m_componentCreativeInventory.CategoryIndex != m_activeCategoryIndex)
			{
				foreach (Category category in m_categories)
				{
					category.Panel.IsVisible = false;
				}
				m_categories[m_componentCreativeInventory.CategoryIndex].Panel.IsVisible = true;
				m_activeCategoryIndex = m_componentCreativeInventory.CategoryIndex;
			}
		}
	}
}
