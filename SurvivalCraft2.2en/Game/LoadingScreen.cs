using Engine;
using Engine.Content;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Game
{
	public class LoadingScreen : Screen
	{
		public List<Action> m_loadActions = new List<Action>();

		public int m_index;

		public bool m_loadingStarted;

		public bool m_loadingFinished;

		public bool m_pauseLoading;

		public bool m_loadingErrorsSuppressed;

		public LoadingScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/LoadingScreen");
			LoadContents(this, node);
			AddLoadAction(delegate
			{
				VrManager.Initialize();
			});
			AddLoadAction(delegate
			{
				CommunityContentManager.Initialize();
			});
			AddLoadAction(delegate
			{
				MotdManager.Initialize();
			});
			AddLoadAction(delegate
			{
				LightingManager.Initialize();
			});
			AddLoadAction(delegate
			{
				StringsManager.LoadStrings();
			});
			AddLoadAction(delegate
			{
				TextureAtlasManager.LoadAtlases();
			});
			foreach (ContentInfo item in ContentManager.List())
			{
				ContentInfo localContentInfo = item;
				AddLoadAction(delegate
				{
					ContentManager.Get(localContentInfo.Name);
				});
			}
			AddLoadAction(delegate
			{
				DatabaseManager.Initialize();
			});
			AddLoadAction(delegate
			{
				WorldsManager.Initialize();
			});
			AddLoadAction(delegate
			{
				BlocksTexturesManager.Initialize();
			});
			AddLoadAction(delegate
			{
				CharacterSkinsManager.Initialize();
			});
			AddLoadAction(delegate
			{
				FurniturePacksManager.Initialize();
			});
			AddLoadAction(delegate
			{
				BlocksManager.Initialize();
			});
			AddLoadAction(delegate
			{
				CraftingRecipesManager.Initialize();
			});
			AddLoadAction(delegate
			{
				MusicManager.CurrentMix = MusicManager.Mix.Menu;
			});
		}

		public void AddLoadAction(Action action)
		{
			m_loadActions.Add(action);
		}

		public override void Leave()
		{
			ContentManager.Dispose("Textures/Gui/CandyRufusLogo");
			ContentManager.Dispose("Textures/Gui/EngineLogo");
		}

		public override void Update()
		{
			if (!m_loadingStarted)
			{
				m_loadingStarted = true;
			}
			else
			{
				if (m_loadingFinished)
				{
					return;
				}
				double realTime = Time.RealTime;
				while (!m_pauseLoading && m_index < m_loadActions.Count)
				{
					try
					{
						m_loadActions[m_index++]();
					}
					catch (Exception ex)
					{
						Log.Error("Loading error. Reason: " + ex.Message);
						if (!m_loadingErrorsSuppressed)
						{
							m_pauseLoading = true;
							DialogsManager.ShowDialog(ScreensManager.RootWidget, new MessageDialog("Loading Error", ExceptionManager.MakeFullErrorMessage(ex), "OK", "Suppress", delegate(MessageDialogButton b)
							{
								switch (b)
								{
								case MessageDialogButton.Button1:
									m_pauseLoading = false;
									break;
								case MessageDialogButton.Button2:
									m_loadingErrorsSuppressed = true;
									break;
								}
							}));
						}
					}
					if (Time.RealTime - realTime > 0.1)
					{
						break;
					}
				}
				if (m_index >= m_loadActions.Count)
				{
					m_loadingFinished = true;
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
					ScreensManager.SwitchScreen("MainMenu");
				}
			}
		}
	}
}
