using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentPilot : Component, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemTerrain m_subsystemTerrain;

		public ComponentCreature m_componentCreature;

		public Random m_random = new Random();

		public Vector2? m_walkOrder;

		public Vector3? m_flyOrder;

		public Vector3? m_swimOrder;

		public Vector2 m_turnOrder;

		public float m_jumpOrder;

		public double m_nextUpdateTime;

		public double m_lastStuckCheckTime;

		public int m_stuckCount;

		public double? m_aboveBelowTime;

		public Vector3? m_lastStuckCheckPosition;

		public DynamicArray<ComponentBody> m_nearbyBodies = new DynamicArray<ComponentBody>();

		public double m_nextBodiesUpdateTime;

		public static bool DrawPilotDestination;

		public Vector3? Destination
		{
			get;
			set;
		}

		public float Speed
		{
			get;
			set;
		}

		public float Range
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

		public bool TakeRisks
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

		public void SetDestination(Vector3? destination, float speed, float range, bool ignoreHeightDifference, bool raycastDestination, bool takeRisks, ComponentBody doNotAvoidBody)
		{
			bool flag = true;
			if (Destination.HasValue && destination.HasValue)
			{
				Vector3 v = Vector3.Normalize(Destination.Value - m_componentCreature.ComponentBody.Position);
				if (Vector3.Dot(Vector3.Normalize(destination.Value - m_componentCreature.ComponentBody.Position), v) > 0.5f)
				{
					flag = false;
				}
			}
			if (flag)
			{
				IsStuck = false;
				m_lastStuckCheckPosition = null;
				m_aboveBelowTime = null;
			}
			Destination = destination;
			Speed = speed;
			Range = range;
			IgnoreHeightDifference = ignoreHeightDifference;
			RaycastDestination = raycastDestination;
			TakeRisks = takeRisks;
			DoNotAvoidBody = doNotAvoidBody;
		}

		public void Stop()
		{
			SetDestination(null, 0f, 0f, ignoreHeightDifference: false, raycastDestination: false, takeRisks: false, null);
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.GameTime >= m_nextUpdateTime)
			{
				m_nextUpdateTime = m_subsystemTime.GameTime + (double)m_random.Float(0.09f, 0.11f);
				m_walkOrder = null;
				m_flyOrder = null;
				m_swimOrder = null;
				m_turnOrder = Vector2.Zero;
				m_jumpOrder = 0f;
				if (Destination.HasValue)
				{
					Vector3 position = m_componentCreature.ComponentBody.Position;
					Vector3 forward = m_componentCreature.ComponentBody.Matrix.Forward;
					Vector3 v = AvoidNearestBody(position, Destination.Value);
					Vector3 vector = v - position;
					float num = vector.LengthSquared();
					Vector2 vector2 = new Vector2(v.X, v.Z) - new Vector2(position.X, position.Z);
					float num2 = vector2.LengthSquared();
					float x = Vector2.Angle(forward.XZ, vector.XZ);
					float num3 = ((m_componentCreature.ComponentBody.CollisionVelocityChange * new Vector3(1f, 0f, 1f)).LengthSquared() > 0f && m_componentCreature.ComponentBody.StandingOnValue.HasValue) ? 0.15f : 0.4f;
					if (m_subsystemTime.GameTime >= m_lastStuckCheckTime + (double)num3 || !m_lastStuckCheckPosition.HasValue)
					{
						m_lastStuckCheckTime = m_subsystemTime.GameTime;
						if (MathUtils.Abs(x) > MathUtils.DegToRad(20f) || !m_lastStuckCheckPosition.HasValue || Vector3.Dot(position - m_lastStuckCheckPosition.Value, Vector3.Normalize(vector)) > 0.2f)
						{
							m_lastStuckCheckPosition = position;
							m_stuckCount = 0;
						}
						else
						{
							m_stuckCount++;
						}
						IsStuck = (m_stuckCount >= 4);
					}
					if (m_componentCreature.ComponentLocomotion.FlySpeed > 0f && (num > 9f || vector.Y > 0.5f || vector.Y < -1.5f || (!m_componentCreature.ComponentBody.StandingOnValue.HasValue && m_componentCreature.ComponentBody.ImmersionFactor == 0f)) && m_componentCreature.ComponentBody.ImmersionFactor < 1f)
					{
						float y = MathUtils.Min(0.08f * vector2.LengthSquared(), 12f);
						Vector3 v2 = v + new Vector3(0f, y, 0f);
						Vector3 value2 = Speed * Vector3.Normalize(v2 - position);
						value2.Y = MathUtils.Max(value2.Y, -0.5f);
						m_flyOrder = value2;
						m_turnOrder = new Vector2(MathUtils.Clamp(x, -1f, 1f), 0f);
					}
					else if (m_componentCreature.ComponentLocomotion.SwimSpeed > 0f && m_componentCreature.ComponentBody.ImmersionFactor > 0.5f)
					{
						Vector3 value3 = Speed * Vector3.Normalize(v - position);
						value3.Y = MathUtils.Clamp(value3.Y, -0.5f, 0.5f);
						m_swimOrder = value3;
						m_turnOrder = new Vector2(MathUtils.Clamp(x, -1f, 1f), 0f);
					}
					else if (m_componentCreature.ComponentLocomotion.WalkSpeed > 0f)
					{
						if (IsTerrainSafeToGo(position, vector))
						{
							m_turnOrder = new Vector2(MathUtils.Clamp(x, -1f, 1f), 0f);
							if (num2 > 1f)
							{
								m_walkOrder = new Vector2(0f, MathUtils.Lerp(Speed, 0f, MathUtils.Saturate((MathUtils.Abs(x) - 0.33f) / 0.66f)));
								if (Speed >= 1f && m_componentCreature.ComponentLocomotion.InAirWalkFactor >= 1f && num > 1f && m_random.Float(0f, 1f) < 0.05f)
								{
									m_jumpOrder = 1f;
								}
							}
							else
							{
								float x2 = Speed * MathUtils.Min(1f * MathUtils.Sqrt(num2), 1f);
								m_walkOrder = new Vector2(0f, MathUtils.Lerp(x2, 0f, MathUtils.Saturate(2f * MathUtils.Abs(x))));
							}
						}
						else
						{
							IsStuck = true;
						}
						if (num2 < 1f && vector.Y < -0.1f)
						{
							m_componentCreature.ComponentBody.IsSmoothRiseEnabled = false;
						}
						else
						{
							m_componentCreature.ComponentBody.IsSmoothRiseEnabled = true;
						}
						if (num2 < 1f && (vector.Y < -0.5f || vector.Y > 1f))
						{
							if (vector.Y > 0f && m_random.Float(0f, 1f) < 0.05f)
							{
								m_jumpOrder = 1f;
							}
							if (!m_aboveBelowTime.HasValue)
							{
								m_aboveBelowTime = m_subsystemTime.GameTime;
							}
							else if (m_subsystemTime.GameTime - m_aboveBelowTime.Value > 2.0 && m_componentCreature.ComponentBody.StandingOnValue.HasValue)
							{
								IsStuck = true;
							}
						}
						else
						{
							m_aboveBelowTime = null;
						}
					}
					bool flag = false;
					if ((!IgnoreHeightDifference) ? (num <= Range * Range) : (num2 <= Range * Range))
					{
						if (RaycastDestination)
						{
							if (!m_subsystemTerrain.Raycast(position + new Vector3(0f, 0.5f, 0f), v + new Vector3(0f, 0.5f, 0f), useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable).HasValue)
							{
								Destination = null;
							}
						}
						else
						{
							Destination = null;
						}
					}
				}
				if (!Destination.HasValue && m_componentCreature.ComponentLocomotion.FlySpeed > 0f && !m_componentCreature.ComponentBody.StandingOnValue.HasValue && m_componentCreature.ComponentBody.ImmersionFactor == 0f)
				{
					m_turnOrder = Vector2.Zero;
					m_walkOrder = null;
					m_swimOrder = null;
					m_flyOrder = new Vector3(0f, -0.5f, 0f);
				}
			}
			m_componentCreature.ComponentLocomotion.WalkOrder = CombineNullables(m_componentCreature.ComponentLocomotion.WalkOrder, m_walkOrder);
			m_componentCreature.ComponentLocomotion.SwimOrder = CombineNullables(m_componentCreature.ComponentLocomotion.SwimOrder, m_swimOrder);
			m_componentCreature.ComponentLocomotion.TurnOrder += m_turnOrder;
			m_componentCreature.ComponentLocomotion.FlyOrder = CombineNullables(m_componentCreature.ComponentLocomotion.FlyOrder, m_flyOrder);
			m_componentCreature.ComponentLocomotion.JumpOrder = MathUtils.Max(m_jumpOrder, m_componentCreature.ComponentLocomotion.JumpOrder);
			m_jumpOrder = 0f;
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
		}

		public bool IsTerrainSafeToGo(Vector3 position, Vector3 direction)
		{
			Vector3 vector = position + new Vector3(0f, 0.1f, 0f) + ((direction.LengthSquared() < 1.2f) ? new Vector3(direction.X, 0f, direction.Z) : (1.2f * Vector3.Normalize(new Vector3(direction.X, 0f, direction.Z))));
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					if (!(Vector3.Dot(direction, new Vector3(i, 0f, j)) > 0f))
					{
						continue;
					}
					for (int num = 0; num >= -2; num--)
					{
						int cellValue = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(vector.X) + i, Terrain.ToCell(vector.Y) + num, Terrain.ToCell(vector.Z) + j);
						Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
						if (block.ShouldAvoid(cellValue))
						{
							return false;
						}
						if (block.IsCollidable)
						{
							break;
						}
					}
				}
			}
			Vector3 vector2 = position + new Vector3(0f, 0.1f, 0f) + ((direction.LengthSquared() < 1f) ? new Vector3(direction.X, 0f, direction.Z) : (1f * Vector3.Normalize(new Vector3(direction.X, 0f, direction.Z))));
			bool flag = true;
			int num2 = TakeRisks ? 7 : 5;
			for (int num3 = 0; num3 >= -num2; num3--)
			{
				int cellValue2 = m_subsystemTerrain.Terrain.GetCellValue(Terrain.ToCell(vector2.X), Terrain.ToCell(vector2.Y) + num3, Terrain.ToCell(vector2.Z));
				Block block2 = BlocksManager.Blocks[Terrain.ExtractContents(cellValue2)];
				if ((block2.IsCollidable || block2.BlockIndex == 18) && !block2.ShouldAvoid(cellValue2))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return false;
			}
			return true;
		}

		public ComponentBody FindNearestBodyInFront(Vector3 position, Vector2 direction)
		{
			if (m_subsystemTime.GameTime >= m_nextBodiesUpdateTime)
			{
				m_nextBodiesUpdateTime = m_subsystemTime.GameTime + 0.5;
				m_nearbyBodies.Clear();
				m_subsystemBodies.FindBodiesAroundPoint(m_componentCreature.ComponentBody.Position.XZ, 4f, m_nearbyBodies);
			}
			ComponentBody result = null;
			float num = 3.40282347E+38f;
			foreach (ComponentBody nearbyBody in m_nearbyBodies)
			{
				if (nearbyBody != m_componentCreature.ComponentBody && !(MathUtils.Abs(nearbyBody.Position.Y - m_componentCreature.ComponentBody.Position.Y) > 1.1f) && Vector2.Dot(nearbyBody.Position.XZ - position.XZ, direction) > 0f)
				{
					float num2 = Vector2.DistanceSquared(nearbyBody.Position.XZ, position.XZ);
					if (num2 < num)
					{
						num = num2;
						result = nearbyBody;
					}
				}
			}
			return result;
		}

		public Vector3 AvoidNearestBody(Vector3 position, Vector3 destination)
		{
			Vector2 v = destination.XZ - position.XZ;
			ComponentBody componentBody = FindNearestBodyInFront(position, Vector2.Normalize(v));
			if (componentBody != null && componentBody != DoNotAvoidBody)
			{
				float num = 0.72f * (componentBody.BoxSize.X + m_componentCreature.ComponentBody.BoxSize.X) + 0.5f;
				Vector2 xZ = componentBody.Position.XZ;
				Vector2 v2 = Segment2.NearestPoint(new Segment2(position.XZ, destination.XZ), xZ) - xZ;
				if (v2.LengthSquared() < num * num)
				{
					float num2 = v.Length();
					Vector2 v3 = Vector2.Normalize(xZ + Vector2.Normalize(v2) * num - position.XZ);
					if (Vector2.Dot(v / num2, v3) > 0.5f)
					{
						return new Vector3(position.X + v3.X * num2, destination.Y, position.Z + v3.Y * num2);
					}
				}
			}
			return destination;
		}

		public static Vector2? CombineNullables(Vector2? v1, Vector2? v2)
		{
			if (!v1.HasValue)
			{
				return v2;
			}
			if (!v2.HasValue)
			{
				return v1;
			}
			return v1.Value + v2.Value;
		}

		public static Vector3? CombineNullables(Vector3? v1, Vector3? v2)
		{
			if (!v1.HasValue)
			{
				return v2;
			}
			if (!v2.HasValue)
			{
				return v1;
			}
			return v1.Value + v2.Value;
		}
	}
}
