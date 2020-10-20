using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class GameMenuDialog : Dialog
	{
		public static bool m_increaseDetailDialogShown;

		public static bool m_decreaseDetailDialogShown;

		public bool m_adventureRestartExists;

		public StackPanelWidget m_statsPanel;

		public ComponentPlayer m_componentPlayer;

		public GameMenuDialog(ComponentPlayer componentPlayer)
		{
			XElement node = ContentManager.Get<XElement>("Dialogs/GameMenuDialog");
			LoadContents(this, node);
			m_statsPanel = Children.Find<StackPanelWidget>("StatsPanel");
			m_componentPlayer = componentPlayer;
			m_adventureRestartExists = WorldsManager.SnapshotExists(GameManager.WorldInfo.DirectoryName, "AdventureRestart");
			if (!m_increaseDetailDialogShown && PerformanceManager.LongTermAverageFrameTime.HasValue && PerformanceManager.LongTermAverageFrameTime.Value * 1000f < 25f && (SettingsManager.VisibilityRange <= 64 || SettingsManager.ResolutionMode == ResolutionMode.Low))
			{
				m_increaseDetailDialogShown = true;
				DialogsManager.ShowDialog(base.ParentWidget, new MessageDialog("Your device is fast", "Consider increasing visibility range or resolution for better graphics. To do so, go to performance settings.", "OK", null, null));
				AnalyticsManager.LogEvent("[GameMenuScreen] IncreaseDetailDialog Shown");
			}
			if (!m_decreaseDetailDialogShown && PerformanceManager.LongTermAverageFrameTime.HasValue && PerformanceManager.LongTermAverageFrameTime.Value * 1000f > 50f && (SettingsManager.VisibilityRange >= 64 || SettingsManager.ResolutionMode == ResolutionMode.High))
			{
				m_decreaseDetailDialogShown = true;
				DialogsManager.ShowDialog(base.ParentWidget, new MessageDialog("Your device is not fast enough", "Consider decreasing visibility range or resolution. To do so, go to performance settings.", "OK", null, null));
				AnalyticsManager.LogEvent("[GameMenuScreen] DecreaseDetailDialog Shown");
			}
			m_statsPanel.Children.Clear();
			Project project = componentPlayer.Project;
			PlayerData playerData = componentPlayer.PlayerData;
			PlayerStats playerStats = componentPlayer.PlayerStats;
			SubsystemGameInfo subsystemGameInfo = project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			SubsystemFurnitureBlockBehavior subsystemFurnitureBlockBehavior = project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
			BitmapFont font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			BitmapFont font2 = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			Color white = Color.White;
			StackPanelWidget stackPanelWidget = new StackPanelWidget
			{
				Direction = LayoutDirection.Vertical,
				HorizontalAlignment = WidgetAlignment.Center
			};
			m_statsPanel.Children.Add(stackPanelWidget);
			stackPanelWidget.Children.Add(new LabelWidget
			{
				Text = "Game Statistics",
				Font = font,
				HorizontalAlignment = WidgetAlignment.Center,
				Margin = new Vector2(0f, 10f),
				Color = white
			});
			AddStat(stackPanelWidget, "Game Mode", subsystemGameInfo.WorldSettings.GameMode.ToString() + ", " + subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode.ToString());
			AddStat(stackPanelWidget, "Terrain Type", StringsManager.GetString("TerrainGenerationMode." + subsystemGameInfo.WorldSettings.TerrainGenerationMode.ToString() + ".Name"));
			string seed = subsystemGameInfo.WorldSettings.Seed;
			AddStat(stackPanelWidget, "World Seed", (!string.IsNullOrEmpty(seed)) ? seed : "(none)");
			AddStat(stackPanelWidget, "Sea Level", WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.SeaLevelOffset));
			AddStat(stackPanelWidget, "Temperature", WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.TemperatureOffset));
			AddStat(stackPanelWidget, "Humidity", WorldOptionsScreen.FormatOffset(subsystemGameInfo.WorldSettings.HumidityOffset));
			AddStat(stackPanelWidget, "Biome Size", subsystemGameInfo.WorldSettings.BiomeSize.ToString() + "x");
			int num = 0;
			for (int i = 0; i < 1024; i++)
			{
				if (subsystemFurnitureBlockBehavior.GetDesign(i) != null)
				{
					num++;
				}
			}
			AddStat(stackPanelWidget, "Furniture Designs In Use", $"{num}/{1024}");
			AddStat(stackPanelWidget, "World Created In Version", string.IsNullOrEmpty(subsystemGameInfo.WorldSettings.OriginalSerializationVersion) ? "before 1.22" : subsystemGameInfo.WorldSettings.OriginalSerializationVersion);
			stackPanelWidget.Children.Add(new LabelWidget
			{
				Text = "Player Statistics",
				Font = font,
				HorizontalAlignment = WidgetAlignment.Center,
				Margin = new Vector2(0f, 10f),
				Color = white
			});
			AddStat(stackPanelWidget, "Name", playerData.Name);
			AddStat(stackPanelWidget, "Sex", playerData.PlayerClass.ToString());
			string value = (playerData.FirstSpawnTime >= 0.0) ? (((subsystemGameInfo.TotalElapsedGameTime - playerData.FirstSpawnTime) / 1200.0).ToString("N1") + " days ago") : "Never spawned yet";
			AddStat(stackPanelWidget, "First Spawned", value);
			string value2 = (playerData.LastSpawnTime >= 0.0) ? (((subsystemGameInfo.TotalElapsedGameTime - playerData.LastSpawnTime) / 1200.0).ToString("N1") + " days") : "Never spawned yet";
			AddStat(stackPanelWidget, "Stayed Alive", value2);
			AddStat(stackPanelWidget, "Respawned", MathUtils.Max(playerData.SpawnsCount - 1, 0).ToString("N0") + " times");
			AddStat(stackPanelWidget, "Highest Level Attained", "Level " + ((int)MathUtils.Floor(playerStats.HighestLevel)).ToString("N0"));
			if (componentPlayer != null)
			{
				Vector3 position = componentPlayer.ComponentBody.Position;
				if (subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
				{
					AddStat(stackPanelWidget, "Location", $"{position.X:0}, {position.Z:0} at altitude {position.Y:0}");
				}
				else
				{
					AddStat(stackPanelWidget, "Location", "(unavailable in " + subsystemGameInfo.WorldSettings.GameMode.ToString() + ")");
				}
			}
			if (string.CompareOrdinal(subsystemGameInfo.WorldSettings.OriginalSerializationVersion, "1.29") > 0)
			{
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "Combat Statistics",
					Font = font,
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 10f),
					Color = white
				});
				AddStat(stackPanelWidget, "Players Killed", playerStats.PlayerKills.ToString("N0"));
				AddStat(stackPanelWidget, "Land Creatures Killed", playerStats.LandCreatureKills.ToString("N0"));
				AddStat(stackPanelWidget, "Water Creatures Killed", playerStats.WaterCreatureKills.ToString("N0"));
				AddStat(stackPanelWidget, "Air Creatures Killed", playerStats.AirCreatureKills.ToString("N0"));
				AddStat(stackPanelWidget, "Melee Attacks", playerStats.MeleeAttacks.ToString("N0"));
				AddStat(stackPanelWidget, "Melee Hits", playerStats.MeleeHits.ToString("N0"), $"({((playerStats.MeleeHits == 0L) ? 0.0 : ((double)playerStats.MeleeHits / (double)playerStats.MeleeAttacks * 100.0)):0}%)");
				AddStat(stackPanelWidget, "Ranged Attacks", playerStats.RangedAttacks.ToString("N0"));
				AddStat(stackPanelWidget, "Ranged Hits", playerStats.RangedHits.ToString("N0"), $"({((playerStats.RangedHits == 0L) ? 0.0 : ((double)playerStats.RangedHits / (double)playerStats.RangedAttacks * 100.0)):0}%)");
				AddStat(stackPanelWidget, "Hits Received", playerStats.HitsReceived.ToString("N0"));
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "Work Statistics",
					Font = font,
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 10f),
					Color = white
				});
				AddStat(stackPanelWidget, "Blocks Dug", playerStats.BlocksDug.ToString("N0"));
				AddStat(stackPanelWidget, "Blocks Placed", playerStats.BlocksPlaced.ToString("N0"));
				AddStat(stackPanelWidget, "Blocks Interacted With", playerStats.BlocksInteracted.ToString("N0"));
				AddStat(stackPanelWidget, "Items Crafted/Smelted", playerStats.ItemsCrafted.ToString("N0"));
				AddStat(stackPanelWidget, "Furniture Made", playerStats.FurnitureItemsMade.ToString("N0"));
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "Movement Statistics",
					Font = font,
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 10f),
					Color = white
				});
				AddStat(stackPanelWidget, "Total Distance Travelled", FormatDistance(playerStats.DistanceTravelled));
				AddStat(stackPanelWidget, "Distance Walked", FormatDistance(playerStats.DistanceWalked), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceWalked / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Distance Fallen", FormatDistance(playerStats.DistanceFallen), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceFallen / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Distance Climbed", FormatDistance(playerStats.DistanceClimbed), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceClimbed / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Distance Flown", FormatDistance(playerStats.DistanceFlown), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceFlown / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Distance Swum", FormatDistance(playerStats.DistanceSwam), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceSwam / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Distance Ridden", FormatDistance(playerStats.DistanceRidden), $"({((playerStats.DistanceTravelled > 0.0) ? (playerStats.DistanceRidden / playerStats.DistanceTravelled * 100.0) : 0.0):0.0}%)");
				AddStat(stackPanelWidget, "Lowest Altitude", FormatDistance(playerStats.LowestAltitude));
				AddStat(stackPanelWidget, "Highest Altitude", FormatDistance(playerStats.HighestAltitude));
				AddStat(stackPanelWidget, "Deepest Dive", playerStats.DeepestDive.ToString("N1") + "m");
				AddStat(stackPanelWidget, "Jumps", playerStats.Jumps.ToString("N0"));
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "Body Statistics",
					Font = font,
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 10f),
					Color = white
				});
				AddStat(stackPanelWidget, "Total Health Lost", (playerStats.TotalHealthLost * 100.0).ToString("N0") + "%");
				AddStat(stackPanelWidget, "Food Eaten", playerStats.FoodItemsEaten.ToString("N0") + " items");
				AddStat(stackPanelWidget, "Went To Sleep", playerStats.TimesWentToSleep.ToString("N0") + " times");
				AddStat(stackPanelWidget, "Total Time Slept", (playerStats.TimeSlept / 1200.0).ToString("N1") + " days");
				AddStat(stackPanelWidget, "Became Sick", playerStats.TimesWasSick.ToString("N0") + " times");
				AddStat(stackPanelWidget, "Vomited", playerStats.TimesPuked.ToString("N0") + " times");
				AddStat(stackPanelWidget, "Contracted Flu", playerStats.TimesHadFlu.ToString("N0") + " times");
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "Other Statistics",
					Font = font,
					HorizontalAlignment = WidgetAlignment.Center,
					Margin = new Vector2(0f, 10f),
					Color = white
				});
				AddStat(stackPanelWidget, "Struck By Lightning", playerStats.StruckByLightning.ToString("N0") + " times");
				GameMode easiestModeUsed = playerStats.EasiestModeUsed;
				AddStat(stackPanelWidget, "Easiest Game Mode Used", easiestModeUsed.ToString());
				if (playerStats.DeathRecords.Count > 0)
				{
					stackPanelWidget.Children.Add(new LabelWidget
					{
						Text = "Death Record",
						Font = font,
						HorizontalAlignment = WidgetAlignment.Center,
						Margin = new Vector2(0f, 10f),
						Color = white
					});
					foreach (PlayerStats.DeathRecord deathRecord in playerStats.DeathRecords)
					{
						float num2 = (float)MathUtils.Remainder(deathRecord.Day, 1.0);
						string arg = (!(num2 < 0.2f) && !(num2 >= 0.8f)) ? ((!(num2 >= 0.7f)) ? ((!(num2 >= 0.5f)) ? "Morning of" : "Afternoon of") : "Evening of") : "Night of";
						AddStat(stackPanelWidget, string.Format("{1} day {0:0}", MathUtils.Floor(deathRecord.Day) + 1.0, arg), "", deathRecord.Cause);
					}
				}
			}
			else
			{
				stackPanelWidget.Children.Add(new LabelWidget
				{
					Text = "No player statistics available because world was started in an old version of the game.",
					WordWrap = true,
					Font = font2,
					HorizontalAlignment = WidgetAlignment.Center,
					TextAnchor = TextAnchor.HorizontalCenter,
					Margin = new Vector2(20f, 10f),
					Color = white
				});
			}
		}

		public override void Update()
		{
			if (Children.Find<ButtonWidget>("More").IsClicked)
			{
				List<Tuple<string, Action>> list = new List<Tuple<string, Action>>();
				if (m_adventureRestartExists && GameManager.WorldInfo.WorldSettings.GameMode == GameMode.Adventure)
				{
					list.Add(new Tuple<string, Action>("Restart Adventure", delegate
					{
						DialogsManager.ShowDialog(base.ParentWidget, new MessageDialog("Reset Adventure?", "The adventure will start from the beginning.", "Yes", "No", delegate(MessageDialogButton result)
						{
							if (result == MessageDialogButton.Button1)
							{
								ScreensManager.SwitchScreen("GameLoading", GameManager.WorldInfo, "AdventureRestart");
							}
						}));
					}));
				}
				if (GetRateableItems().FirstOrDefault() != null && UserManager.ActiveUser != null)
				{
					list.Add(new Tuple<string, Action>("Rate", delegate
					{
						DialogsManager.ShowDialog(base.ParentWidget, new ListSelectionDialog("Select Content To Rate", GetRateableItems(), 60f, (object o) => ((ActiveExternalContentInfo)o).DisplayName, delegate(object o)
						{
							ActiveExternalContentInfo activeExternalContentInfo = (ActiveExternalContentInfo)o;
							DialogsManager.ShowDialog(base.ParentWidget, new RateCommunityContentDialog(activeExternalContentInfo.Address, activeExternalContentInfo.DisplayName, UserManager.ActiveUser.UniqueId));
						}));
					}));
				}
				list.Add(new Tuple<string, Action>("Edit Players", delegate
				{
					ScreensManager.SwitchScreen("Players", m_componentPlayer.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true));
				}));
				list.Add(new Tuple<string, Action>("Settings", delegate
				{
					ScreensManager.SwitchScreen("Settings");
				}));
				list.Add(new Tuple<string, Action>("Help", delegate
				{
					ScreensManager.SwitchScreen("Help");
				}));
				if ((base.Input.Devices & (WidgetInputDevice.Keyboard | WidgetInputDevice.Mouse)) != 0)
				{
					list.Add(new Tuple<string, Action>("Keyboard Controls", delegate
					{
						DialogsManager.ShowDialog(base.ParentWidget, new KeyboardHelpDialog());
					}));
				}
				if ((base.Input.Devices & WidgetInputDevice.Gamepads) != 0)
				{
					list.Add(new Tuple<string, Action>("Gamepad Controls", delegate
					{
						DialogsManager.ShowDialog(base.ParentWidget, new GamepadHelpDialog());
					}));
				}
				ListSelectionDialog dialog = new ListSelectionDialog("More Actions", list, 60f, (object t) => ((Tuple<string, Action>)t).Item1, delegate(object t)
				{
					((Tuple<string, Action>)t).Item2();
				});
				DialogsManager.ShowDialog(base.ParentWidget, dialog);
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("Resume").IsClicked)
			{
				DialogsManager.HideDialog(this);
			}
			if (Children.Find<ButtonWidget>("Quit").IsClicked)
			{
				DialogsManager.HideDialog(this);
				GameManager.SaveProject(waitForCompletion: true, showErrorDialog: true);
				GameManager.DisposeProject();
				ScreensManager.SwitchScreen("MainMenu");
			}
		}

		public IEnumerable<ActiveExternalContentInfo> GetRateableItems()
		{
			if (GameManager.Project != null && UserManager.ActiveUser != null)
			{
				SubsystemGameInfo subsystemGameInfo = GameManager.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
				foreach (ActiveExternalContentInfo item in subsystemGameInfo.GetActiveExternalContent())
				{
					if (!CommunityContentManager.IsContentRated(item.Address, UserManager.ActiveUser.UniqueId))
					{
						yield return item;
					}
				}
			}
		}

		public static string FormatDistance(double value)
		{
			if (value < 1000.0)
			{
				return $"{value:0}m";
			}
			return $"{value / 1000.0:N2}km";
		}

		public void AddStat(ContainerWidget containerWidget, string title, string value1, string value2 = "")
		{
			BitmapFont font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			Color white = Color.White;
			Color gray = Color.Gray;
			containerWidget.Children.Add(new UniformSpacingPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Center,
				Children = 
				{
					(Widget)new LabelWidget
					{
						Text = title + ":",
						HorizontalAlignment = WidgetAlignment.Far,
						Font = font,
						Color = gray,
						Margin = new Vector2(5f, 1f)
					},
					(Widget)new StackPanelWidget
					{
						Direction = LayoutDirection.Horizontal,
						HorizontalAlignment = WidgetAlignment.Near,
						Children = 
						{
							(Widget)new LabelWidget
							{
								Text = value1,
								Font = font,
								Color = white,
								Margin = new Vector2(5f, 1f)
							},
							(Widget)new LabelWidget
							{
								Text = value2,
								Font = font,
								Color = gray,
								Margin = new Vector2(5f, 1f)
							}
						}
					}
				}
			});
		}
	}
}
