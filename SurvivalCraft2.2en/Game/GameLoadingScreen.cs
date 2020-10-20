using System;
using System.Xml.Linq;

namespace Game
{
	public class GameLoadingScreen : Screen
	{
		public WorldInfo m_worldInfo;

		public string m_worldSnapshotName;

		public StateMachine m_stateMachine = new StateMachine();

		public GameLoadingScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/GameLoadingScreen");
			LoadContents(this, node);
			m_stateMachine.AddState("WaitingForFadeIn", null, delegate
			{
				if (!ScreensManager.IsAnimating)
				{
					if (string.IsNullOrEmpty(m_worldSnapshotName))
					{
						m_stateMachine.TransitionTo("Loading");
					}
					else
					{
						m_stateMachine.TransitionTo("RestoringSnapshot");
					}
				}
			}, null);
			m_stateMachine.AddState("Loading", null, delegate
			{
				ContainerWidget gamesWidget = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
				GameManager.LoadProject(m_worldInfo, gamesWidget);
				ScreensManager.SwitchScreen("Game");
			}, null);
			m_stateMachine.AddState("RestoringSnapshot", null, delegate
			{
				GameManager.DisposeProject();
				WorldsManager.RestoreWorldFromSnapshot(m_worldInfo.DirectoryName, m_worldSnapshotName);
				m_stateMachine.TransitionTo("Loading");
			}, null);
		}

		public override void Update()
		{
			try
			{
				m_stateMachine.Update();
			}
			catch (Exception e)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
				DialogsManager.ShowDialog(null, new MessageDialog("Error loading world", ExceptionManager.MakeFullErrorMessage(e), "OK", null, null));
			}
		}

		public override void Enter(object[] parameters)
		{
			m_worldInfo = (WorldInfo)parameters[0];
			m_worldSnapshotName = (string)parameters[1];
			m_stateMachine.TransitionTo("WaitingForFadeIn");
			ProgressManager.UpdateProgress("Loading World", 0f);
		}
	}
}
