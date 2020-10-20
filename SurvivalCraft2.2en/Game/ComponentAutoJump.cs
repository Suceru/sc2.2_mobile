using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentAutoJump : Component, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemTerrain m_subsystemTerrain;

		public ComponentCreature m_componentCreature;

		public double m_lastAutoJumpTime;

		public bool m_alwaysEnabled;

		public float m_jumpStrength;

		public bool m_collidedWithBody;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void Update(float dt)
		{
			if ((SettingsManager.AutoJump || m_alwaysEnabled) && m_subsystemTime.GameTime - m_lastAutoJumpTime > 0.25)
			{
				Vector2? lastWalkOrder = m_componentCreature.ComponentLocomotion.LastWalkOrder;
				if (lastWalkOrder.HasValue)
				{
					Vector2 vector = new Vector2(m_componentCreature.ComponentBody.CollisionVelocityChange.X, m_componentCreature.ComponentBody.CollisionVelocityChange.Z);
					if (vector != Vector2.Zero && !m_collidedWithBody)
					{
						Vector2 v = Vector2.Normalize(vector);
						Vector3 vector2 = m_componentCreature.ComponentBody.Matrix.Right * lastWalkOrder.Value.X + m_componentCreature.ComponentBody.Matrix.Forward * lastWalkOrder.Value.Y;
						Vector2 v2 = Vector2.Normalize(new Vector2(vector2.X, vector2.Z));
						bool flag = false;
						Vector3 v3 = Vector3.Zero;
						Vector3 vector3 = Vector3.Zero;
						Vector3 vector4 = Vector3.Zero;
						if (Vector2.Dot(v2, -v) > 0.6f)
						{
							if (Vector2.Dot(v2, Vector2.UnitX) > 0.6f)
							{
								v3 = m_componentCreature.ComponentBody.Position + Vector3.UnitX;
								vector3 = v3 - Vector3.UnitZ;
								vector4 = v3 + Vector3.UnitZ;
								flag = true;
							}
							else if (Vector2.Dot(v2, -Vector2.UnitX) > 0.6f)
							{
								v3 = m_componentCreature.ComponentBody.Position - Vector3.UnitX;
								vector3 = v3 - Vector3.UnitZ;
								vector4 = v3 + Vector3.UnitZ;
								flag = true;
							}
							else if (Vector2.Dot(v2, Vector2.UnitY) > 0.6f)
							{
								v3 = m_componentCreature.ComponentBody.Position + Vector3.UnitZ;
								vector3 = v3 - Vector3.UnitX;
								vector4 = v3 + Vector3.UnitX;
								flag = true;
							}
							else if (Vector2.Dot(v2, -Vector2.UnitY) > 0.6f)
							{
								v3 = m_componentCreature.ComponentBody.Position - Vector3.UnitZ;
								vector3 = v3 - Vector3.UnitX;
								vector4 = v3 + Vector3.UnitX;
								flag = true;
							}
						}
						if (flag)
						{
							int cellContents = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(v3.X), Terrain.ToCell(v3.Y), Terrain.ToCell(v3.Z));
							int cellContents2 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector3.X), Terrain.ToCell(vector3.Y), Terrain.ToCell(vector3.Z));
							int cellContents3 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector4.X), Terrain.ToCell(vector4.Y), Terrain.ToCell(vector4.Z));
							int cellContents4 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(v3.X), Terrain.ToCell(v3.Y) + 1, Terrain.ToCell(v3.Z));
							int cellContents5 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector3.X), Terrain.ToCell(vector3.Y) + 1, Terrain.ToCell(vector3.Z));
							int cellContents6 = m_subsystemTerrain.Terrain.GetCellContents(Terrain.ToCell(vector4.X), Terrain.ToCell(vector4.Y) + 1, Terrain.ToCell(vector4.Z));
							Block block = BlocksManager.Blocks[cellContents];
							Block block2 = BlocksManager.Blocks[cellContents2];
							Block block3 = BlocksManager.Blocks[cellContents3];
							Block block4 = BlocksManager.Blocks[cellContents4];
							Block block5 = BlocksManager.Blocks[cellContents5];
							Block block6 = BlocksManager.Blocks[cellContents6];
							if (!block.NoAutoJump && ((block.IsCollidable && !block4.IsCollidable) || (block2.IsCollidable && !block5.IsCollidable) || (block3.IsCollidable && !block6.IsCollidable)))
							{
								m_componentCreature.ComponentLocomotion.JumpOrder = MathUtils.Max(m_jumpStrength, m_componentCreature.ComponentLocomotion.JumpOrder);
								m_lastAutoJumpTime = m_subsystemTime.GameTime;
							}
						}
					}
				}
			}
			m_collidedWithBody = false;
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_alwaysEnabled = valuesDictionary.GetValue<bool>("AlwaysEnabled");
			m_jumpStrength = valuesDictionary.GetValue<float>("JumpStrength");
			m_componentCreature.ComponentBody.CollidedWithBody += delegate
			{
				m_collidedWithBody = true;
			};
		}
	}
}
