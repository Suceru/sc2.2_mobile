using Engine;
using SimpleJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Game
{
	public class DropboxExternalContentProvider : IExternalContentProvider, IDisposable
	{
		public class LoginProcessData
		{
			public bool IsTokenFlow;

			public Action Success;

			public Action<Exception> Failure;

			public CancellableProgress Progress;

			public void Succeed(DropboxExternalContentProvider provider)
			{
				provider.m_loginProcessData = null;
				Success?.Invoke();
			}

			public void Fail(DropboxExternalContentProvider provider, Exception error)
			{
				provider.m_loginProcessData = null;
				Failure?.Invoke(error);
			}
		}

		public const string m_appKey = "1unnzwkb8igx70k";

		public const string m_appSecret = "3i5u3j3141php7u";

		public const string m_redirectUri = "com.candyrufusgames.survivalcraft2://redirect";

		public LoginProcessData m_loginProcessData;

		public string DisplayName => "Dropbox";

		public string Description
		{
			get
			{
				if (!IsLoggedIn)
				{
					return "Not logged in";
				}
				return "Logged in";
			}
		}

		public bool SupportsListing => true;

		public bool SupportsLinks => true;

		public bool RequiresLogin => true;

		public bool IsLoggedIn => !string.IsNullOrEmpty(SettingsManager.DropboxAccessToken);

		public DropboxExternalContentProvider()
		{
			Program.HandleUri += HandleUri;
			Window.Activated += WindowActivated;
		}

		public void Dispose()
		{
			Program.HandleUri -= HandleUri;
			Window.Activated -= WindowActivated;
		}

		public void Login(CancellableProgress progress, Action success, Action<Exception> failure)
		{
			try
			{
				if (m_loginProcessData != null)
				{
					throw new InvalidOperationException("Login already in progress.");
				}
				if (!WebManager.IsInternetConnectionAvailable())
				{
					throw new InvalidOperationException("Internet connection is unavailable.");
				}
				Logout();
				progress.Cancelled += delegate
				{
					if (m_loginProcessData != null)
					{
						LoginProcessData loginProcessData = m_loginProcessData;
						m_loginProcessData = null;
						loginProcessData.Fail(this, null);
					}
				};
				m_loginProcessData = new LoginProcessData();
				m_loginProcessData.Progress = progress;
				m_loginProcessData.Success = success;
				m_loginProcessData.Failure = failure;
				LoginLaunchBrowser();
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Logout()
		{
			SettingsManager.DropboxAccessToken = string.Empty;
		}

		public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Authorization", "Bearer " + SettingsManager.DropboxAccessToken);
				dictionary.Add("Content-Type", "application/json");
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("path", NormalizePath(path));
				jsonObject.Add("recursive", false);
				jsonObject.Add("include_media_info", false);
				jsonObject.Add("include_deleted", false);
				jsonObject.Add("include_has_explicit_shared_members", false);
				MemoryStream data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToString()));
				WebManager.Post("https://api.dropboxapi.com/2/files/list_folder", null, dictionary, data, progress, delegate(byte[] result)
				{
					try
					{
						JsonObject jsonObject2 = (JsonObject)WebManager.JsonFromBytes(result);
						success(JsonObjectToEntry(jsonObject2));
					}
					catch (Exception obj2)
					{
						failure(obj2);
					}
				}, delegate(Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("path", NormalizePath(path));
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Authorization", "Bearer " + SettingsManager.DropboxAccessToken);
				dictionary.Add("Dropbox-API-Arg", jsonObject.ToString());
				WebManager.Get("https://content.dropboxapi.com/2/files/download", null, dictionary, progress, delegate(byte[] result)
				{
					success(new MemoryStream(result));
				}, delegate(Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("path", NormalizePath(path));
				jsonObject.Add("mode", "add");
				jsonObject.Add("autorename", true);
				jsonObject.Add("mute", false);
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Authorization", "Bearer " + SettingsManager.DropboxAccessToken);
				dictionary.Add("Content-Type", "application/octet-stream");
				dictionary.Add("Dropbox-API-Arg", jsonObject.ToString());
				WebManager.Post("https://content.dropboxapi.com/2/files/upload", null, dictionary, stream, progress, delegate
				{
					success(null);
				}, delegate(Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			try
			{
				VerifyLoggedIn();
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Authorization", "Bearer " + SettingsManager.DropboxAccessToken);
				dictionary.Add("Content-Type", "application/json");
				JsonObject jsonObject = new JsonObject();
				jsonObject.Add("path", NormalizePath(path));
				jsonObject.Add("short_url", false);
				MemoryStream data = new MemoryStream(Encoding.UTF8.GetBytes(jsonObject.ToString()));
				WebManager.Post("https://api.dropboxapi.com/2/sharing/create_shared_link", null, dictionary, data, progress, delegate(byte[] result)
				{
					try
					{
						JsonObject jsonObject2 = (JsonObject)WebManager.JsonFromBytes(result);
						success(JsonObjectToLinkAddress(jsonObject2));
					}
					catch (Exception obj2)
					{
						failure(obj2);
					}
				}, delegate(Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void LoginLaunchBrowser()
		{
			try
			{
				m_loginProcessData.IsTokenFlow = true;
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("response_type", "token");
				dictionary.Add("client_id", "1unnzwkb8igx70k");
				dictionary.Add("redirect_uri", "com.candyrufusgames.survivalcraft2://redirect");
				WebBrowserManager.LaunchBrowser("https://www.dropbox.com/oauth2/authorize?" + WebManager.UrlParametersToString(dictionary));
			}
			catch (Exception error)
			{
				m_loginProcessData.Fail(this, error);
			}
		}

		public void WindowActivated()
		{
			if (m_loginProcessData != null && !m_loginProcessData.IsTokenFlow)
			{
				LoginProcessData loginProcessData = m_loginProcessData;
				m_loginProcessData = null;
				TextBoxDialog dialog = new TextBoxDialog("Enter Dropbox authorization code", "", 256, delegate(string s)
				{
					if (s != null)
					{
						try
						{
							WebManager.Post("https://api.dropboxapi.com/oauth2/token", new Dictionary<string, string>
							{
								{
									"code",
									s.Trim()
								},
								{
									"client_id",
									"1unnzwkb8igx70k"
								},
								{
									"client_secret",
									"3i5u3j3141php7u"
								},
								{
									"grant_type",
									"authorization_code"
								}
							}, null, new MemoryStream(), loginProcessData.Progress, delegate(byte[] result)
							{
								SettingsManager.DropboxAccessToken = ((IDictionary<string, object>)WebManager.JsonFromBytes(result))["access_token"].ToString();
								loginProcessData.Succeed(this);
							}, delegate(Exception error)
							{
								loginProcessData.Fail(this, error);
							});
						}
						catch (Exception error2)
						{
							loginProcessData.Fail(this, error2);
						}
					}
					else
					{
						loginProcessData.Fail(this, null);
					}
				});
				DialogsManager.ShowDialog(null, dialog);
			}
		}

		public void HandleUri(Uri uri)
		{
			if (m_loginProcessData == null)
			{
				m_loginProcessData = new LoginProcessData();
				m_loginProcessData.IsTokenFlow = true;
			}
			LoginProcessData loginProcessData = m_loginProcessData;
			m_loginProcessData = null;
			if (loginProcessData.IsTokenFlow)
			{
				try
				{
					if (!(uri != null) || string.IsNullOrEmpty(uri.Fragment))
					{
						goto IL_00ae;
					}
					Dictionary<string, string> dictionary = WebManager.UrlParametersFromString(uri.Fragment.TrimStart('#'));
					if (!dictionary.ContainsKey("access_token"))
					{
						if (dictionary.ContainsKey("error"))
						{
							throw new Exception(dictionary["error"]);
						}
						goto IL_00ae;
					}
					SettingsManager.DropboxAccessToken = dictionary["access_token"];
					loginProcessData.Succeed(this);
					goto end_IL_0038;
					IL_00ae:
					throw new Exception("Could not retrieve Dropbox access token.");
					end_IL_0038:;
				}
				catch (Exception error)
				{
					loginProcessData.Fail(this, error);
				}
			}
		}

		public void VerifyLoggedIn()
		{
			if (!IsLoggedIn)
			{
				throw new InvalidOperationException("Not logged in to Dropbox in this app.");
			}
		}

		internal static ExternalContentEntry JsonObjectToEntry(JsonObject jsonObject)
		{
			ExternalContentEntry externalContentEntry = new ExternalContentEntry();
			if (jsonObject.ContainsKey("entries"))
			{
				foreach (JsonObject item in (JsonArray)jsonObject["entries"])
				{
					ExternalContentEntry externalContentEntry2 = new ExternalContentEntry();
					externalContentEntry2.Path = item["path_display"].ToString();
					externalContentEntry2.Type = ((item[".tag"].ToString() == "folder") ? ExternalContentType.Directory : ExternalContentManager.ExtensionToType(Storage.GetExtension(externalContentEntry2.Path)));
					if (externalContentEntry2.Type != ExternalContentType.Directory)
					{
						externalContentEntry2.Time = (item.ContainsKey("server_modified") ? DateTime.Parse(item["server_modified"].ToString(), CultureInfo.InvariantCulture) : new DateTime(2000, 1, 1));
						externalContentEntry2.Size = (item.ContainsKey("size") ? ((long)item["size"]) : 0);
					}
					externalContentEntry.ChildEntries.Add(externalContentEntry2);
				}
				return externalContentEntry;
			}
			return externalContentEntry;
		}

		internal static string JsonObjectToLinkAddress(JsonObject jsonObject)
		{
			if (jsonObject.ContainsKey("url"))
			{
				return jsonObject["url"].ToString().Replace("www.dropbox.", "dl.dropbox.").Replace("?dl=0", "") + "?dl=1";
			}
			throw new InvalidOperationException("Share information not found.");
		}

		public static string NormalizePath(string path)
		{
			if (path == "/")
			{
				return string.Empty;
			}
			if (path.Length > 0 && path[0] != '/')
			{
				return "/" + path;
			}
			return path;
		}
	}
}
