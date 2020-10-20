using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentSwimAwayBehavior : ComponentBehavior, IUpdateable
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemTime m_subsystemTime;

		public ComponentCreature m_componentCreature;

		public ComponentPathfinding m_componentPathfinding;

		public ComponentHerdBehavior m_componentHerdBehavior;

		public StateMachine m_stateMachine = new StateMachine();

		public Random m_random = new Random();

		public float m_importanceLevel;

		public ComponentFrame m_attacker;

		public float m_timeToForgetAttacker;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override float ImportanceLevel => m_importanceLevel;

		public void SwimAwayFrom(ComponentBody attacker)
		{
			m_attacker = attacker;
			m_timeToForgetAttacker = m_random.Float(10f, 20f);
		}

		public void Update(float dt)
		{
			m_stateMachine.Update();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_componentPathfinding = base.Entity.FindComponent<ComponentPathfinding>(throwOnError: true);
			m_componentHerdBehavior = base.Entity.FindComponent<ComponentHerdBehavior>();
			m_componentCreature.ComponentHealth.Attacked += delegate(ComponentCreature attacker)
			{
				SwimAwayFrom(attacker.ComponentBody);
			};
			m_stateMachine.AddState("Inactive", delegate
			{
				m_importanceLevel = 0f;
				m_attacker = null;
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
				if (m_componentCreature.ComponentHealth.HealthChange < 0f)
				{
					m_importanceLevel = ((m_componentCreature.ComponentHealth.Health < 0.33f) ? 300 : 100);
				}
				else if (m_attacker != null && Vector3.DistanceSquared(m_attacker.Position, m_componentCreature.ComponentBody.Position) < 25f)
				{
					m_importanceLevel = 100f;
				}
				if (IsActive)
				{
					m_stateMachine.TransitionTo("SwimmingAway");
				}
			}, null);
			m_stateMachine.AddState("SwimmingAway", delegate
			{
				m_componentPathfinding.SetDestination(FindSafePlace(), 1f, 1f, 0, useRandomMovements: false, ignoreHeightDifference: true, raycastDestination: false, null);
			}, delegate
			{
				if (!IsActive || !m_componentPathfinding.Destination.HasValue || m_componentPathfinding.IsStuck)
				{
					m_stateMachine.TransitionTo("Inactive");
				}
			}, null);
			m_stateMachine.TransitionTo("Inactive");
		}

		public Vector3 FindSafePlace()
		{
			Vector3 vector = 0.5f * (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max);
			Vector3? herdPosition = (m_componentHerdBehavior != null) ? m_componentHerdBehavior.FindHerdCenter() : null;
			float num = -1f / 0f;
			Vector3 result = vector;
			for (int i = 0; i < 40; i++)
			{
				Vector2 vector2 = m_random.Vector2(1f, 1f);
				float y = 0.4f * m_random.Float(-1f, 1f);
				Vector3 v = Vector3.Normalize(new Vector3(vector2.X, y, vector2.Y));
				Vector3 vector3 = vector + m_random.Float(10f, 20f) * v;
				TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(vector, vector3, useInteractionBoxes: false, skipAirBlocks: false, delegate(int value, float d)
				{
					int num3 = Terrain.ExtractContents(value);
					return !(BlocksManager.Blocks[num3] is WaterBlock);
				});
				Vector3 vector4 = terrainRaycastResult.HasValue ? (vector + v * terrainRaycastResult.Value.Distance) : vector3;
				float num2 = ScoreSafePlace(vector, vector4, herdPosition);
				if (num2 > num)
				{
					num = num2;
					result = vector4;
				}
			}
			return result;
		}

		public float ScoreSafePlace(Vector3 currentPosition, Vector3 safePosition, Vector3? herdPosition)
		{
			Vector2 vector = new Vector2(currentPosition.X, currentPosition.Z);
			Vector2 vector2 = new Vector2(safePosition.X, safePosition.Z);
			Vector2? vector3 = herdPosition.HasValue ? new Vector2?(new Vector2(herdPosition.Value.X, herdPosition.Value.Z)) : null;
			Segment2 s = new Segment2(vector, vector2);
			float num = vector3.HasValue ? Segment2.Distance(s, vector3.Value) : 0f;
			if (m_attacker != null)
			{
				Vector3 position = m_attacker.Position;
				Vector2 vector4 = new Vector2(position.X, position.Z);
				float num2 = Vector2.Distance(vector4, vector2);
				float num3 = Segment2.Distance(s, vector4);
				return num2 + 1.5f * num3 - num;
			}
			return 1.5f * Vector2.Distance(vector, vector2) - num;
		}
	}
}
