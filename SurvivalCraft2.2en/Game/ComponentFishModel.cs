using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentFishModel : ComponentCreatureModel
	{
		public ModelBone m_bodyBone;

		public ModelBone m_tail1Bone;

		public ModelBone m_tail2Bone;

		public ModelBone m_jawBone;

		public float m_swimAnimationSpeed;

		public bool m_hasVerticalTail;

		public float m_bitingPhase;

		public float m_tailWagPhase;

		public Vector2 m_tailTurn;

		public float m_digInDepth;

		public float m_digInTailPhase;

		public float? BendOrder
		{
			get;
			set;
		}

		public float DigInOrder
		{
			get;
			set;
		}

		public override void Update(float dt)
		{
			if (m_componentCreature.ComponentLocomotion.LastSwimOrder.HasValue && m_componentCreature.ComponentLocomotion.LastSwimOrder.Value != Vector3.Zero)
			{
				float num = (m_componentCreature.ComponentLocomotion.LastSwimOrder.Value.LengthSquared() > 0.99f) ? 1.75f : 1f;
				base.MovementAnimationPhase = MathUtils.Remainder(base.MovementAnimationPhase + m_swimAnimationSpeed * num * dt, 1000f);
			}
			else
			{
				base.MovementAnimationPhase = MathUtils.Remainder(base.MovementAnimationPhase + 0.15f * m_swimAnimationSpeed * dt, 1000f);
			}
			if (BendOrder.HasValue)
			{
				if (m_hasVerticalTail)
				{
					m_tailTurn.X = 0f;
					m_tailTurn.Y = BendOrder.Value;
				}
				else
				{
					m_tailTurn.X = BendOrder.Value;
					m_tailTurn.Y = 0f;
				}
			}
			else
			{
				m_tailTurn.X += MathUtils.Saturate(2f * m_componentCreature.ComponentLocomotion.TurnSpeed * dt) * (0f - m_componentCreature.ComponentLocomotion.LastTurnOrder.X - m_tailTurn.X);
			}
			if (DigInOrder > m_digInDepth)
			{
				float num2 = (DigInOrder - m_digInDepth) * MathUtils.Min(1.5f * dt, 1f);
				m_digInDepth += num2;
				m_digInTailPhase += 20f * num2;
			}
			else if (DigInOrder < m_digInDepth)
			{
				m_digInDepth += (DigInOrder - m_digInDepth) * MathUtils.Min(5f * dt, 1f);
			}
			float num3 = 0.33f * m_componentCreature.ComponentLocomotion.TurnSpeed;
			float num4 = 1f * m_componentCreature.ComponentLocomotion.TurnSpeed;
			base.IsAttackHitMoment = false;
			if (base.AttackOrder || base.FeedOrder)
			{
				if (base.AttackOrder)
				{
					m_tailWagPhase = MathUtils.Remainder(m_tailWagPhase + num3 * dt, 1f);
				}
				float bitingPhase = m_bitingPhase;
				m_bitingPhase = MathUtils.Remainder(m_bitingPhase + num4 * dt, 1f);
				if (base.AttackOrder && bitingPhase < 0.5f && m_bitingPhase >= 0.5f)
				{
					base.IsAttackHitMoment = true;
				}
			}
			else
			{
				if (m_tailWagPhase != 0f)
				{
					m_tailWagPhase = MathUtils.Remainder(MathUtils.Min(m_tailWagPhase + num3 * dt, 1f), 1f);
				}
				if (m_bitingPhase != 0f)
				{
					m_bitingPhase = MathUtils.Remainder(MathUtils.Min(m_bitingPhase + num4 * dt, 1f), 1f);
				}
			}
			base.AttackOrder = false;
			base.FeedOrder = false;
			BendOrder = null;
			DigInOrder = 0f;
			base.Update(dt);
		}

		public override void Animate()
		{
			Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
			if (m_componentCreature.ComponentHealth.Health > 0f)
			{
				float num = m_digInTailPhase + m_tailWagPhase;
				float num2;
				float num3;
				float num4;
				float num5;
				if (m_hasVerticalTail)
				{
					num2 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * num) - m_tailTurn.X, -1f, 1f);
					num3 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin(2f * ((float)Math.PI * MathUtils.Max(num - 0.25f, 0f))) - m_tailTurn.X, -1f, 1f);
					num4 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * base.MovementAnimationPhase) - m_tailTurn.Y, -1f, 1f);
					num5 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * MathUtils.Max(base.MovementAnimationPhase - 0.25f, 0f)) - m_tailTurn.Y, -1f, 1f);
				}
				else
				{
					num2 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0.5f * MathUtils.Sin((float)Math.PI * 2f * (base.MovementAnimationPhase + num)) - m_tailTurn.X, -1f, 1f);
					num3 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0.5f * MathUtils.Sin(2f * ((float)Math.PI * MathUtils.Max(base.MovementAnimationPhase + num - 0.25f, 0f))) - m_tailTurn.X, -1f, 1f);
					num4 = MathUtils.DegToRad(25f) * MathUtils.Clamp(0f - m_tailTurn.Y, -1f, 1f);
					num5 = MathUtils.DegToRad(30f) * MathUtils.Clamp(0f - m_tailTurn.Y, -1f, 1f);
				}
				float radians = 0f;
				if (m_bitingPhase > 0f)
				{
					radians = (0f - MathUtils.DegToRad(30f)) * MathUtils.Sin((float)Math.PI * m_bitingPhase);
				}
				Matrix value = Matrix.CreateFromYawPitchRoll(vector.X, 0f, 0f) * Matrix.CreateTranslation(m_componentCreature.ComponentBody.Position + new Vector3(0f, 0f - m_digInDepth, 0f));
				SetBoneTransform(m_bodyBone.Index, value);
				Matrix identity = Matrix.Identity;
				if (num2 != 0f)
				{
					identity *= Matrix.CreateRotationZ(num2);
				}
				if (num4 != 0f)
				{
					identity *= Matrix.CreateRotationX(num4);
				}
				Matrix identity2 = Matrix.Identity;
				if (num3 != 0f)
				{
					identity2 *= Matrix.CreateRotationZ(num3);
				}
				if (num5 != 0f)
				{
					identity2 *= Matrix.CreateRotationX(num5);
				}
				SetBoneTransform(m_tail1Bone.Index, identity);
				SetBoneTransform(m_tail2Bone.Index, identity2);
				if (m_jawBone != null)
				{
					SetBoneTransform(m_jawBone.Index, Matrix.CreateRotationX(radians));
				}
			}
			else
			{
				float num6 = m_componentCreature.ComponentBody.BoundingBox.Max.Y - m_componentCreature.ComponentBody.BoundingBox.Min.Y;
				Vector3 position = m_componentCreature.ComponentBody.Position + 1f * num6 * base.DeathPhase * Vector3.UnitY;
				SetBoneTransform(m_bodyBone.Index, Matrix.CreateFromYawPitchRoll(vector.X, 0f, (float)Math.PI * base.DeathPhase) * Matrix.CreateTranslation(position));
				SetBoneTransform(m_tail1Bone.Index, Matrix.Identity);
				SetBoneTransform(m_tail2Bone.Index, Matrix.Identity);
				if (m_jawBone != null)
				{
					SetBoneTransform(m_jawBone.Index, Matrix.Identity);
				}
			}
			base.Animate();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_hasVerticalTail = valuesDictionary.GetValue<bool>("HasVerticalTail");
			m_swimAnimationSpeed = valuesDictionary.GetValue<float>("SwimAnimationSpeed");
		}

		protected override void SetModel(Model model)
		{
			base.SetModel(model);
			if (base.Model != null)
			{
				m_bodyBone = base.Model.FindBone("Body");
				m_tail1Bone = base.Model.FindBone("Tail1");
				m_tail2Bone = base.Model.FindBone("Tail2");
				m_jawBone = base.Model.FindBone("Jaw", throwIfNotFound: false);
			}
			else
			{
				m_bodyBone = null;
				m_tail1Bone = null;
				m_tail2Bone = null;
				m_jawBone = null;
			}
		}

		protected override Vector3 CalculateEyePosition()
		{
			Matrix matrix = m_componentCreature.ComponentBody.Matrix;
			return m_componentCreature.ComponentBody.Position + matrix.Up * 1f * m_componentCreature.ComponentBody.BoxSize.Y + matrix.Forward * 0.45f * m_componentCreature.ComponentBody.BoxSize.Z;
		}
	}
}
