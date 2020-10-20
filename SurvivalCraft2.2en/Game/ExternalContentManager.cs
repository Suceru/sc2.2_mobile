using Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Game
{
	public static class ExternalContentManager
	{
		public static List<IExternalContentProvider> m_providers;

		public static IExternalContentProvider DefaultProvider
		{
			get
			{
				if (Providers.Count <= 0)
				{
					return null;
				}
				return Providers[0];
			}
		}

		public static ReadOnlyList<IExternalContentProvider> Providers => new ReadOnlyList<IExternalContentProvider>(m_providers);

		public static void Initialize()
		{
			m_providers = new List<IExternalContentProvider>();
			m_providers.Add(new DropboxExternalContentProvider());
			m_providers.Add(new TransferShExternalContentProvider());
			m_providers.Add(new AndroidSdCardExternalContentProvider());
		}

		public static ExternalContentType ExtensionToType(string extension)
		{
			extension = extension.ToLower();
			foreach (ExternalContentType value in Enum.GetValues(typeof(ExternalContentType)))
			{
				if (GetEntryTypeExtensions(value).FirstOrDefault((string e) => e == extension) != null)
				{
					return value;
				}
			}
			return ExternalContentType.Unknown;
		}

		public static IEnumerable<string> GetEntryTypeExtensions(ExternalContentType type)
		{
			switch (type)
			{
			case ExternalContentType.World:
				yield return ".scworld";
				break;
			case ExternalContentType.BlocksTexture:
				yield return ".scbtex";
				yield return ".png";
				break;
			case ExternalContentType.CharacterSkin:
				yield return ".scskin";
				break;
			case ExternalContentType.FurniturePack:
				yield return ".scfpack";
				break;
			}
		}

		public static Subtexture GetEntryTypeIcon(ExternalContentType type)
		{
			switch (type)
			{
			case ExternalContentType.Directory:
				return ContentManager.Get<Subtexture>("Textures/Atlas/FolderIcon");
			case ExternalContentType.World:
				return ContentManager.Get<Subtexture>("Textures/Atlas/WorldIcon");
			case ExternalContentType.BlocksTexture:
				return ContentManager.Get<Subtexture>("Textures/Atlas/TexturePackIcon");
			case ExternalContentType.CharacterSkin:
				return ContentManager.Get<Subtexture>("Textures/Atlas/CharacterSkinIcon");
			case ExternalContentType.FurniturePack:
				return ContentManager.Get<Subtexture>("Textures/Atlas/FurnitureIcon");
			default:
				return ContentManager.Get<Subtexture>("Textures/Atlas/QuestionMarkIcon");
			}
		}

		public static string GetEntryTypeDescription(ExternalContentType type)
		{
			switch (type)
			{
			case ExternalContentType.Directory:
				return "Directory";
			case ExternalContentType.World:
				return "World";
			case ExternalContentType.BlocksTexture:
				return "Blocks Texture";
			case ExternalContentType.CharacterSkin:
				return "Character Skin";
			case ExternalContentType.FurniturePack:
				return "Furniture Pack";
			default:
				return string.Empty;
			}
		}

		public static bool IsEntryTypeDownloadSupported(ExternalContentType type)
		{
			switch (type)
			{
			case ExternalContentType.World:
				return true;
			case ExternalContentType.BlocksTexture:
				return true;
			case ExternalContentType.CharacterSkin:
				return true;
			case ExternalContentType.FurniturePack:
				return true;
			default:
				return false;
			}
		}

		public static bool DoesEntryTypeRequireName(ExternalContentType type)
		{
			switch (type)
			{
			case ExternalContentType.BlocksTexture:
				return true;
			case ExternalContentType.CharacterSkin:
				return true;
			case ExternalContentType.FurniturePack:
				return true;
			default:
				return false;
			}
		}

		public static Exception VerifyExternalContentName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return new InvalidOperationException("Name empty.");
			}
			if (name.Length > 50)
			{
				return new InvalidOperationException("Name too long. Maximum 50 characters allowed.");
			}
			if (name[0] == ' ' || name[name.Length - 1] == ' ')
			{
				return new InvalidOperationException("Name cannot start or end with a space.");
			}
			foreach (char c in name)
			{
				if ((c < 'A' || c > 'Z') && (c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '-' && c != '.' && c != '_' && c != ' ')
				{
					return new InvalidOperationException("Name contains invalid characters. Only latin letters, digits, dashes, dots, underscores and spaces are allowed.");
				}
			}
			return null;
		}

		public static void DeleteExternalContent(ExternalContentType type, string name)
		{
			switch (type)
			{
			case ExternalContentType.World:
				WorldsManager.DeleteWorld(name);
				break;
			case ExternalContentType.BlocksTexture:
				BlocksTexturesManager.DeleteBlocksTexture(name);
				break;
			case ExternalContentType.CharacterSkin:
				CharacterSkinsManager.DeleteCharacterSkin(name);
				break;
			case ExternalContentType.FurniturePack:
				FurniturePacksManager.DeleteFurniturePack(name);
				break;
			default:
				throw new InvalidOperationException("Unsupported external content type.");
			}
		}

		public static void ImportExternalContent(Stream stream, ExternalContentType type, string name, Action<string> success, Action<Exception> failure)
		{
			Task.Run(delegate
			{
				try
				{
					success(ImportExternalContentSync(stream, type, name));
				}
				catch (Exception obj)
				{
					failure(obj);
				}
			});
		}

		public static string ImportExternalContentSync(Stream stream, ExternalContentType type, string name)
		{
			switch (type)
			{
			case ExternalContentType.World:
				return WorldsManager.ImportWorld(stream);
			case ExternalContentType.BlocksTexture:
				return BlocksTexturesManager.ImportBlocksTexture(name, stream);
			case ExternalContentType.CharacterSkin:
				return CharacterSkinsManager.ImportCharacterSkin(name, stream);
			case ExternalContentType.FurniturePack:
				return FurniturePacksManager.ImportFurniturePack(name, stream);
			default:
				throw new InvalidOperationException("Unsupported external content type.");
			}
		}

		public static void ShowLoginUiIfNeeded(IExternalContentProvider provider, bool showWarningDialog, Action handler)
		{
			if (provider.RequiresLogin && !provider.IsLoggedIn)
			{
				Action loginAction = delegate
				{
					CancellableBusyDialog busyDialog = new CancellableBusyDialog("Logging in", autoHideOnCancel: true);
					DialogsManager.ShowDialog(null, busyDialog);
					provider.Login(busyDialog.Progress, delegate
					{
						DialogsManager.HideDialog(busyDialog);
						handler?.Invoke();
					}, delegate(Exception error)
					{
						DialogsManager.HideDialog(busyDialog);
						if (error != null)
						{
							DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
						}
					});
				};
				if (showWarningDialog)
				{
					DialogsManager.ShowDialog(null, new MessageDialog("Login Required", $"Do you want to login to {provider.DisplayName}?", "Login", "Cancel", delegate(MessageDialogButton b)
					{
						if (b == MessageDialogButton.Button1)
						{
							loginAction();
						}
					}));
				}
				else
				{
					loginAction();
				}
			}
			else
			{
				handler?.Invoke();
			}
		}

		public static void ShowUploadUi(ExternalContentType type, string name)
		{
			DialogsManager.ShowDialog(null, new SelectExternalContentProviderDialog("Select Upload Destination", listingSupportRequired: false, delegate(IExternalContentProvider provider)
			{
				try
				{
					if (provider != null)
					{
						ShowLoginUiIfNeeded(provider, showWarningDialog: true, delegate
						{
							CancellableBusyDialog busyDialog = new CancellableBusyDialog("Uploading", autoHideOnCancel: false);
							DialogsManager.ShowDialog(null, busyDialog);
							Task.Run(delegate
							{
								bool needsDelete = false;
								string sourcePath = null;
								Stream stream = null;
								Action cleanup = delegate
								{
									Utilities.Dispose(ref stream);
									if (needsDelete && sourcePath != null)
									{
										try
										{
											Storage.DeleteFile(sourcePath);
										}
										catch
										{
										}
									}
								};
								try
								{
									string path;
									if (type == ExternalContentType.BlocksTexture)
									{
										sourcePath = BlocksTexturesManager.GetFileName(name);
										if (sourcePath == null)
										{
											throw new InvalidOperationException("Cannot upload this item.");
										}
										path = Storage.GetFileName(sourcePath);
									}
									else if (type == ExternalContentType.CharacterSkin)
									{
										sourcePath = CharacterSkinsManager.GetFileName(name);
										if (sourcePath == null)
										{
											throw new InvalidOperationException("Cannot upload this item.");
										}
										path = Storage.GetFileName(sourcePath);
									}
									else if (type == ExternalContentType.FurniturePack)
									{
										sourcePath = FurniturePacksManager.GetFileName(name);
										if (sourcePath == null)
										{
											throw new InvalidOperationException("Cannot upload this item.");
										}
										path = Storage.GetFileName(sourcePath);
									}
									else
									{
										if (type != ExternalContentType.World)
										{
											throw new InvalidOperationException("Unsupported content type.");
										}
										busyDialog.LargeMessage = "Compressing world";
										sourcePath = "data:/WorldUpload.tmp";
										needsDelete = true;
										string name2 = WorldsManager.GetWorldInfo(name).WorldSettings.Name;
										path = $"{name2}.scworld";
										using (Stream targetStream = Storage.OpenFile(sourcePath, OpenFileMode.Create))
										{
											WorldsManager.ExportWorld(name, targetStream);
										}
									}
									busyDialog.LargeMessage = "Uploading";
									stream = Storage.OpenFile(sourcePath, OpenFileMode.Read);
									provider.Upload(path, stream, busyDialog.Progress, delegate(string link)
									{
										long length = stream.Length;
										cleanup();
										DialogsManager.HideDialog(busyDialog);
										if (string.IsNullOrEmpty(link))
										{
											DialogsManager.ShowDialog(null, new MessageDialog("Success", $"{DataSizeFormatter.Format(length)} uploaded.", "OK", null, null));
										}
										else
										{
											DialogsManager.ShowDialog(null, new ExternalContentLinkDialog(link));
										}
									}, delegate(Exception error)
									{
										cleanup();
										DialogsManager.HideDialog(busyDialog);
										DialogsManager.ShowDialog(null, new MessageDialog("Error", error.Message, "OK", null, null));
									});
								}
								catch (Exception ex2)
								{
									cleanup();
									DialogsManager.HideDialog(busyDialog);
									DialogsManager.ShowDialog(null, new MessageDialog("Error", ex2.Message, "OK", null, null));
								}
							});
						});
					}
				}
				catch (Exception ex)
				{
					DialogsManager.ShowDialog(null, new MessageDialog("Error", ex.Message, "OK", null, null));
				}
			}));
		}
	}
}
