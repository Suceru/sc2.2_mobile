using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class ManageContentScreen : Screen
	{
		public class ListItem
		{
			public ExternalContentType Type;

			public bool IsBuiltIn;

			public string Name;

			public string DisplayName;

			public DateTime CreationTime;

			public int UseCount;
		}

		public ListPanelWidget m_contentList;

		public ButtonWidget m_deleteButton;

		public ButtonWidget m_uploadButton;

		public LabelWidget m_filterLabel;

		public ButtonWidget m_changeFilterButton;

		public BlocksTexturesCache m_blocksTexturesCache = new BlocksTexturesCache();

		public CharacterSkinsCache m_characterSkinsCache = new CharacterSkinsCache();

		public ExternalContentType m_filter;

		public ManageContentScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/ManageContentScreen");
			LoadContents(this, node);
			m_contentList = Children.Find<ListPanelWidget>("ContentList");
			m_deleteButton = Children.Find<ButtonWidget>("DeleteButton");
			m_uploadButton = Children.Find<ButtonWidget>("UploadButton");
			m_changeFilterButton = Children.Find<ButtonWidget>("ChangeFilter");
			m_filterLabel = Children.Find<LabelWidget>("Filter");
			m_contentList.ItemWidgetFactory = delegate(object obj)
			{
				ListItem listItem = (ListItem)obj;
				ContainerWidget containerWidget;
				if (listItem.Type == ExternalContentType.BlocksTexture)
				{
					XElement node2 = ContentManager.Get<XElement>("Widgets/BlocksTextureItem");
					containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
					RectangleWidget rectangleWidget = containerWidget.Children.Find<RectangleWidget>("BlocksTextureItem.Icon");
					LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("BlocksTextureItem.Text");
					LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("BlocksTextureItem.Details");
					Texture2D texture = m_blocksTexturesCache.GetTexture(listItem.Name);
					BlocksTexturesManager.GetCreationDate(listItem.Name);
					rectangleWidget.Subtexture = new Subtexture(texture, Vector2.Zero, Vector2.One);
					labelWidget.Text = listItem.DisplayName;
					labelWidget2.Text = $"Blocks Texture | {texture.Width}x{texture.Height}";
					if (!listItem.IsBuiltIn)
					{
						labelWidget2.Text += $" | {listItem.CreationTime.ToLocalTime():dd MMM yyyy HH:mm}";
						if (listItem.UseCount > 0)
						{
							labelWidget2.Text += $" | in {listItem.UseCount} world(s)";
						}
					}
				}
				else
				{
					if (listItem.Type != ExternalContentType.CharacterSkin)
					{
						if (listItem.Type == ExternalContentType.FurniturePack)
						{
							XElement node3 = ContentManager.Get<XElement>("Widgets/FurniturePackItem");
							containerWidget = (ContainerWidget)Widget.LoadWidget(this, node3, null);
							LabelWidget labelWidget3 = containerWidget.Children.Find<LabelWidget>("FurniturePackItem.Text");
							LabelWidget labelWidget4 = containerWidget.Children.Find<LabelWidget>("FurniturePackItem.Details");
							labelWidget3.Text = listItem.DisplayName;
							try
							{
								List<FurnitureDesign> designs = FurniturePacksManager.LoadFurniturePack(null, listItem.Name);
								labelWidget4.Text = $"Furniture Pack | {FurnitureDesign.ListChains(designs).Count} designs(s)";
								if (string.IsNullOrEmpty(listItem.Name))
								{
									return containerWidget;
								}
								labelWidget4.Text += $" | {listItem.CreationTime.ToLocalTime():dd MMM yyyy HH:mm}";
								return containerWidget;
							}
							catch (Exception ex)
							{
								labelWidget4.Text = labelWidget4.Text + "Error: " + ex.Message;
								return containerWidget;
							}
						}
						throw new InvalidOperationException("Invalid item type.");
					}
					XElement node4 = ContentManager.Get<XElement>("Widgets/CharacterSkinItem");
					containerWidget = (ContainerWidget)Widget.LoadWidget(this, node4, null);
					PlayerModelWidget playerModelWidget = containerWidget.Children.Find<PlayerModelWidget>("CharacterSkinItem.Model");
					LabelWidget labelWidget5 = containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Text");
					LabelWidget labelWidget6 = containerWidget.Children.Find<LabelWidget>("CharacterSkinItem.Details");
					Texture2D texture2 = m_characterSkinsCache.GetTexture(listItem.Name);
					playerModelWidget.PlayerClass = PlayerClass.Male;
					playerModelWidget.CharacterSkinTexture = texture2;
					labelWidget5.Text = listItem.DisplayName;
					labelWidget6.Text = $"Character Skin | {texture2.Width}x{texture2.Height}";
					if (!listItem.IsBuiltIn)
					{
						labelWidget6.Text += $" | {listItem.CreationTime.ToLocalTime():dd MMM yyyy HH:mm}";
						if (listItem.UseCount > 0)
						{
							labelWidget6.Text += $" | in {listItem.UseCount} world(s)";
						}
					}
				}
				return containerWidget;
			};
		}

		public override void Enter(object[] parameters)
		{
			UpdateList();
		}

		public override void Leave()
		{
			m_blocksTexturesCache.Clear();
			m_characterSkinsCache.Clear();
		}

		public override void Update()
		{
			ListItem selectedItem = (ListItem)m_contentList.SelectedItem;
			m_deleteButton.IsEnabled = (selectedItem != null && !selectedItem.IsBuiltIn);
			m_uploadButton.IsEnabled = (selectedItem != null && !selectedItem.IsBuiltIn);
			m_filterLabel.Text = GetFilterDisplayName(m_filter);
			if (m_deleteButton.IsClicked)
			{
				string smallMessage = (selectedItem.UseCount <= 0) ? $"\"{selectedItem.DisplayName}\" will be irrecoverably deleted." : $"\"{selectedItem.DisplayName}\" will be irrecoverably deleted.\n(used by {selectedItem.UseCount} world(s), they will revert to using the default)";
				DialogsManager.ShowDialog(null, new MessageDialog("Are you sure?", smallMessage, "Yes", "No", delegate(MessageDialogButton button)
				{
					if (button == MessageDialogButton.Button1)
					{
						ExternalContentManager.DeleteExternalContent(selectedItem.Type, selectedItem.Name);
						UpdateList();
					}
				}));
			}
			if (m_uploadButton.IsClicked)
			{
				ExternalContentManager.ShowUploadUi(selectedItem.Type, selectedItem.Name);
			}
			if (m_changeFilterButton.IsClicked)
			{
				List<ExternalContentType> list = new List<ExternalContentType>();
				list.Add(ExternalContentType.Unknown);
				list.Add(ExternalContentType.BlocksTexture);
				list.Add(ExternalContentType.CharacterSkin);
				list.Add(ExternalContentType.FurniturePack);
				DialogsManager.ShowDialog(null, new ListSelectionDialog("Select Items Filter", list, 60f, (object item) => GetFilterDisplayName((ExternalContentType)item), delegate(object item)
				{
					if ((ExternalContentType)item != m_filter)
					{
						m_filter = (ExternalContentType)item;
						UpdateList();
					}
				}));
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}

		public void UpdateList()
		{
			WorldsManager.UpdateWorldsList();
			List<ListItem> list = new List<ListItem>();
			if (m_filter == ExternalContentType.BlocksTexture || m_filter == ExternalContentType.Unknown)
			{
				BlocksTexturesManager.UpdateBlocksTexturesList();
				foreach (string name2 in BlocksTexturesManager.BlockTexturesNames)
				{
					list.Add(new ListItem
					{
						Name = name2,
						IsBuiltIn = BlocksTexturesManager.IsBuiltIn(name2),
						Type = ExternalContentType.BlocksTexture,
						DisplayName = BlocksTexturesManager.GetDisplayName(name2),
						CreationTime = BlocksTexturesManager.GetCreationDate(name2),
						UseCount = WorldsManager.WorldInfos.Count((WorldInfo wi) => wi.WorldSettings.BlocksTextureName == name2)
					});
				}
			}
			if (m_filter == ExternalContentType.CharacterSkin || m_filter == ExternalContentType.Unknown)
			{
				CharacterSkinsManager.UpdateCharacterSkinsList();
				foreach (string name in CharacterSkinsManager.CharacterSkinsNames)
				{
					list.Add(new ListItem
					{
						Name = name,
						IsBuiltIn = CharacterSkinsManager.IsBuiltIn(name),
						Type = ExternalContentType.CharacterSkin,
						DisplayName = CharacterSkinsManager.GetDisplayName(name),
						CreationTime = CharacterSkinsManager.GetCreationDate(name),
						UseCount = WorldsManager.WorldInfos.Count((WorldInfo wi) => wi.PlayerInfos.Any((PlayerInfo pi) => pi.CharacterSkinName == name))
					});
				}
			}
			if (m_filter == ExternalContentType.FurniturePack || m_filter == ExternalContentType.Unknown)
			{
				FurniturePacksManager.UpdateFurniturePacksList();
				foreach (string furniturePackName in FurniturePacksManager.FurniturePackNames)
				{
					list.Add(new ListItem
					{
						Name = furniturePackName,
						IsBuiltIn = false,
						Type = ExternalContentType.FurniturePack,
						DisplayName = FurniturePacksManager.GetDisplayName(furniturePackName),
						CreationTime = FurniturePacksManager.GetCreationDate(furniturePackName)
					});
				}
			}
			list.Sort(delegate(ListItem o1, ListItem o2)
			{
				if (o1.IsBuiltIn && !o2.IsBuiltIn)
				{
					return -1;
				}
				if (o2.IsBuiltIn && !o1.IsBuiltIn)
				{
					return 1;
				}
				if (string.IsNullOrEmpty(o1.Name) && !string.IsNullOrEmpty(o2.Name))
				{
					return -1;
				}
				return (!string.IsNullOrEmpty(o1.Name) && string.IsNullOrEmpty(o2.Name)) ? 1 : string.Compare(o1.DisplayName, o2.DisplayName);
			});
			m_contentList.ClearItems();
			foreach (ListItem item in list)
			{
				m_contentList.AddItem(item);
			}
		}

		public static string GetFilterDisplayName(ExternalContentType filter)
		{
			if (filter == ExternalContentType.Unknown)
			{
				return "All Items";
			}
			return ExternalContentManager.GetEntryTypeDescription(filter);
		}
	}
}
