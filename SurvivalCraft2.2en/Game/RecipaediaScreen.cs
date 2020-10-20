using Engine;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class RecipaediaScreen : Screen
	{
		public ListPanelWidget m_blocksList;

		public LabelWidget m_categoryLabel;

		public ButtonWidget m_prevCategoryButton;

		public ButtonWidget m_nextCategoryButton;

		public ButtonWidget m_detailsButton;

		public ButtonWidget m_recipesButton;

		public Screen m_previousScreen;

		public List<string> m_categories = new List<string>();

		public int m_categoryIndex;

		public int m_listCategoryIndex = -1;

		public RecipaediaScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/RecipaediaScreen");
			LoadContents(this, node);
			m_blocksList = Children.Find<ListPanelWidget>("BlocksList");
			m_categoryLabel = Children.Find<LabelWidget>("Category");
			m_prevCategoryButton = Children.Find<ButtonWidget>("PreviousCategory");
			m_nextCategoryButton = Children.Find<ButtonWidget>("NextCategory");
			m_detailsButton = Children.Find<ButtonWidget>("DetailsButton");
			m_recipesButton = Children.Find<ButtonWidget>("RecipesButton");
			m_categories.Add(null);
			m_categories.AddRange(BlocksManager.Categories);
			m_blocksList.ItemWidgetFactory = delegate(object item)
			{
				int value = (int)item;
				int num = Terrain.ExtractContents(value);
				Block block = BlocksManager.Blocks[num];
				XElement node2 = ContentManager.Get<XElement>("Widgets/RecipaediaItem");
				ContainerWidget obj = (ContainerWidget)Widget.LoadWidget(this, node2, null);
				obj.Children.Find<BlockIconWidget>("RecipaediaItem.Icon").Value = value;
				obj.Children.Find<LabelWidget>("RecipaediaItem.Text").Text = block.GetDisplayName(null, value);
				obj.Children.Find<LabelWidget>("RecipaediaItem.Details").Text = block.GetDescription(value);
				return obj;
			};
			m_blocksList.ItemClicked += delegate(object item)
			{
				if (m_blocksList.SelectedItem == item && item is int)
				{
					ScreensManager.SwitchScreen("RecipaediaDescription", item, m_blocksList.Items.Cast<int>().ToList());
				}
			};
		}

		public override void Enter(object[] parameters)
		{
			if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaRecipes") && ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaDescription"))
			{
				m_previousScreen = ScreensManager.PreviousScreen;
			}
		}

		public override void Update()
		{
			if (m_listCategoryIndex != m_categoryIndex)
			{
				PopulateBlocksList();
			}
			string arg = m_categories[m_categoryIndex] ?? "All Blocks";
			m_categoryLabel.Text = $"{arg} ({m_blocksList.Items.Count})";
			m_prevCategoryButton.IsEnabled = (m_categoryIndex > 0);
			m_nextCategoryButton.IsEnabled = (m_categoryIndex < m_categories.Count - 1);
			int? value = null;
			int num = 0;
			if (m_blocksList.SelectedItem is int)
			{
				value = (int)m_blocksList.SelectedItem;
				num = CraftingRecipesManager.Recipes.Count((CraftingRecipe r) => r.ResultValue == value);
			}
			if (num > 0)
			{
				m_recipesButton.Text = string.Format("{0} {1}", num, (num == 1) ? "recipe" : "recipes");
				m_recipesButton.IsEnabled = true;
			}
			else
			{
				m_recipesButton.Text = "No recipes";
				m_recipesButton.IsEnabled = false;
			}
			m_detailsButton.IsEnabled = value.HasValue;
			if (m_prevCategoryButton.IsClicked || base.Input.Left)
			{
				m_categoryIndex = MathUtils.Max(m_categoryIndex - 1, 0);
			}
			if (m_nextCategoryButton.IsClicked || base.Input.Right)
			{
				m_categoryIndex = MathUtils.Min(m_categoryIndex + 1, m_categories.Count - 1);
			}
			if (value.HasValue && m_detailsButton.IsClicked)
			{
				ScreensManager.SwitchScreen("RecipaediaDescription", value.Value, m_blocksList.Items.Cast<int>().ToList());
			}
			if (value.HasValue && m_recipesButton.IsClicked)
			{
				ScreensManager.SwitchScreen("RecipaediaRecipes", value.Value);
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(m_previousScreen);
			}
		}

		public void PopulateBlocksList()
		{
			m_listCategoryIndex = m_categoryIndex;
			string text = m_categories[m_categoryIndex];
			m_blocksList.ScrollPosition = 0f;
			m_blocksList.ClearItems();
			foreach (Block item in BlocksManager.Blocks.OrderBy((Block b) => b.DisplayOrder))
			{
				foreach (int creativeValue in item.GetCreativeValues())
				{
					if (text == null || item.GetCategory(creativeValue) == text)
					{
						m_blocksList.AddItem(creativeValue);
					}
				}
			}
		}
	}
}
