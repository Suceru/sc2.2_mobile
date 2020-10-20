using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentRunAwayBehavior : ComponentBehavior, IUpdateable, INoiseListener
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemTime m_subsystemTime;

		public SubsystemNoise m_subsystemNoise;

		public ComponentCreature m_componentCreature;

		public ComponentPathfinding m_componentPathfinding;

		public ComponentHerdBehavior m_componentHerdBehavior;

		public Random m_random = new Random();

		public StateMachine m_stateMachine = new StateMachine();

		public float m_importanceLevel;

		public ComponentFrame m_attacker;

		public float m_timeToForgetAttacker;

		public bool m_heardNoise;

		public Vector3? m_lastNoiseSourcePosition;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override float ImportanceLevel => m_importanceLevel;

		public void RunAwayFrom(ComponentBody componentBody)
		{
			m_attacker = componentBody;
			m_timeToForgetAttacker = m_random.Float(10f, 20f);
		}

		public void Update(float dt)
		{
			m_stateMachine.Update();
			m_heardNoise = false;
		}

		public void HearNoise(ComponentBody sourceBody, Vector3 sourcePosition, float loudness)
		{
			if (loudness >= 1f)
			{
				m_heardNoise = true;
				m_lastNoiseSourcePosition = sourcePosition;
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemNoise = base.Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_componentPathfinding = base.Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
			m_componentHerdBehavior = base.Entity.FindComponent<ComponentHerdBehavior>();
			m_componentCreature.ComponentHealth.Attacked += delegate(ComponentCreature attacker)
			{
				RunAwayFrom(attacker.ComponentBody);
			};
			m_stateMachine.AddState("Inactive", delegate
			{
				m_importanceLevel = 0f;
				m_lastNoiseSourcePosition = null;
			}, delegate
			{
				if (m_attacker != null)
				{
					m_timeToForgetAttacker -= m_subsystemTime.GameTimeDelta;
					if (m_timeToForgetAttacker <= 0f)
					{
						m_attacker = null;
					}
				}
				if (m_componentCreature.ComponentHealth.HealthChange < 0f || (m_attacker != null && Vector3.DistanceSquared(m_attacker.Position, m_componentCreature.ComponentBody.Position) < 36f))
				{
					m_importanceLevel = MathUtils.Max(m_importanceLevel, (m_componentCreature.ComponentHealth.Health < 0.33f) ? 300 : 100);
				}
				else if (m_heardNoise)
				{
					m_importanceLevel = MathUtils.Max(m_importanceLevel, 5f);
				}
				else if (!IsActive)
				{
					m_importanceLevel = 0f;
				}
				if (IsActive)
				{
					m_stateMachine.TransitionTo("RunningAway");
				}
			}, null);
			m_stateMachine.AddState("RunningAway", delegate
			{
				Vector3 value = FindSafePlace();
				m_componentPathfinding.SetDestination(value, 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
				m_componentCreature.ComponentCreatureSounds.PlayPainSound();
				m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 6f);
			}, delegate
			{
				if (!IsActive)
				{
					m_stateMachine.TransitionTo("Inactive");
				}
				else if (!m_componentPathfinding.Destination.HasValue || m_componentPathfinding.IsStuck)
				{
					m_importanceLevel = 0f;
				}
				else if (m_attacker != null)
				{
					if (!m_attacker.IsAddedToProject)
					{
						m_importanceLevel = 0f;
						m_attacker = null;
					}
					else
					{
						ComponentHealth componentHealth = m_attacker.Entity.FindComponent<ComponentHealth>();
						if (componentHealth != null && componentHealth.Health == 0f)
						{
							m_importanceLevel = 0f;
							m_attacker = null;
						}
					}
				}
			}, null);
			m_stateMachine.TransitionTo("Inactive");
		}

		public Vector3 FindSafePlace()
		{
			Vector3 position = m_componentCreature.ComponentBody.Position;
			Vector3? herdPosition = (m_componentHerdBehavior != null) ? m_componentHerdBehavior.FindHerdCenter() : null;
			if (herdPosition.HasValue && Vector3.DistanceSquared(position, herdPosition.Value) < 144f)
			{
				herdPosition = null;
			}
			float num = -1f / 0f;
			Vector3 result = position;
			for (int i = 0; i < 30; i++)
			{
				int num2 = Terrain.ToCell(position.X + m_random.Float(-25f, 25f));
				int num3 = Terrain.ToCell(position.Z + m_random.Float(-25f, 25f));
				for (int num4 = 255; num4 >= 0; num4--)
				{
					int cellContents = m_subsystemTerrain.Terrain.GetCellContents(num2, num4, num3);
					if (BlocksManager.Blocks[cellContents].IsCollidable || cellContents == 18)
					{
						Vector3 vector = new Vector3((float)num2 + 0.5f, (float)num4 + 1.1f, (float)num3 + 0.5f);
						float num5 = ScoreSafePlace(position, vector, herdPosition, m_lastNoiseSourcePosition, cellContents);
						if (num5 > num)
						{
							num = num5;
							result = vector;
						}
						break;
					}
				}
			}
			return result;
		}

		public float ScoreSafePlace(Vector3 currentPosition, Vector3 safePosition, Vector3? herdPosition, Vector3? noiseSourcePosition, int contents)
		{
			float num = 0f;
			Vector2 vector = new Vector2(currentPosition.X, currentPosition.Z);
			Vector2 vector2 = new Vector2(safePosition.X, safePosition.Z);
			Segment2 s = new Segment2(vector, vector2);
			if (m_attacker != null)
			{
				Vector3 position = m_attacker.Position;
				Vector2 vector3 = new Vector2(position.X, position.Z);
				float num2 = Vector2.Distance(vector3, vector2);
				float num3 = Segment2.Distance(s, vector3);
				num += num2 + 3f * num3;
			}
			else
			{
				num += 2f * Vector2.Distance(vector, vector2);
			}
			Vector2? vector4 = herdPosition.HasValue ? new Vector2?(new Vector2(herdPosition.Value.X, herdPosition.Value.Z)) : null;
			float num4 = vector4.HasValue ? Segment2.Distance(s, vector4.Value) : 0f;
			num -= num4;
			Vector2? vector5 = noiseSourcePosition.HasValue ? new Vector2?(new Vector2(noiseSourcePosition.Value.X, noiseSourcePosition.Value.Z)) : null;
			float num5 = vector5.HasValue ? Segment2.Distance(s, vector5.Value) : 0f;
			num += 1.5f * num5;
			if (contents == 18)
			{
				num -= 4f;
			}
			return num;
		}
	}
}
