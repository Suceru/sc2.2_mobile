using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentPathfinding : Component, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemPathfinding m_subsystemPathfinding;

		public ComponentCreature m_componentCreature;

		public ComponentPilot m_componentPilot;

		public StateMachine m_stateMachine = new StateMachine();

		public Random m_random = new Random();

		public Vector3? m_lastPathfindingDestination;

		public double? m_lastPathfindingTime;

		public float m_pathfindingCongestion;

		public PathfindingResult m_pathfindingResult = new PathfindingResult();

		public double m_nextUpdateTime;

		public int m_randomMoveCount;

		public bool m_destinationChanged;

		public const float m_minPathfindingPeriod = 8f;

		public const float m_pathfindingCongestionCapacity = 500f;

		public const float m_pathfindingCongestionCapacityLimit = 1000f;

		public const float m_pathfindingCongestionDecayRate = 20f;

		public static bool DrawPathfinding;

		public Vector3? Destination
		{
			get;
			set;
		}

		public float Range
		{
			get;
			set;
		}

		public float Speed
		{
			get;
			set;
		}

		public int MaxPathfindingPositions
		{
			get;
			set;
		}

		public bool UseRandomMovements
		{
			get;
			set;
		}

		public bool IgnoreHeightDifference
		{
			get;
			set;
		}

		public bool RaycastDestination
		{
			get;
			set;
		}

		public ComponentBody DoNotAvoidBody
		{
			get;
			set;
		}

		public bool IsStuck
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void SetDestination(Vector3? destination, float speed, float range, int maxPathfindingPositions, bool useRandomMovements, bool ignoreHeightDifference, bool raycastDestination, ComponentBody doNotAvoidBody)
		{
			Destination = destination;
			Speed = speed;
			Range = range;
			MaxPathfindingPositions = maxPathfindingPositions;
			UseRandomMovements = useRandomMovements;
			IgnoreHeightDifference = ignoreHeightDifference;
			RaycastDestination = raycastDestination;
			DoNotAvoidBody = doNotAvoidBody;
			m_destinationChanged = true;
			m_nextUpdateTime = 0.0;
		}

		public void Stop()
		{
			SetDestination(null, 0f, 0f, 0, useRandomMovements: false, ignoreHeightDifference: false, raycastDestination: false, null);
			m_componentPilot.Stop();
			IsStuck = false;
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.GameTime >= m_nextUpdateTime)
			{
				float num = m_random.Float(0.08f, 0.12f);
				m_nextUpdateTime = m_subsystemTime.GameTime + (double)num;
				m_pathfindingCongestion = MathUtils.Max(m_pathfindingCongestion - 20f * num, 0f);
				m_stateMachine.Update();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemPathfinding = base.Project.FindSubsystem<SubsystemPathfinding>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_componentPilot = base.Entity.FindComponent<ComponentPilot>(throwOnError: true);
			m_stateMachine.AddState("Stopped", delegate
			{
				Stop();
				m_randomMoveCount = 0;
			}, delegate
			{
				if (Destination.HasValue)
				{
					m_stateMachine.TransitionTo("MovingDirect");
				}
			}, null);
			m_stateMachine.AddState("MovingDirect", delegate
			{
				IsStuck = false;
				m_destinationChanged = true;
			}, delegate
			{
				if (!Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (m_destinationChanged)
				{
					m_componentPilot.SetDestination(Destination, Speed, Range, IgnoreHeightDifference, RaycastDestination, Speed >= 1f, DoNotAvoidBody);
					m_destinationChanged = false;
				}
				else if (!m_componentPilot.Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (m_componentPilot.IsStuck)
				{
					if (MaxPathfindingPositions > 0 && m_componentCreature.ComponentLocomotion.WalkSpeed > 0f)
					{
						m_stateMachine.TransitionTo("SearchingForPath");
					}
					else if (UseRandomMovements)
					{
						m_stateMachine.TransitionTo("MovingRandomly");
					}
					else
					{
						m_stateMachine.TransitionTo("Stuck");
					}
				}
			}, null);
			m_stateMachine.AddState("SearchingForPath", delegate
			{
				m_pathfindingResult.IsCompleted = false;
				m_pathfindingResult.IsInProgress = false;
			}, delegate
			{
				if (!Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (!m_pathfindingResult.IsInProgress && (!m_lastPathfindingTime.HasValue || m_subsystemTime.GameTime - m_lastPathfindingTime > 8.0) && m_pathfindingCongestion < 500f)
				{
					m_lastPathfindingDestination = Destination.Value;
					m_lastPathfindingTime = m_subsystemTime.GameTime;
					m_subsystemPathfinding.QueuePathSearch(m_componentCreature.ComponentBody.Position + new Vector3(0f, 0.01f, 0f), Destination.Value + new Vector3(0f, 0.01f, 0f), 1f, m_componentCreature.ComponentBody.BoxSize, MaxPathfindingPositions, m_pathfindingResult);
				}
				else if (UseRandomMovements)
				{
					m_stateMachine.TransitionTo("MovingRandomly");
				}
				if (m_pathfindingResult.IsCompleted)
				{
					m_pathfindingCongestion = MathUtils.Min(m_pathfindingCongestion + (float)m_pathfindingResult.PositionsChecked, 1000f);
					if (m_pathfindingResult.Path.Count > 0)
					{
						m_stateMachine.TransitionTo("MovingWithPath");
					}
					else if (UseRandomMovements)
					{
						m_stateMachine.TransitionTo("MovingRandomly");
					}
					else
					{
						m_stateMachine.TransitionTo("Stuck");
					}
				}
			}, null);
			m_stateMachine.AddState("MovingWithPath", delegate
			{
				m_componentPilot.Stop();
				m_randomMoveCount = 0;
			}, delegate
			{
				if (!Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (!m_componentPilot.Destination.HasValue)
				{
					if (m_pathfindingResult.Path.Count > 0)
					{
						Vector3 value = m_pathfindingResult.Path.Array[m_pathfindingResult.Path.Count - 1];
						m_componentPilot.SetDestination(value, MathUtils.Min(Speed, 0.75f), 0.75f, ignoreHeightDifference: false, raycastDestination: false, Speed >= 1f, DoNotAvoidBody);
						m_pathfindingResult.Path.RemoveAt(m_pathfindingResult.Path.Count - 1);
					}
					else
					{
						m_stateMachine.TransitionTo("MovingDirect");
					}
				}
				else if (m_componentPilot.IsStuck)
				{
					if (UseRandomMovements)
					{
						m_stateMachine.TransitionTo("MovingRandomly");
					}
					else
					{
						m_stateMachine.TransitionTo("Stuck");
					}
				}
				else
				{
					float num = Vector3.DistanceSquared(m_componentCreature.ComponentBody.Position, Destination.Value);
					if (Vector3.DistanceSquared(m_lastPathfindingDestination.Value, Destination.Value) > num)
					{
						m_stateMachine.TransitionTo("MovingDirect");
					}
				}
			}, null);
			m_stateMachine.AddState("MovingRandomly", delegate
			{
				m_componentPilot.SetDestination(m_componentCreature.ComponentBody.Position + new Vector3(5f * m_random.Float(-1f, 1f), 0f, 5f * m_random.Float(-1f, 1f)), 1f, 1f, ignoreHeightDifference: true, raycastDestination: false, takeRisks: false, DoNotAvoidBody);
				m_randomMoveCount++;
			}, delegate
			{
				if (!Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (m_randomMoveCount > 3)
				{
					m_stateMachine.TransitionTo("Stuck");
				}
				else if (m_componentPilot.IsStuck || !m_componentPilot.Destination.HasValue)
				{
					m_stateMachine.TransitionTo("MovingDirect");
				}
			}, null);
			m_stateMachine.AddState("Stuck", delegate
			{
				IsStuck = true;
			}, delegate
			{
				if (!Destination.HasValue)
				{
					m_stateMachine.TransitionTo("Stopped");
				}
				else if (m_destinationChanged)
				{
					m_destinationChanged = false;
					m_stateMachine.TransitionTo("MovingDirect");
				}
			}, null);
			m_stateMachine.TransitionTo("Stopped");
		}
	}
}
