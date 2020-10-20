using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemTime : Subsystem
	{
		public struct DelayedExecutionRequest
		{
			public double GameTime;

			public Action Action;
		}

		public const float MaxGameTimeDelta = 0.1f;

		public double m_gameTime;

		public float m_gameTimeDelta;

		public float m_prevGameTimeDelta;

		public float m_gameTimeFactor = 1f;

		public List<DelayedExecutionRequest> m_delayedExecutionsRequests = new List<DelayedExecutionRequest>();

		public SubsystemPlayers m_subsystemPlayers;

		public SubsystemUpdate m_subsystemUpdate;

		public double GameTime => m_gameTime;

		public float GameTimeDelta => m_gameTimeDelta;

		public float PreviousGameTimeDelta => m_prevGameTimeDelta;

		public float GameTimeFactor
		{
			get
			{
				return m_gameTimeFactor;
			}
			set
			{
				m_gameTimeFactor = MathUtils.Clamp(value, 0f, 256f);
			}
		}

		public float? FixedTimeStep
		{
			get;
			set;
		}

		public void NextFrame()
		{
			m_prevGameTimeDelta = m_gameTimeDelta;
			if (!FixedTimeStep.HasValue)
			{
				m_gameTimeDelta = MathUtils.Min(Time.FrameDuration * m_gameTimeFactor, 0.1f);
			}
			else
			{
				m_gameTimeDelta = MathUtils.Min(FixedTimeStep.Value * m_gameTimeFactor, 0.1f);
			}
			m_gameTime += m_gameTimeDelta;
			int num = 0;
			while (num < m_delayedExecutionsRequests.Count)
			{
				DelayedExecutionRequest delayedExecutionRequest = m_delayedExecutionsRequests[num];
				if (delayedExecutionRequest.GameTime >= 0.0 && GameTime >= delayedExecutionRequest.GameTime)
				{
					m_delayedExecutionsRequests.RemoveAt(num);
					delayedExecutionRequest.Action();
				}
				else
				{
					num++;
				}
			}
			int num2 = 0;
			int num3 = 0;
			foreach (ComponentPlayer componentPlayer in m_subsystemPlayers.ComponentPlayers)
			{
				if (componentPlayer.ComponentHealth.Health == 0f)
				{
					num3++;
				}
				else if (componentPlayer.ComponentSleep.SleepFactor == 1f)
				{
					num2++;
				}
			}
			if (num2 + num3 == m_subsystemPlayers.ComponentPlayers.Count && num2 >= 1)
			{
				FixedTimeStep = 0.05f;
				m_subsystemUpdate.UpdatesPerFrame = 20;
			}
			else
			{
				FixedTimeStep = null;
				m_subsystemUpdate.UpdatesPerFrame = 1;
			}
			bool flag = true;
			foreach (ComponentPlayer componentPlayer2 in m_subsystemPlayers.ComponentPlayers)
			{
				if (!componentPlayer2.ComponentGui.IsGameMenuDialogVisible())
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				GameTimeFactor = 0f;
			}
			else if (GameTimeFactor == 0f)
			{
				GameTimeFactor = 1f;
			}
		}

		public void QueueGameTimeDelayedExecution(double gameTime, Action action)
		{
			m_delayedExecutionsRequests.Add(new DelayedExecutionRequest
			{
				GameTime = gameTime,
				Action = action
			});
		}

		public bool PeriodicGameTimeEvent(double period, double offset)
		{
			double num = GameTime - offset;
			double num2 = MathUtils.Floor(num / period) * period;
			if (num >= num2)
			{
				return num - (double)GameTimeDelta < num2;
			}
			return false;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemPlayers = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true);
			m_subsystemUpdate = base.Project.FindSubsystem<SubsystemUpdate>(throwOnError: true);
		}
	}
}
