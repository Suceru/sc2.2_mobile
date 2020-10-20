using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemGameWidgets : Subsystem, IUpdateable
	{
		public const int MaxGameWidgets = 4;

		public SubsystemPlayers m_subsystemPlayers;

		public List<GameWidget> m_gameWidgets = new List<GameWidget>();

		public GamesWidget GamesWidget
		{
			get;
			set;
		}

		public ReadOnlyList<GameWidget> GameWidgets => new ReadOnlyList<GameWidget>(m_gameWidgets);

		public SubsystemTerrain SubsystemTerrain
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Views;

		public float CalculateSquaredDistanceFromNearestView(Vector3 p)
		{
			float num = 3.40282347E+38f;
			foreach (GameWidget gameWidget in m_gameWidgets)
			{
				float num2 = Vector3.DistanceSquared(p, gameWidget.ActiveCamera.ViewPosition);
				if (num2 < num)
				{
					num = num2;
				}
			}
			return num;
		}

		public float CalculateDistanceFromNearestView(Vector3 p)
		{
			return MathUtils.Sqrt(CalculateSquaredDistanceFromNearestView(p));
		}

		public void Update(float dt)
		{
			foreach (GameWidget gameWidget in GameWidgets)
			{
				gameWidget.ActiveCamera.Update(Time.FrameDuration);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemPlayers = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			SubsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemPlayers.PlayerAdded += delegate(PlayerData playerData)
			{
				AddGameWidgetForPlayer(playerData);
			};
			m_subsystemPlayers.PlayerRemoved += delegate(PlayerData playerData)
			{
				if (playerData.GameWidget != null)
				{
					RemoveGameWidget(playerData.GameWidget);
				}
			};
			GamesWidget = valuesDictionary.GetValue<GamesWidget>("GamesWidget");
			foreach (PlayerData playersDatum in m_subsystemPlayers.PlayersData)
			{
				AddGameWidgetForPlayer(playersDatum);
			}
		}

		public override void Dispose()
		{
			GameWidget[] array = GameWidgets.ToArray();
			foreach (GameWidget gameWidget in array)
			{
				RemoveGameWidget(gameWidget);
				gameWidget.Dispose();
			}
		}

		public void AddGameWidgetForPlayer(PlayerData playerData)
		{
			int index = 0;
			while (index < 4 && m_gameWidgets.FirstOrDefault((GameWidget v) => v.GameWidgetIndex == index) != null)
			{
				int num = ++index;
			}
			if (index >= 4)
			{
				throw new InvalidOperationException("Too many GameWidgets.");
			}
			GameWidget gameWidget = new GameWidget(playerData, index);
			m_gameWidgets.Add(gameWidget);
			GamesWidget.Children.Add(gameWidget);
		}

		public void RemoveGameWidget(GameWidget gameWidget)
		{
			m_gameWidgets.Remove(gameWidget);
			GamesWidget.Children.Remove(gameWidget);
		}
	}
}
