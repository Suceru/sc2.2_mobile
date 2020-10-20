using Engine;
using Engine.Media;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Game
{
	public class ViewGameLogDialog : Dialog
	{
		public ListPanelWidget m_listPanel;

		public ButtonWidget m_copyButton;

		public ButtonWidget m_filterButton;

		public ButtonWidget m_closeButton;

		public LogType m_filter;

		public ViewGameLogDialog()
		{
			XElement node = ContentManager.Get<XElement>("Dialogs/ViewGameLogDialog");
			LoadContents(this, node);
			m_listPanel = Children.Find<ListPanelWidget>("ViewGameLogDialog.ListPanel");
			m_copyButton = Children.Find<ButtonWidget>("ViewGameLogDialog.CopyButton");
			m_filterButton = Children.Find<ButtonWidget>("ViewGameLogDialog.FilterButton");
			m_closeButton = Children.Find<ButtonWidget>("ViewGameLogDialog.CloseButton");
			m_listPanel.ItemClicked += delegate(object item)
			{
				if (m_listPanel.SelectedItem == item)
				{
					DialogsManager.ShowDialog(base.ParentWidget, new MessageDialog("Log Item", item.ToString(), "OK", null, null));
				}
			};
			PopulateList();
		}

		public override void Update()
		{
			if (m_copyButton.IsClicked)
			{
				ClipboardManager.ClipboardString = GameLogSink.GetRecentLog(131072);
			}
			if (m_filterButton.IsClicked)
			{
				if (m_filter < LogType.Warning)
				{
					m_filter = LogType.Warning;
				}
				else if (m_filter < LogType.Error)
				{
					m_filter = LogType.Error;
				}
				else
				{
					m_filter = LogType.Debug;
				}
				PopulateList();
			}
			if (base.Input.Cancel || m_closeButton.IsClicked)
			{
				DialogsManager.HideDialog(this);
			}
			if (m_filter == LogType.Debug)
			{
				m_filterButton.Text = "All";
			}
			else if (m_filter == LogType.Warning)
			{
				m_filterButton.Text = "Warnings";
			}
			else if (m_filter == LogType.Error)
			{
				m_filterButton.Text = "Errors";
			}
		}

		public void PopulateList()
		{
			m_listPanel.ItemWidgetFactory = delegate(object item)
			{
				string text = (item != null) ? item.ToString() : string.Empty;
				Color color = Color.Gray;
				if (text.Contains("ERROR:"))
				{
					color = Color.Red;
				}
				else if (text.Contains("WARNING:"))
				{
					color = Color.DarkYellow;
				}
				else if (text.Contains("INFO:"))
				{
					color = Color.LightGray;
				}
				return new LabelWidget
				{
					Text = text,
					Font = BitmapFont.DebugFont,
					HorizontalAlignment = WidgetAlignment.Near,
					VerticalAlignment = WidgetAlignment.Center,
					Color = color
				};
			};
			List<string> recentLogLines = GameLogSink.GetRecentLogLines(131072);
			m_listPanel.ClearItems();
			if (recentLogLines.Count > 1000)
			{
				recentLogLines.RemoveRange(0, recentLogLines.Count - 1000);
			}
			foreach (string item in recentLogLines)
			{
				if (m_filter == LogType.Warning)
				{
					if (!item.Contains("WARNING:") && !item.Contains("ERROR:"))
					{
						continue;
					}
				}
				else if (m_filter == LogType.Error && !item.Contains("ERROR:"))
				{
					continue;
				}
				m_listPanel.AddItem(item);
			}
			m_listPanel.ScrollPosition = (float)m_listPanel.Items.Count * m_listPanel.ItemSize;
		}
	}
}
