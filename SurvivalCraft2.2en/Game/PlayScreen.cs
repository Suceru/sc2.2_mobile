using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Game
{
	public class PlayScreen : Screen
	{
		public ListPanelWidget m_worldsListWidget;

		public PlayScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/PlayScreen");
			LoadContents(this, node);
			m_worldsListWidget = Children.Find<ListPanelWidget>("WorldsList");
			ListPanelWidget worldsListWidget = m_worldsListWidget;
			worldsListWidget.ItemWidgetFactory = (Func<object, Widget>)Delegate.Combine(worldsListWidget.ItemWidgetFactory, (Func<object, Widget>)delegate(object item)
			{
				WorldInfo worldInfo = (WorldInfo)item;
				XElement node2 = ContentManager.Get<XElement>("Widgets/SavedWorldItem");
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
				LabelWidget labelWidget = containerWidget.Children.Find<LabelWidget>("WorldItem.Name");
				LabelWidget labelWidget2 = containerWidget.Children.Find<LabelWidget>("WorldItem.Details");
				containerWidget.Tag = worldInfo;
				labelWidget.Text = worldInfo.WorldSettings.Name;
				labelWidget2.Text = string.Format("{0} | {1:dd MMM yyyy HH:mm} | {2} | {3} | {4}", DataSizeFormatter.Format(worldInfo.Size), worldInfo.LastSaveTime.ToLocalTime(), (worldInfo.PlayerInfos.Count > 1) ? $"{worldInfo.PlayerInfos.Count} players" : "1 player", worldInfo.WorldSettings.GameMode.ToString(), worldInfo.WorldSettings.EnvironmentBehaviorMode.ToString());
				if (worldInfo.SerializationVersion != VersionsManager.SerializationVersion)
				{
					labelWidget2.Text = labelWidget2.Text + " | " + (string.IsNullOrEmpty(worldInfo.SerializationVersion) ? "(unknown)" : ("(" + worldInfo.SerializationVersion + ")"));
				}
				return containerWidget;
			});
			m_worldsListWidget.ScrollPosition = 0f;
			m_worldsListWidget.ScrollSpeed = 0f;
			m_worldsListWidget.ItemClicked += delegate(object item)
			{
				if (item != null && m_worldsListWidget.SelectedItem == item)
				{
					Play(item);
				}
			};
		}

		public override void Enter(object[] parameters)
		{
			BusyDialog dialog = new BusyDialog("Scanning Worlds", null);
			DialogsManager.ShowDialog(null, dialog);
			Task.Run(delegate
			{
				WorldInfo selectedItem = (WorldInfo)m_worldsListWidget.SelectedItem;
				WorldsManager.UpdateWorldsList();
				List<WorldInfo> worldInfos = new List<WorldInfo>(WorldsManager.WorldInfos);
				worldInfos.Sort((WorldInfo w1, WorldInfo w2) => DateTime.Compare(w2.LastSaveTime, w1.LastSaveTime));
				Dispatcher.Dispatch(delegate
				{
					m_worldsListWidget.ClearItems();
					foreach (WorldInfo item in worldInfos)
					{
						m_worldsListWidget.AddItem(item);
					}
					if (selectedItem != null)
					{
						m_worldsListWidget.SelectedItem = worldInfos.FirstOrDefault((WorldInfo wi) => wi.DirectoryName == selectedItem.DirectoryName);
					}
					DialogsManager.HideDialog(dialog);
				});
			});
		}

		public override void Update()
		{
			if (m_worldsListWidget.SelectedItem != null && WorldsManager.WorldInfos.IndexOf((WorldInfo)m_worldsListWidget.SelectedItem) < 0)
			{
				m_worldsListWidget.SelectedItem = null;
			}
			Children.Find<LabelWidget>("TopBar.Label").Text = $"Existing Worlds ({m_worldsListWidget.Items.Count})";
			Children.Find("Play").IsEnabled = (m_worldsListWidget.SelectedItem != null);
			Children.Find("Properties").IsEnabled = (m_worldsListWidget.SelectedItem != null);
			if (Children.Find<ButtonWidget>("Play").IsClicked && m_worldsListWidget.SelectedItem != null)
			{
				Play(m_worldsListWidget.SelectedItem);
			}
			if (Children.Find<ButtonWidget>("NewWorld").IsClicked)
			{
				if (WorldsManager.WorldInfos.Count >= 30)
				{
					DialogsManager.ShowDialog(null, new MessageDialog("Too many worlds", $"A maximum of {30} worlds is allowed on a device. Delete some to make space for new ones.", "OK", null, null));
				}
				else
				{
					ScreensManager.SwitchScreen("NewWorld");
					m_worldsListWidget.SelectedItem = null;
				}
			}
			if (Children.Find<ButtonWidget>("Properties").IsClicked && m_worldsListWidget.SelectedItem != null)
			{
				WorldInfo worldInfo = (WorldInfo)m_worldsListWidget.SelectedItem;
				ScreensManager.SwitchScreen("ModifyWorld", worldInfo.DirectoryName, worldInfo.WorldSettings);
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen("MainMenu");
				m_worldsListWidget.SelectedItem = null;
			}
		}

		public void Play(object item)
		{
			ScreensManager.SwitchScreen("GameLoading", item, null);
			m_worldsListWidget.SelectedItem = null;
		}
	}
}
