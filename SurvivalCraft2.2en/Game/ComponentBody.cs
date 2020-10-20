using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBody : ComponentFrame, IUpdateable
	{
		public struct CollisionBox
		{
			public int BlockValue;

			public Vector3 BlockVelocity;

			public ComponentBody ComponentBody;

			public BoundingBox Box;
		}

		public SubsystemTime m_subsystemTime;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemMovingBlocks m_subsystemMovingBlocks;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemBlockBehaviors m_subsystemBlockBehaviors;

		public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

		public Random m_random = new Random();

		public DynamicArray<CollisionBox> m_collisionBoxes = new DynamicArray<CollisionBox>();

		public DynamicArray<ComponentBody> m_componentBodies = new DynamicArray<ComponentBody>();

		public DynamicArray<IMovingBlockSet> m_movingBlockSets = new DynamicArray<IMovingBlockSet>();

		public DynamicArray<CollisionBox> m_bodiesCollisionBoxes = new DynamicArray<CollisionBox>();

		public DynamicArray<CollisionBox> m_movingBlocksCollisionBoxes = new DynamicArray<CollisionBox>();

		public ComponentBody m_parentBody;

		public List<ComponentBody> m_childBodies = new List<ComponentBody>();

		public Vector3 m_velocity;

		public bool m_isSneaking;

		public Vector3 m_totalImpulse;

		public Vector3 m_directMove;

		public bool m_fluidEffectsPlayed;

		public float m_stoppedTime;

		public static Vector3[] m_freeSpaceOffsets;

		public static bool DrawBodiesBounds;

		public const float SleepThresholdSpeed = 1E-05f;

		public const float MaxSpeed = 25f;

		public Vector3 BoxSize
		{
			get;
			set;
		}

		public float Mass
		{
			get;
			set;
		}

		public float Density
		{
			get;
			set;
		}

		public Vector2 AirDrag
		{
			get;
			set;
		}

		public Vector2 WaterDrag
		{
			get;
			set;
		}

		public float WaterSwayAngle
		{
			get;
			set;
		}

		public float WaterTurnSpeed
		{
			get;
			set;
		}

		public float ImmersionDepth
		{
			get;
			set;
		}

		public float ImmersionFactor
		{
			get;
			set;
		}

		public FluidBlock ImmersionFluidBlock
		{
			get;
			set;
		}

		public int? StandingOnValue
		{
			get;
			set;
		}

		public ComponentBody StandingOnBody
		{
			get;
			set;
		}

		public Vector3 StandingOnVelocity
		{
			get;
			set;
		}

		public Vector3 Velocity
		{
			get
			{
				return m_velocity;
			}
			set
			{
				if (value.LengthSquared() > 625f)
				{
					m_velocity = 25f * Vector3.Normalize(value);
				}
				else
				{
					m_velocity = value;
				}
			}
		}

		public bool IsSneaking
		{
			get
			{
				return m_isSneaking;
			}
			set
			{
				if (!StandingOnValue.HasValue)
				{
					value = false;
				}
				m_isSneaking = value;
			}
		}

		public bool IsGravityEnabled
		{
			get;
			set;
		}

		public bool IsGroundDragEnabled
		{
			get;
			set;
		}

		public bool IsWaterDragEnabled
		{
			get;
			set;
		}

		public bool IsSmoothRiseEnabled
		{
			get;
			set;
		}

		public float MaxSmoothRiseHeight
		{
			get;
			set;
		}

		public Vector3 CollisionVelocityChange
		{
			get;
			set;
		}

		public BoundingBox BoundingBox
		{
			get
			{
				Vector3 boxSize = BoxSize;
				Vector3 position = base.Position;
				return new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
			}
		}

		public ReadOnlyList<ComponentBody> ChildBodies => new ReadOnlyList<ComponentBody>(m_childBodies);

		public ComponentBody ParentBody
		{
			get
			{
				return m_parentBody;
			}
			set
			{
				if (value != m_parentBody)
				{
					if (m_parentBody != null)
					{
						m_parentBody.m_childBodies.Remove(this);
					}
					m_parentBody = value;
					if (m_parentBody != null)
					{
						m_parentBody.m_childBodies.Add(this);
					}
				}
			}
		}

		public Vector3 ParentBodyPositionOffset
		{
			get;
			set;
		}

		public Quaternion ParentBodyRotationOffset
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder
		{
			get
			{
				if (m_parentBody == null)
				{
					return UpdateOrder.Body;
				}
				return m_parentBody.UpdateOrder + 1;
			}
		}

		public event Action<ComponentBody> CollidedWithBody;

		static ComponentBody()
		{
			List<Vector3> list = new List<Vector3>();
			for (int i = -2; i <= 2; i++)
			{
				for (int j = -2; j <= 2; j++)
				{
					for (int k = -2; k <= 2; k++)
					{
						Vector3 item = new Vector3(0.25f * (float)i, 0.25f * (float)j, 0.25f * (float)k);
						list.Add(item);
					}
				}
			}
			list.Sort((Vector3 o1, Vector3 o2) => Comparer<float>.Default.Compare(o1.LengthSquared(), o2.LengthSquared()));
			m_freeSpaceOffsets = list.ToArray();
		}

		public void ApplyImpulse(Vector3 impulse)
		{
			m_totalImpulse += impulse;
		}

		public void ApplyDirectMove(Vector3 directMove)
		{
			m_directMove += directMove;
		}

		public bool IsChildOfBody(ComponentBody componentBody)
		{
			if (ParentBody != componentBody)
			{
				if (ParentBody != null)
				{
					return ParentBody.IsChildOfBody(componentBody);
				}
				return false;
			}
			return true;
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemBlockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true);
			m_subsystemFluidBlockBehavior = base.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			BoxSize = valuesDictionary.GetValue<Vector3>("BoxSize");
			Mass = valuesDictionary.GetValue<float>("Mass");
			Density = valuesDictionary.GetValue<float>("Density");
			AirDrag = valuesDictionary.GetValue<Vector2>("AirDrag");
			WaterDrag = valuesDictionary.GetValue<Vector2>("WaterDrag");
			WaterSwayAngle = valuesDictionary.GetValue<float>("WaterSwayAngle");
			WaterTurnSpeed = valuesDictionary.GetValue<float>("WaterTurnSpeed");
			Velocity = valuesDictionary.GetValue<Vector3>("Velocity");
			MaxSmoothRiseHeight = valuesDictionary.GetValue<float>("MaxSmoothRiseHeight");
			ParentBody = valuesDictionary.GetValue<EntityReference>("ParentBody").GetComponent<ComponentBody>(base.Entity, idToEntityMap, throwIfNotFound: false);
			ParentBodyPositionOffset = valuesDictionary.GetValue<Vector3>("ParentBodyPositionOffset");
			ParentBodyRotationOffset = valuesDictionary.GetValue<Quaternion>("ParentBodyRotationOffset");
			IsSmoothRiseEnabled = true;
			IsGravityEnabled = true;
			IsGroundDragEnabled = true;
			IsWaterDragEnabled = true;
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			base.Save(valuesDictionary, entityToIdMap);
			if (Velocity != Vector3.Zero)
			{
				valuesDictionary.SetValue("Velocity", Velocity);
			}
			EntityReference value = EntityReference.FromId(ParentBody, entityToIdMap);
			if (!value.IsNullOrEmpty())
			{
				valuesDictionary.SetValue("ParentBody", value);
				valuesDictionary.SetValue("ParentBodyPositionOffset", ParentBodyPositionOffset);
				valuesDictionary.SetValue("ParentBodyRotationOffset", ParentBodyRotationOffset);
			}
		}

		public override void OnEntityRemoved()
		{
			ParentBody = null;
			ComponentBody[] array = ChildBodies.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].ParentBody = null;
			}
		}

		public void Update(float dt)
		{
			CollisionVelocityChange = Vector3.Zero;
			Velocity += m_totalImpulse;
			m_totalImpulse = Vector3.Zero;
			if (m_parentBody != null || m_velocity.LengthSquared() > 9.99999944E-11f || m_directMove != Vector3.Zero)
			{
				m_stoppedTime = 0f;
			}
			else
			{
				m_stoppedTime += dt;
				if (m_stoppedTime > 0.5f && !Time.PeriodicEvent(0.25, 0.0))
				{
					return;
				}
			}
			Vector3 position = base.Position;
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(Terrain.ToCell(position.X), Terrain.ToCell(position.Z));
			if (chunkAtCell == null || chunkAtCell.State <= TerrainChunkState.InvalidContents4)
			{
				Velocity = Vector3.Zero;
				return;
			}
			m_bodiesCollisionBoxes.Clear();
			FindBodiesCollisionBoxes(position, m_bodiesCollisionBoxes);
			m_movingBlocksCollisionBoxes.Clear();
			FindMovingBlocksCollisionBoxes(position, m_movingBlocksCollisionBoxes);
			if (!MoveToFreeSpace())
			{
				ComponentHealth componentHealth = base.Entity.FindComponent<ComponentHealth>();
				if (componentHealth != null)
				{
					componentHealth.Injure(1f, null, ignoreInvulnerability: true, "Crushed");
				}
				else
				{
					base.Project.RemoveEntity(base.Entity, disposeEntity: true);
				}
				return;
			}
			if (IsGravityEnabled)
			{
				m_velocity.Y -= 10f * dt;
				if (ImmersionFactor > 0f)
				{
					float num = ImmersionFactor * (1f + 0.03f * MathUtils.Sin((float)MathUtils.Remainder(2.0 * m_subsystemTime.GameTime, 6.2831854820251465)));
					m_velocity.Y += 10f * (1f / Density * num) * dt;
				}
			}
			float num2 = MathUtils.Saturate(AirDrag.X * dt);
			float num3 = MathUtils.Saturate(AirDrag.Y * dt);
			m_velocity.X *= 1f - num2;
			m_velocity.Y *= 1f - num3;
			m_velocity.Z *= 1f - num2;
			if (IsWaterDragEnabled && ImmersionFactor > 0f && ImmersionFluidBlock != null)
			{
				Vector2? vector = m_subsystemFluidBlockBehavior.CalculateFlowSpeed(Terrain.ToCell(position.X), Terrain.ToCell(position.Y), Terrain.ToCell(position.Z));
				Vector3 vector2 = vector.HasValue ? new Vector3(vector.Value.X, 0f, vector.Value.Y) : Vector3.Zero;
				float num4 = 1f;
				if (ImmersionFluidBlock.FrictionFactor != 1f)
				{
					num4 = ((SimplexNoise.Noise((float)MathUtils.Remainder(6.0 * Time.FrameStartTime + (double)(GetHashCode() % 1000), 1000.0)) > 0.5f) ? ImmersionFluidBlock.FrictionFactor : 1f);
				}
				float f = MathUtils.Saturate(WaterDrag.X * num4 * ImmersionFactor * dt);
				float f2 = MathUtils.Saturate(WaterDrag.Y * num4 * dt);
				m_velocity.X = MathUtils.Lerp(m_velocity.X, vector2.X, f);
				m_velocity.Y = MathUtils.Lerp(m_velocity.Y, vector2.Y, f2);
				m_velocity.Z = MathUtils.Lerp(m_velocity.Z, vector2.Z, f);
				if (m_parentBody == null && vector.HasValue && !StandingOnValue.HasValue)
				{
					if (WaterTurnSpeed > 0f)
					{
						float s = MathUtils.Saturate(MathUtils.Lerp(1f, 0f, m_velocity.Length()));
						Vector2 vector3 = Vector2.Normalize(vector.Value) * s;
						base.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, WaterTurnSpeed * (-1f * vector3.X + 0.71f * vector3.Y) * dt);
					}
					if (WaterSwayAngle > 0f)
					{
						base.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, WaterSwayAngle * (float)MathUtils.Sin((double)(200f / Mass) * m_subsystemTime.GameTime));
					}
				}
			}
			if (m_parentBody != null)
			{
				Vector3 v = Vector3.Transform(ParentBodyPositionOffset, m_parentBody.Rotation) + m_parentBody.Position - position;
				m_velocity = ((dt > 0f) ? (v / dt) : Vector3.Zero);
				base.Rotation = ParentBodyRotationOffset * m_parentBody.Rotation;
			}
			StandingOnValue = null;
			StandingOnBody = null;
			StandingOnVelocity = Vector3.Zero;
			Vector3 velocity = m_velocity;
			float num5 = m_velocity.Length();
			if (num5 > 0f)
			{
				float x = 0.45f * MathUtils.Min(BoxSize.X, BoxSize.Y, BoxSize.Z) / num5;
				float num6 = dt;
				while (num6 > 0f)
				{
					float num7 = MathUtils.Min(num6, x);
					MoveWithCollision(num7, m_velocity * num7 + m_directMove);
					m_directMove = Vector3.Zero;
					num6 -= num7;
				}
			}
			CollisionVelocityChange = m_velocity - velocity;
			if (IsGroundDragEnabled && StandingOnValue.HasValue)
			{
				m_velocity = Vector3.Lerp(m_velocity, StandingOnVelocity, 6f * dt);
			}
			if (!StandingOnValue.HasValue)
			{
				IsSneaking = false;
			}
			UpdateImmersionData();
			if (ImmersionFluidBlock is WaterBlock && ImmersionDepth > 0.3f && !m_fluidEffectsPlayed)
			{
				m_fluidEffectsPlayed = true;
				m_subsystemAudio.PlayRandomSound("Audio/WaterFallIn", m_random.Float(0.75f, 1f), m_random.Float(-0.3f, 0f), position, 4f, autoDelay: true);
				m_subsystemParticles.AddParticleSystem(new WaterSplashParticleSystem(m_subsystemTerrain, position, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f));
			}
			else if (ImmersionFluidBlock is MagmaBlock && ImmersionDepth > 0f && !m_fluidEffectsPlayed)
			{
				m_fluidEffectsPlayed = true;
				m_subsystemAudio.PlaySound("Audio/SizzleLong", 1f, 0f, position, 4f, autoDelay: true);
				m_subsystemParticles.AddParticleSystem(new MagmaSplashParticleSystem(m_subsystemTerrain, position, (BoundingBox.Max - BoundingBox.Min).Length() > 0.8f));
			}
			else if (ImmersionFluidBlock == null)
			{
				m_fluidEffectsPlayed = false;
			}
		}

		public void UpdateImmersionData()
		{
			Vector3 position = base.Position;
			int x = Terrain.ToCell(position.X);
			int y = Terrain.ToCell(position.Y + 0.01f);
			int z = Terrain.ToCell(position.Z);
			FluidBlock surfaceFluidBlock;
			float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z, out surfaceFluidBlock);
			if (surfaceHeight.HasValue)
			{
				int cellValue = m_subsystemTerrain.Terrain.GetCellValue(x, y, z);
				ImmersionDepth = MathUtils.Max(surfaceHeight.Value - position.Y, 0f);
				ImmersionFactor = MathUtils.Saturate(MathUtils.Pow(ImmersionDepth / BoxSize.Y, 0.7f));
				ImmersionFluidBlock = BlocksManager.FluidBlocks[Terrain.ExtractContents(cellValue)];
			}
			else
			{
				ImmersionDepth = 0f;
				ImmersionFactor = 0f;
				ImmersionFluidBlock = null;
			}
		}

		public bool MoveToFreeSpace()
		{
			Vector3 boxSize = BoxSize;
			Vector3 position = base.Position;
			for (int i = 0; i < m_freeSpaceOffsets.Length; i++)
			{
				Vector3? vector = null;
				Vector3 vector2 = position + m_freeSpaceOffsets[i];
				if (Terrain.ToCell(vector2) != Terrain.ToCell(position))
				{
					continue;
				}
				BoundingBox box = new BoundingBox(vector2 - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), vector2 + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
				box.Min += new Vector3(0.01f, MaxSmoothRiseHeight + 0.01f, 0.01f);
				box.Max -= new Vector3(0.01f);
				m_collisionBoxes.Clear();
				FindTerrainCollisionBoxes(box, m_collisionBoxes);
				m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
				m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
				if (!IsColliding(box, m_collisionBoxes))
				{
					vector = vector2;
				}
				else
				{
					m_stoppedTime = 0f;
					CollisionBox pushingCollisionBox;
					float num = CalculatePushBack(box, 0, m_collisionBoxes, out pushingCollisionBox);
					CollisionBox pushingCollisionBox2;
					float num2 = CalculatePushBack(box, 1, m_collisionBoxes, out pushingCollisionBox2);
					CollisionBox pushingCollisionBox3;
					float num3 = CalculatePushBack(box, 2, m_collisionBoxes, out pushingCollisionBox3);
					float num4 = num * num;
					float num5 = num2 * num2;
					float num6 = num3 * num3;
					List<Vector3> list = new List<Vector3>();
					if (num4 <= num5 && num4 <= num6)
					{
						list.Add(vector2 + new Vector3(num, 0f, 0f));
						if (num5 <= num6)
						{
							list.Add(vector2 + new Vector3(0f, num2, 0f));
							list.Add(vector2 + new Vector3(0f, 0f, num3));
						}
						else
						{
							list.Add(vector2 + new Vector3(0f, 0f, num3));
							list.Add(vector2 + new Vector3(0f, num2, 0f));
						}
					}
					else if (num5 <= num4 && num5 <= num6)
					{
						list.Add(vector2 + new Vector3(0f, num2, 0f));
						if (num4 <= num6)
						{
							list.Add(vector2 + new Vector3(num, 0f, 0f));
							list.Add(vector2 + new Vector3(0f, 0f, num3));
						}
						else
						{
							list.Add(vector2 + new Vector3(0f, 0f, num3));
							list.Add(vector2 + new Vector3(num, 0f, 0f));
						}
					}
					else
					{
						list.Add(vector2 + new Vector3(0f, 0f, num3));
						if (num4 <= num5)
						{
							list.Add(vector2 + new Vector3(num, 0f, 0f));
							list.Add(vector2 + new Vector3(0f, num2, 0f));
						}
						else
						{
							list.Add(vector2 + new Vector3(0f, num2, 0f));
							list.Add(vector2 + new Vector3(num, 0f, 0f));
						}
					}
					foreach (Vector3 item in list)
					{
						box = new BoundingBox(item - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), item + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
						box.Min += new Vector3(0.02f, MaxSmoothRiseHeight + 0.02f, 0.02f);
						box.Max -= new Vector3(0.02f);
						m_collisionBoxes.Clear();
						FindTerrainCollisionBoxes(box, m_collisionBoxes);
						m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
						m_collisionBoxes.AddRange(m_bodiesCollisionBoxes);
						if (!IsColliding(box, m_collisionBoxes))
						{
							vector = item;
							break;
						}
					}
				}
				if (vector.HasValue)
				{
					base.Position = vector.Value;
					return true;
				}
			}
			return false;
		}

		public void MoveWithCollision(float dt, Vector3 move)
		{
			Vector3 position = base.Position;
			bool isSmoothRising = IsSmoothRiseEnabled && MaxSmoothRiseHeight > 0f && HandleSmoothRise(ref move, position, dt);
			HandleAxisCollision(1, move.Y, ref position, isSmoothRising);
			HandleAxisCollision(0, move.X, ref position, isSmoothRising);
			HandleAxisCollision(2, move.Z, ref position, isSmoothRising);
			base.Position = position;
		}

		public bool HandleSmoothRise(ref Vector3 move, Vector3 position, float dt)
		{
			Vector3 boxSize = BoxSize;
			BoundingBox box = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
			box.Min += new Vector3(0.04f, 0f, 0.04f);
			box.Max -= new Vector3(0.04f, 0f, 0.04f);
			m_collisionBoxes.Clear();
			FindTerrainCollisionBoxes(box, m_collisionBoxes);
			m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
			CollisionBox pushingCollisionBox;
			float num = MathUtils.Max(CalculatePushBack(box, 1, m_collisionBoxes, out pushingCollisionBox), 0f);
			if (!BlocksManager.Blocks[Terrain.ExtractContents(pushingCollisionBox.BlockValue)].NoSmoothRise && num > 0.04f)
			{
				float x = MathUtils.Min(4.5f * dt, num);
				move.Y = MathUtils.Max(move.Y, x);
				m_velocity.Y = MathUtils.Max(m_velocity.Y, 0f);
				StandingOnValue = pushingCollisionBox.BlockValue;
				StandingOnBody = pushingCollisionBox.ComponentBody;
				m_stoppedTime = 0f;
				return true;
			}
			return false;
		}

		public void HandleAxisCollision(int axis, float move, ref Vector3 position, bool isSmoothRising)
		{
			Vector3 boxSize = BoxSize;
			m_collisionBoxes.Clear();
			if (IsSneaking && axis != 1)
			{
				FindSneakCollisionBoxes(position, new Vector2(boxSize.X - 0.08f, boxSize.Z - 0.08f), m_collisionBoxes);
			}
			Vector3 v;
			switch (axis)
			{
			case 0:
				position.X += move;
				v = new Vector3(0f, 0.04f, 0.04f);
				break;
			case 1:
				position.Y += move;
				v = new Vector3(0.04f, 0f, 0.04f);
				break;
			default:
				position.Z += move;
				v = new Vector3(0.04f, 0.04f, 0f);
				break;
			}
			BoundingBox boundingBox = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f) + v, position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f) - v);
			FindTerrainCollisionBoxes(boundingBox, m_collisionBoxes);
			m_collisionBoxes.AddRange(m_movingBlocksCollisionBoxes);
			float num;
			CollisionBox pushingCollisionBox;
			if ((axis != 1) | isSmoothRising)
			{
				BoundingBox smoothRiseBox = boundingBox;
				smoothRiseBox.Min.Y += MaxSmoothRiseHeight;
				num = CalculateSmoothRisePushBack(boundingBox, smoothRiseBox, axis, m_collisionBoxes, out pushingCollisionBox);
			}
			else
			{
				num = CalculatePushBack(boundingBox, axis, m_collisionBoxes, out pushingCollisionBox);
			}
			BoundingBox box = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f) + v, position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f) - v);
			CollisionBox pushingCollisionBox2;
			float num2 = CalculatePushBack(box, axis, m_bodiesCollisionBoxes, out pushingCollisionBox2);
			if (MathUtils.Abs(num) > MathUtils.Abs(num2))
			{
				if (num == 0f)
				{
					return;
				}
				int num3 = Terrain.ExtractContents(pushingCollisionBox.BlockValue);
				if (BlocksManager.Blocks[num3].HasCollisionBehavior)
				{
					SubsystemBlockBehavior[] blockBehaviors = m_subsystemBlockBehaviors.GetBlockBehaviors(num3);
					for (int i = 0; i < blockBehaviors.Length; i++)
					{
						Vector3 vector = (pushingCollisionBox.Box.Min + pushingCollisionBox.Box.Max) / 2f;
						CellFace cellFace = CellFace.FromAxisAndDirection(Terrain.ToCell(vector.X), Terrain.ToCell(vector.Y), Terrain.ToCell(vector.Z), axis, 0f - GetVectorComponent(m_velocity, axis));
						blockBehaviors[i].OnCollide(cellFace, GetVectorComponent(m_velocity, axis), this);
					}
				}
				switch (axis)
				{
				case 0:
					position.X += num;
					m_velocity.X = pushingCollisionBox.BlockVelocity.X;
					break;
				case 1:
					position.Y += num;
					m_velocity.Y = pushingCollisionBox.BlockVelocity.Y;
					if (move < 0f)
					{
						StandingOnValue = pushingCollisionBox.BlockValue;
						StandingOnBody = pushingCollisionBox.ComponentBody;
						StandingOnVelocity = pushingCollisionBox.BlockVelocity;
					}
					break;
				default:
					position.Z += num;
					m_velocity.Z = pushingCollisionBox.BlockVelocity.Z;
					break;
				}
			}
			else
			{
				if (num2 == 0f)
				{
					return;
				}
				ComponentBody componentBody = pushingCollisionBox2.ComponentBody;
				switch (axis)
				{
				case 0:
					InelasticCollision(m_velocity.X, componentBody.m_velocity.X, Mass, componentBody.Mass, 0.5f, out m_velocity.X, out componentBody.m_velocity.X);
					position.X += num2;
					break;
				case 1:
					InelasticCollision(m_velocity.Y, componentBody.m_velocity.Y, Mass, componentBody.Mass, 0.5f, out m_velocity.Y, out componentBody.m_velocity.Y);
					position.Y += num2;
					if (move < 0f)
					{
						StandingOnValue = pushingCollisionBox2.BlockValue;
						StandingOnBody = pushingCollisionBox2.ComponentBody;
						StandingOnVelocity = new Vector3(componentBody.m_velocity.X, 0f, componentBody.m_velocity.Z);
					}
					break;
				default:
					InelasticCollision(m_velocity.Z, componentBody.m_velocity.Z, Mass, componentBody.Mass, 0.5f, out m_velocity.Z, out componentBody.m_velocity.Z);
					position.Z += num2;
					break;
				}
				if (this.CollidedWithBody != null)
				{
					this.CollidedWithBody(componentBody);
				}
				if (componentBody.CollidedWithBody != null)
				{
					componentBody.CollidedWithBody(this);
				}
			}
		}

		public void FindBodiesCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result)
		{
			m_componentBodies.Clear();
			m_subsystemBodies.FindBodiesAroundPoint(new Vector2(position.X, position.Z), 4f, m_componentBodies);
			for (int i = 0; i < m_componentBodies.Count; i++)
			{
				ComponentBody componentBody = m_componentBodies.Array[i];
				if (componentBody != this && componentBody != m_parentBody && componentBody.m_parentBody != this)
				{
					result.Add(new CollisionBox
					{
						Box = componentBody.BoundingBox,
						ComponentBody = componentBody
					});
				}
			}
		}

		public void FindMovingBlocksCollisionBoxes(Vector3 position, DynamicArray<CollisionBox> result)
		{
			Vector3 boxSize = BoxSize;
			BoundingBox boundingBox = new BoundingBox(position - new Vector3(boxSize.X / 2f, 0f, boxSize.Z / 2f), position + new Vector3(boxSize.X / 2f, boxSize.Y, boxSize.Z / 2f));
			boundingBox.Min -= new Vector3(1f);
			boundingBox.Max += new Vector3(1f);
			m_movingBlockSets.Clear();
			m_subsystemMovingBlocks.FindMovingBlocks(boundingBox, extendToFillCells: false, m_movingBlockSets);
			for (int i = 0; i < m_movingBlockSets.Count; i++)
			{
				IMovingBlockSet movingBlockSet = m_movingBlockSets.Array[i];
				for (int j = 0; j < movingBlockSet.Blocks.Count; j++)
				{
					MovingBlock movingBlock = movingBlockSet.Blocks[j];
					int num = Terrain.ExtractContents(movingBlock.Value);
					Block block = BlocksManager.Blocks[num];
					if (block.IsCollidable)
					{
						BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, movingBlock.Value);
						Vector3 v = new Vector3(movingBlock.Offset) + movingBlockSet.Position;
						for (int k = 0; k < customCollisionBoxes.Length; k++)
						{
							result.Add(new CollisionBox
							{
								Box = new BoundingBox(v + customCollisionBoxes[k].Min, v + customCollisionBoxes[k].Max),
								BlockValue = movingBlock.Value,
								BlockVelocity = movingBlockSet.CurrentVelocity
							});
						}
					}
				}
			}
		}

		public void FindTerrainCollisionBoxes(BoundingBox box, DynamicArray<CollisionBox> result)
		{
			Point3 point = Terrain.ToCell(box.Min);
			Point3 point2 = Terrain.ToCell(box.Max);
			point.Y = MathUtils.Max(point.Y, 0);
			point2.Y = MathUtils.Min(point2.Y, 255);
			if (point.Y > point2.Y)
			{
				return;
			}
			for (int i = point.X; i <= point2.X; i++)
			{
				for (int j = point.Z; j <= point2.Z; j++)
				{
					TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(i, j);
					if (chunkAtCell == null)
					{
						continue;
					}
					int num = TerrainChunk.CalculateCellIndex(i & 0xF, point.Y, j & 0xF);
					int num2 = point.Y;
					while (num2 <= point2.Y)
					{
						int cellValueFast = chunkAtCell.GetCellValueFast(num);
						int num3 = Terrain.ExtractContents(cellValueFast);
						if (num3 != 0)
						{
							Block block = BlocksManager.Blocks[num3];
							if (block.IsCollidable)
							{
								BoundingBox[] customCollisionBoxes = block.GetCustomCollisionBoxes(m_subsystemTerrain, cellValueFast);
								Vector3 v = new Vector3(i, num2, j);
								for (int k = 0; k < customCollisionBoxes.Length; k++)
								{
									result.Add(new CollisionBox
									{
										Box = new BoundingBox(v + customCollisionBoxes[k].Min, v + customCollisionBoxes[k].Max),
										BlockValue = cellValueFast
									});
								}
							}
						}
						num2++;
						num++;
					}
				}
			}
		}

		public void FindSneakCollisionBoxes(Vector3 position, Vector2 overhang, DynamicArray<CollisionBox> result)
		{
			int num = Terrain.ToCell(position.X);
			int num2 = Terrain.ToCell(position.Y);
			int num3 = Terrain.ToCell(position.Z);
			if (BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3)].IsCollidable)
			{
				return;
			}
			bool num4 = position.X < (float)num + 0.5f;
			bool flag = position.Z < (float)num3 + 0.5f;
			CollisionBox item;
			if (num4)
			{
				if (flag)
				{
					bool isCollidable = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)].IsCollidable;
					bool isCollidable2 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)].IsCollidable;
					bool isCollidable3 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 - 1)].IsCollidable;
					if ((isCollidable && !isCollidable2) || ((!isCollidable && !isCollidable2) & isCollidable3))
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
							BlockValue = 0
						};
						result.Add(item);
					}
					if ((!isCollidable && isCollidable2) || ((!isCollidable && !isCollidable2) & isCollidable3))
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)),
							BlockValue = 0
						};
						result.Add(item);
					}
					if (isCollidable && isCollidable2)
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
							BlockValue = 0
						};
						result.Add(item);
					}
				}
				else
				{
					bool isCollidable4 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)].IsCollidable;
					bool isCollidable5 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3)].IsCollidable;
					bool isCollidable6 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num - 1, num2 - 1, num3 + 1)].IsCollidable;
					if ((isCollidable4 && !isCollidable5) || ((!isCollidable4 && !isCollidable5) & isCollidable6))
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
							BlockValue = 0
						};
						result.Add(item);
					}
					if ((!isCollidable4 && isCollidable5) || ((!isCollidable4 && !isCollidable5) & isCollidable6))
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, num3 + 1)),
							BlockValue = 0
						};
						result.Add(item);
					}
					if (isCollidable4 && isCollidable5)
					{
						item = new CollisionBox
						{
							Box = new BoundingBox(new Vector3((float)num + overhang.X, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
							BlockValue = 0
						};
						result.Add(item);
					}
				}
			}
			else if (flag)
			{
				bool isCollidable7 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 - 1)].IsCollidable;
				bool isCollidable8 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)].IsCollidable;
				bool isCollidable9 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 - 1)].IsCollidable;
				if ((isCollidable7 && !isCollidable8) || ((!isCollidable7 && !isCollidable8) & isCollidable9))
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3(num + 1, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item);
				}
				if ((!isCollidable7 && isCollidable8) || ((!isCollidable7 && !isCollidable8) & isCollidable9))
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item);
				}
				if (isCollidable7 && isCollidable8)
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, (float)num3 + overhang.Y), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item);
				}
			}
			else
			{
				bool isCollidable10 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num, num2 - 1, num3 + 1)].IsCollidable;
				bool isCollidable11 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3)].IsCollidable;
				bool isCollidable12 = BlocksManager.Blocks[m_subsystemTerrain.Terrain.GetCellContents(num + 1, num2 - 1, num3 + 1)].IsCollidable;
				if ((isCollidable10 && !isCollidable11) || ((!isCollidable10 && !isCollidable11) & isCollidable12))
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3(num + 1, num2 + 1, (float)(num3 + 1) - overhang.Y)),
						BlockValue = 0
					};
					result.Add(item);
				}
				if ((!isCollidable10 && isCollidable11) || ((!isCollidable10 && !isCollidable11) & isCollidable12))
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, num3 + 1)),
						BlockValue = 0
					};
					result.Add(item);
				}
				if (isCollidable10 && isCollidable11)
				{
					item = new CollisionBox
					{
						Box = new BoundingBox(new Vector3(num, num2, num3), new Vector3((float)(num + 1) - overhang.X, num2 + 1, (float)(num3 + 1) - overhang.Y)),
						BlockValue = 0
					};
					result.Add(item);
				}
			}
		}

		public bool IsColliding(BoundingBox box, DynamicArray<CollisionBox> collisionBoxes)
		{
			for (int i = 0; i < collisionBoxes.Count; i++)
			{
				if (box.Intersection(collisionBoxes.Array[i].Box))
				{
					return true;
				}
			}
			return false;
		}

		public float CalculatePushBack(BoundingBox box, int axis, DynamicArray<CollisionBox> collisionBoxes, out CollisionBox pushingCollisionBox)
		{
			pushingCollisionBox = default(CollisionBox);
			float num = 0f;
			for (int i = 0; i < collisionBoxes.Count; i++)
			{
				float num2 = CalculateBoxBoxOverlap(ref box, ref collisionBoxes.Array[i].Box, axis);
				if (MathUtils.Abs(num2) > MathUtils.Abs(num))
				{
					num = num2;
					pushingCollisionBox = collisionBoxes.Array[i];
				}
			}
			return num;
		}

		public float CalculateSmoothRisePushBack(BoundingBox normalBox, BoundingBox smoothRiseBox, int axis, DynamicArray<CollisionBox> collisionBoxes, out CollisionBox pushingCollisionBox)
		{
			pushingCollisionBox = default(CollisionBox);
			float num = 0f;
			for (int i = 0; i < collisionBoxes.Count; i++)
			{
				float num2 = (!BlocksManager.Blocks[Terrain.ExtractContents(collisionBoxes.Array[i].BlockValue)].NoSmoothRise) ? CalculateBoxBoxOverlap(ref smoothRiseBox, ref collisionBoxes.Array[i].Box, axis) : CalculateBoxBoxOverlap(ref normalBox, ref collisionBoxes.Array[i].Box, axis);
				if (MathUtils.Abs(num2) > MathUtils.Abs(num))
				{
					num = num2;
					pushingCollisionBox = collisionBoxes.Array[i];
				}
			}
			return num;
		}

		public static float CalculateBoxBoxOverlap(ref BoundingBox b1, ref BoundingBox b2, int axis)
		{
			if (b1.Max.X <= b2.Min.X || b1.Min.X >= b2.Max.X || b1.Max.Y <= b2.Min.Y || b1.Min.Y >= b2.Max.Y || b1.Max.Z <= b2.Min.Z || b1.Min.Z >= b2.Max.Z)
			{
				return 0f;
			}
			switch (axis)
			{
			case 0:
			{
				float num13 = b1.Min.X + b1.Max.X;
				float num14 = b2.Min.X + b2.Max.X;
				float num15 = b1.Max.X - b1.Min.X;
				float num16 = b2.Max.X - b2.Min.X;
				float num17 = num14 - num13;
				float num18 = num15 + num16;
				return 0.5f * ((num17 > 0f) ? (num17 - num18) : (num17 + num18));
			}
			case 1:
			{
				float num7 = b1.Min.Y + b1.Max.Y;
				float num8 = b2.Min.Y + b2.Max.Y;
				float num9 = b1.Max.Y - b1.Min.Y;
				float num10 = b2.Max.Y - b2.Min.Y;
				float num11 = num8 - num7;
				float num12 = num9 + num10;
				return 0.5f * ((num11 > 0f) ? (num11 - num12) : (num11 + num12));
			}
			default:
			{
				float num = b1.Min.Z + b1.Max.Z;
				float num2 = b2.Min.Z + b2.Max.Z;
				float num3 = b1.Max.Z - b1.Min.Z;
				float num4 = b2.Max.Z - b2.Min.Z;
				float num5 = num2 - num;
				float num6 = num3 + num4;
				return 0.5f * ((num5 > 0f) ? (num5 - num6) : (num5 + num6));
			}
			}
		}

		public static float GetVectorComponent(Vector3 v, int axis)
		{
			switch (axis)
			{
			case 0:
				return v.X;
			case 1:
				return v.Y;
			default:
				return v.Z;
			}
		}

		public static void InelasticCollision(float v1, float v2, float m1, float m2, float cr, out float result1, out float result2)
		{
			float num = 1f / (m1 + m2);
			result1 = (cr * m2 * (v2 - v1) + m1 * v1 + m2 * v2) * num;
			result2 = (cr * m1 * (v1 - v2) + m1 * v1 + m2 * v2) * num;
		}
	}
}
