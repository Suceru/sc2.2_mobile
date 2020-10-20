using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class FurnitureInventoryPanel : CanvasWidget
	{
		public CreativeInventoryWidget m_creativeInventoryWidget;

		public ComponentPlayer m_componentPlayer;

		public ListPanelWidget m_furnitureSetList;

		public GridPanelWidget m_inventoryGrid;

		public ButtonWidget m_addButton;

		public ButtonWidget m_moreButton;

		public int m_pagesCount;

		public int m_assignedPage;

		public bool m_ignoreSelectionChanged;

		public bool m_populateNeeded;

		public SubsystemTerrain SubsystemTerrain
		{
			get;
			set;
		}

		public SubsystemFurnitureBlockBehavior SubsystemFurnitureBlockBehavior
		{
			get;
			set;
		}

		public ComponentFurnitureInventory ComponentFurnitureInventory
		{
			get;
			set;
		}

		public FurnitureInventoryPanel(CreativeInventoryWidget creativeInventoryWidget)
		{
			m_creativeInventoryWidget = creativeInventoryWidget;
			ComponentFurnitureInventory = creativeInventoryWidget.Entity.FindComponent<ComponentFurnitureInventory>(throwOnError: true);
			m_componentPlayer = creativeInventoryWidget.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			SubsystemFurnitureBlockBehavior = ComponentFurnitureInventory.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
			SubsystemTerrain = ComponentFurnitureInventory.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			XElement node = ContentManager.Get<XElement>("Widgets/FurnitureInventoryPanel");
			LoadContents(this, node);
			m_furnitureSetList = Children.Find<ListPanelWidget>("FurnitureSetList");
			m_inventoryGrid = Children.Find<GridPanelWidget>("InventoryGrid");
			m_addButton = Children.Find<ButtonWidget>("AddButton");
			m_moreButton = Children.Find<ButtonWidget>("MoreButton");
			for (int i = 0; i < m_inventoryGrid.RowsCount; i++)
			{
				for (int j = 0; j < m_inventoryGrid.ColumnsCount; j++)
				{
					InventorySlotWidget widget = new InventorySlotWidget();
					m_inventoryGrid.Children.Add(widget);
					m_inventoryGrid.SetWidgetCell(widget, new Point2(j, i));
				}
			}
			ListPanelWidget furnitureSetList = m_furnitureSetList;
			furnitureSetList.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(furnitureSetList.ItemWidgetFactory, (Func<object, Widget>)((object item) => new FurnitureSetItemWidget(this, (FurnitureSet)item)));
			m_furnitureSetList.SelectionChanged += delegate
			{
				if (!m_ignoreSelectionChanged && ComponentFurnitureInventory.FurnitureSet != m_furnitureSetList.SelectedItem as FurnitureSet)
				{
					ComponentFurnitureInventory.PageIndex = 0;
					ComponentFurnitureInventory.FurnitureSet = (m_furnitureSetList.SelectedItem as FurnitureSet);
					if (ComponentFurnitureInventory.FurnitureSet == null)
					{
						m_furnitureSetList.SelectedIndex = 0;
					}
					AssignInventorySlots();
				}
			};
			m_populateNeeded = true;
		}

		public override void Update()
		{
			if (m_populateNeeded)
			{
				Populate();
				m_populateNeeded = false;
			}
			if (ComponentFurnitureInventory.PageIndex != m_assignedPage)
			{
				AssignInventorySlots();
			}
			m_creativeInventoryWidget.PageUpButton.IsEnabled = (ComponentFurnitureInventory.PageIndex > 0);
			m_creativeInventoryWidget.PageDownButton.IsEnabled = (ComponentFurnitureInventory.PageIndex < m_pagesCount - 1);
			m_creativeInventoryWidget.PageLabel.Text = ((m_pagesCount > 0) ? $"{ComponentFurnitureInventory.PageIndex + 1}/{m_pagesCount}" : string.Empty);
			m_moreButton.IsEnabled = (ComponentFurnitureInventory.FurnitureSet != null);
			if (base.Input.Scroll.HasValue && HitTestGlobal(base.Input.Scroll.Value.XY).IsChildWidgetOf(m_inventoryGrid))
			{
				ComponentFurnitureInventory.PageIndex -= (int)base.Input.Scroll.Value.Z;
			}
			if (m_creativeInventoryWidget.PageUpButton.IsClicked)
			{
				int num = --ComponentFurnitureInventory.PageIndex;
			}
			if (m_creativeInventoryWidget.PageDownButton.IsClicked)
			{
				int num = ++ComponentFurnitureInventory.PageIndex;
			}
			ComponentFurnitureInventory.PageIndex = ((m_pagesCount > 0) ? MathUtils.Clamp(ComponentFurnitureInventory.PageIndex, 0, m_pagesCount - 1) : 0);
			if (m_addButton.IsClicked)
			{
				List<Tuple<string, Action>> list = new List<Tuple<string, Action>>();
				list.Add(new Tuple<string, Action>("New", delegate
				{
					if (SubsystemFurnitureBlockBehavior.FurnitureSets.Count < 32)
					{
						NewFurnitureSet();
					}
					else
					{
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Too many furniture sets", "Delete some furniture sets before adding a new one.", "OK", null, null));
					}
				}));
				list.Add(new Tuple<string, Action>("Import From Content", delegate
				{
					ImportFurnitureSet(SubsystemTerrain);
				}));
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog("Add Furniture Set", list, 64f, (object t) => ((Tuple<string, Action>)t).Item1, delegate(object t)
				{
					((Tuple<string, Action>)t).Item2();
				}));
			}
			if (m_moreButton.IsClicked && ComponentFurnitureInventory.FurnitureSet != null)
			{
				List<Tuple<string, Action>> list2 = new List<Tuple<string, Action>>();
				list2.Add(new Tuple<string, Action>("Rename", delegate
				{
					RenameFurnitureSet();
				}));
				list2.Add(new Tuple<string, Action>("Delete", delegate
				{
					if (SubsystemFurnitureBlockBehavior.GetFurnitureSetDesigns(ComponentFurnitureInventory.FurnitureSet).Count() > 0)
					{
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Warning", "All furniture in the set will become uncategorized and will be deleted if not used in the world.", "Delete", "Cancel", delegate(MessageDialogButton b)
						{
							if (b == MessageDialogButton.Button1)
							{
								DeleteFurnitureSet();
							}
						}));
					}
					else
					{
						DeleteFurnitureSet();
					}
				}));
				list2.Add(new Tuple<string, Action>("Move Up", delegate
				{
					MoveFurnitureSet(-1);
				}));
				list2.Add(new Tuple<string, Action>("Move Down", delegate
				{
					MoveFurnitureSet(1);
				}));
				list2.Add(new Tuple<string, Action>("Export To Content", delegate
				{
					ExportFurnitureSet();
				}));
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog("Furniture Set Action", list2, 64f, (object t) => ((Tuple<string, Action>)t).Item1, delegate(object t)
				{
					((Tuple<string, Action>)t).Item2();
				}));
			}
		}

		public override void UpdateCeases()
		{
			base.UpdateCeases();
			ComponentFurnitureInventory.ClearSlots();
			m_populateNeeded = true;
		}

		public void Invalidate()
		{
			m_populateNeeded = true;
		}

		public void Populate()
		{
			ComponentFurnitureInventory.FillSlots();
			try
			{
				m_ignoreSelectionChanged = true;
				m_furnitureSetList.ClearItems();
				m_furnitureSetList.AddItem(null);
				foreach (FurnitureSet furnitureSet in SubsystemFurnitureBlockBehavior.FurnitureSets)
				{
					m_furnitureSetList.AddItem(furnitureSet);
				}
			}
			finally
			{
				m_ignoreSelectionChanged = false;
			}
			m_furnitureSetList.SelectedItem = ComponentFurnitureInventory.FurnitureSet;
			AssignInventorySlots();
		}

		public void AssignInventorySlots()
		{
			List<int> list = new List<int>();
			for (int i = 0; i < ComponentFurnitureInventory.SlotsCount; i++)
			{
				int slotValue = ComponentFurnitureInventory.GetSlotValue(i);
				int slotCount = ComponentFurnitureInventory.GetSlotCount(i);
				if (slotValue != 0 && slotCount > 0 && Terrain.ExtractContents(slotValue) == 227)
				{
					int designIndex = FurnitureBlock.GetDesignIndex(Terrain.ExtractData(slotValue));
					FurnitureDesign design = SubsystemFurnitureBlockBehavior.GetDesign(designIndex);
					if (design != null && design.FurnitureSet == ComponentFurnitureInventory.FurnitureSet)
					{
						list.Add(i);
					}
				}
			}
			List<InventorySlotWidget> list2 = new List<InventorySlotWidget>((from w in m_inventoryGrid.Children
				select w as InventorySlotWidget into w
				where w != null
				select w).Cast<InventorySlotWidget>());
			int num = ComponentFurnitureInventory.PageIndex * list2.Count;
			for (int j = 0; j < list2.Count; j++)
			{
				if (num < list.Count)
				{
					list2[j].AssignInventorySlot(ComponentFurnitureInventory, list[num]);
				}
				else
				{
					list2[j].AssignInventorySlot(null, 0);
				}
				num++;
			}
			m_pagesCount = (list.Count + list2.Count - 1) / list2.Count;
			m_assignedPage = ComponentFurnitureInventory.PageIndex;
		}

		public void NewFurnitureSet()
		{
			ComponentPlayer componentPlayer = ComponentFurnitureInventory.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			base.Input.EnterText(componentPlayer.GuiWidget, "Furniture Set Name", "New Set", 20, delegate(string s)
			{
				if (s != null)
				{
					FurnitureSet furnitureSet = SubsystemFurnitureBlockBehavior.NewFurnitureSet(s, null);
					ComponentFurnitureInventory.FurnitureSet = furnitureSet;
					Populate();
					m_furnitureSetList.ScrollToItem(furnitureSet);
				}
			});
		}

		public void DeleteFurnitureSet()
		{
			FurnitureSet furnitureSet = m_furnitureSetList.SelectedItem as FurnitureSet;
			if (furnitureSet != null)
			{
				int num = SubsystemFurnitureBlockBehavior.FurnitureSets.IndexOf(furnitureSet);
				SubsystemFurnitureBlockBehavior.DeleteFurnitureSet(furnitureSet);
				SubsystemFurnitureBlockBehavior.GarbageCollectDesigns();
				ComponentFurnitureInventory.FurnitureSet = ((num > 0) ? SubsystemFurnitureBlockBehavior.FurnitureSets[num - 1] : null);
				Invalidate();
			}
		}

		public void RenameFurnitureSet()
		{
			FurnitureSet furnitureSet = m_furnitureSetList.SelectedItem as FurnitureSet;
			if (furnitureSet != null)
			{
				ComponentPlayer componentPlayer = ComponentFurnitureInventory.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
				base.Input.EnterText(componentPlayer.GuiWidget, "Furniture Set Name", furnitureSet.Name, 20, delegate(string s)
				{
					if (s != null)
					{
						furnitureSet.Name = s;
						Invalidate();
					}
				});
			}
		}

		public void MoveFurnitureSet(int move)
		{
			FurnitureSet furnitureSet = m_furnitureSetList.SelectedItem as FurnitureSet;
			if (furnitureSet != null)
			{
				SubsystemFurnitureBlockBehavior.MoveFurnitureSet(furnitureSet, move);
				Invalidate();
			}
		}

		public void ImportFurnitureSet(SubsystemTerrain subsystemTerrain)
		{
			FurniturePacksManager.UpdateFurniturePacksList();
			if (FurniturePacksManager.FurniturePackNames.Count() == 0)
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("No Furniture Packs", "No furniture packs found in your content. Download some from Community Content.", "OK", null, null));
			}
			else
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new ListSelectionDialog("Select Furniture Pack", FurniturePacksManager.FurniturePackNames, 64f, (object s) => FurniturePacksManager.GetDisplayName((string)s), delegate(object s)
				{
					try
					{
						int num = 0;
						int num2 = 0;
						string text = (string)s;
						List<List<FurnitureDesign>> list = FurnitureDesign.ListChains(FurniturePacksManager.LoadFurniturePack(subsystemTerrain, text));
						List<FurnitureDesign> list2 = new List<FurnitureDesign>();
						SubsystemFurnitureBlockBehavior.GarbageCollectDesigns();
						foreach (List<FurnitureDesign> item in list)
						{
							FurnitureDesign furnitureDesign = SubsystemFurnitureBlockBehavior.TryAddDesignChain(item[0], garbageCollectIfNeeded: false);
							if (furnitureDesign == item[0])
							{
								list2.Add(furnitureDesign);
							}
							else if (furnitureDesign == null)
							{
								num2++;
							}
							else
							{
								num++;
							}
						}
						if (list2.Count > 0)
						{
							FurnitureSet furnitureSet = SubsystemFurnitureBlockBehavior.NewFurnitureSet(FurniturePacksManager.GetDisplayName(text), text);
							foreach (FurnitureDesign item2 in list2)
							{
								SubsystemFurnitureBlockBehavior.AddToFurnitureSet(item2, furnitureSet);
							}
							ComponentFurnitureInventory.FurnitureSet = furnitureSet;
						}
						Invalidate();
						string text2 = $"{list2.Count} design(s) added. ";
						if (num > 0)
						{
							text2 += $"{num} design(s) were already present in the world and were skipped. ";
						}
						if (num2 > 0)
						{
							text2 += $"{num2} design(s) were skipped because {1024} designs limit is reached. ";
						}
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Import Successful", text2.Trim(), "OK", null, null));
					}
					catch (Exception ex)
					{
						DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Import Failed", ex.Message, "OK", null, null));
					}
				}));
			}
		}

		public void ExportFurnitureSet()
		{
			try
			{
				FurnitureDesign[] designs = SubsystemFurnitureBlockBehavior.GetFurnitureSetDesigns(ComponentFurnitureInventory.FurnitureSet).ToArray();
				string displayName = FurniturePacksManager.GetDisplayName(FurniturePacksManager.CreateFurniturePack(ComponentFurnitureInventory.FurnitureSet.Name, designs));
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Exported", $"Furniture pack \"{displayName}\" exported to content.", "OK", null, null));
			}
			catch (Exception ex)
			{
				DialogsManager.ShowDialog(m_componentPlayer.GuiWidget, new MessageDialog("Export Failed", ex.Message, "OK", null, null));
			}
		}
	}
}
