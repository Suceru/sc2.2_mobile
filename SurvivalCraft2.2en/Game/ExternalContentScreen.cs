using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class ExternalContentScreen : Screen
	{
		public LabelWidget m_directoryLabel;

		public ListPanelWidget m_directoryList;

		public LabelWidget m_providerNameLabel;

		public ButtonWidget m_changeProviderButton;

		public ButtonWidget m_loginLogoutButton;

		public ButtonWidget m_upDirectoryButton;

		public ButtonWidget m_actionButton;

		public ButtonWidget m_copyLinkButton;

		public string m_path;

		public bool m_listDirty;

		public Dictionary<string, bool> m_downloadedFiles = new Dictionary<string, bool>();

		public IExternalContentProvider m_externalContentProvider = ExternalContentManager.DefaultProvider;

		public ExternalContentScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/ExternalContentScreen");
			LoadContents(this, node);
			m_directoryLabel = Children.Find<LabelWidget>("TopBar.Label");
			m_directoryList = Children.Find<ListPanelWidget>("DirectoryList");
			m_providerNameLabel = Children.Find<LabelWidget>("ProviderName");
			m_changeProviderButton = Children.Find<ButtonWidget>("ChangeProvider");
			m_loginLogoutButton = Children.Find<ButtonWidget>("LoginLogout");
			m_upDirectoryButton = Children.Find<ButtonWidget>("UpDirectory");
			m_actionButton = Children.Find<ButtonWidget>("Action");
			m_copyLinkButton = Children.Find<ButtonWidget>("CopyLink");
			m_directoryList.ItemWidgetFactory = delegate(object item)
			{
				ExternalContentEntry externalContentEntry2 = (ExternalContentEntry)item;
				XElement node2 = ContentManager.Get<XElement>("Widgets/ExternalContentItem");
				ContainerWidget containerWidget = (ContainerWidget)Widget.LoadWidget(this, node2, null);
				string fileName = Storage.GetFileName(externalContentEntry2.Path);
				string text = m_downloadedFiles.ContainsKey(externalContentEntry2.Path) ? " | (downloaded)" : string.Empty;
				string text2 = (externalContentEntry2.Type != ExternalContentType.Directory) ? $"{ExternalContentManager.GetEntryTypeDescription(externalContentEntry2.Type)} | {DataSizeFormatter.Format(externalContentEntry2.Size)} | {externalContentEntry2.Time:dd-MMM-yyyy HH:mm}{text}" : ExternalContentManager.GetEntryTypeDescription(externalContentEntry2.Type);
				containerWidget.Children.Find<RectangleWidget>("ExternalContentItem.Icon").Subtexture = ExternalContentManager.GetEntryTypeIcon(externalContentEntry2.Type);
				containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Text").Text = fileName;
				containerWidget.Children.Find<LabelWidget>("ExternalContentItem.Details").Text = text2;
				return containerWidget;
			};
			m_directoryList.ItemClicked += delegate(object item)
			{
				if (m_directoryList.SelectedItem == item)
				{
					ExternalContentEntry externalContentEntry = item as ExternalContentEntry;
					if (externalContentEntry != null && externalContentEntry.Type == ExternalContentType.Directory)
					{
						SetPath(externalContentEntry.Path);
					}
				}
			};
		}

		public override void Enter(object[] parameters)
		{
			m_directoryList.ClearItems();
			SetPath(null);
			m_listDirty = true;
		}

		public override void Update()
		{
			if (m_listDirty)
			{
				m_listDirty = false;
				UpdateList();
			}
			ExternalContentEntry externalContentEntry = null;
			if (m_directoryList.SelectedIndex.HasValue)
			{
				externalContentEntry = (m_directoryList.Items[m_directoryList.SelectedIndex.Value] as ExternalContentEntry);
			}
			if (externalContentEntry != null)
			{
				m_actionButton.IsVisible = true;
				if (externalContentEntry.Type == ExternalContentType.Directory)
				{
					m_actionButton.Text = "Enter";
					m_actionButton.IsEnabled = true;
					m_copyLinkButton.IsEnabled = false;
				}
				else
				{
					m_actionButton.Text = "Download";
					if (ExternalContentManager.IsEntryTypeDownloadSupported(ExternalContentManager.ExtensionToType(Storage.GetExtension(externalContentEntry.Path).ToLower())))
					{
						m_actionButton.IsEnabled = true;
						m_copyLinkButton.IsEnabled = true;
					}
					else
					{
						m_actionButton.IsEnabled = false;
						m_copyLinkButton.IsEnabled = false;
					}
				}
			}
			else
			{
				m_actionButton.IsVisible = false;
				m_copyLinkButton.IsVisible = false;
			}
			m_directoryLabel.Text = (m_externalContentProvider.IsLoggedIn ? ("Contents Of " + m_path) : "Not logged in");
			m_providerNameLabel.Text = m_externalContentProvider.DisplayName;
			m_upDirectoryButton.IsEnabled = (m_externalContentProvider.IsLoggedIn && m_path != "/");
			m_loginLogoutButton.Text = (m_externalContentProvider.IsLoggedIn ? "Logout" : "Login");
			m_loginLogoutButton.IsVisible = m_externalContentProvider.RequiresLogin;
			m_copyLinkButton.IsVisible = m_externalContentProvider.SupportsLinks;
			m_copyLinkButton.IsEnabled = (externalContentEntry != null && ExternalContentManager.IsEntryTypeDownloadSupported(externalContentEntry.Type));
			if (m_changeProviderButton.IsClicked)
			{
				DialogsManager.ShowDialog(null, new SelectExternalContentProviderDialog("Select Content Source", listingSupportRequired: true, delegate(IExternalContentProvider provider)
				{
					m_externalContentProvider = provider;
					m_listDirty = true;
					SetPath(null);
				}));
			}
			if (m_upDirectoryButton.IsClicked)
			{
				string directoryName = Storage.GetDirectoryName(m_path);
				SetPath(directoryName);
			}
			if (m_actionButton.IsClicked && externalContentEntry != null)
			{
				if (externalContentEntry.Type == ExternalContentType.Directory)
				{
					SetPath(externalContentEntry.Path);
				}
				else
				{
					DownloadEntry(externalContentEntry);
				}
			}
			if (m_copyLinkButton.IsClicked && externalContentEntry != null && ExternalContentManager.IsEntryTypeDownloadSupported(externalContentEntry.Type))
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog("Creating Link", autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				m_externalContentProvider.Link(externalContentEntry.Path, busyDialog.Progress, delegate(string link)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new ExternalContentLinkDialog(link));
				}, delegate(Exception error)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
				});
			}
			if (m_loginLogoutButton.IsClicked)
			{
				if (m_externalContentProvider.IsLoggedIn)
				{
					m_externalContentProvider.Logout();
					SetPath(null);
					m_listDirty = true;
				}
				else
				{
					ExternalContentManager.ShowLoginUiIfNeeded(m_externalContentProvider, showWarningDialog: false, delegate
					{
						SetPath(null);
						m_listDirty = true;
					});
				}
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen("Content");
			}
		}

		public void SetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				path = "/";
			}
			path = path.Replace("\\", "/");
			if (path != m_path)
			{
				m_path = path;
				m_listDirty = true;
			}
		}

		public void UpdateList()
		{
			m_directoryList.ClearItems();
			if (m_externalContentProvider != null && m_externalContentProvider.IsLoggedIn)
			{
				CancellableBusyDialog busyDialog = new CancellableBusyDialog("Retrieving Directory", autoHideOnCancel: false);
				DialogsManager.ShowDialog(null, busyDialog);
				m_externalContentProvider.List(m_path, busyDialog.Progress, delegate(ExternalContentEntry entry)
				{
					DialogsManager.HideDialog(busyDialog);
					List<ExternalContentEntry> list = new List<ExternalContentEntry>(entry.ChildEntries.Where((ExternalContentEntry e) => EntryFilter(e)).Take(1000));
					m_directoryList.ClearItems();
					list.Sort(delegate(ExternalContentEntry e1, ExternalContentEntry e2)
					{
						if (e1.Type == ExternalContentType.Directory && e2.Type != ExternalContentType.Directory)
						{
							return -1;
						}
						return (e1.Type != ExternalContentType.Directory && e2.Type == ExternalContentType.Directory) ? 1 : string.Compare(e1.Path, e2.Path);
					});
					foreach (ExternalContentEntry item in list)
					{
						m_directoryList.AddItem(item);
					}
				}, delegate(Exception error)
				{
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
				});
			}
		}

		public void DownloadEntry(ExternalContentEntry entry)
		{
			CancellableBusyDialog busyDialog = new CancellableBusyDialog("Downloading", autoHideOnCancel: false);
			DialogsManager.ShowDialog(null, busyDialog);
			m_externalContentProvider.Download(entry.Path, busyDialog.Progress, delegate(Stream stream)
			{
				busyDialog.LargeMessage = "Importing";
				ExternalContentManager.ImportExternalContent(stream, entry.Type, Storage.GetFileName(entry.Path), delegate
				{
					stream.Dispose();
					DialogsManager.HideDialog(busyDialog);
				}, delegate(Exception error)
				{
					stream.Dispose();
					DialogsManager.HideDialog(busyDialog);
					DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
				});
			}, delegate(Exception error)
			{
				DialogsManager.HideDialog(busyDialog);
				DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
			});
		}

		public static bool EntryFilter(ExternalContentEntry entry)
		{
			return entry.Type != ExternalContentType.Unknown;
		}
	}
}
