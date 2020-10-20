using Engine;
using Engine.Graphics;
using Engine.Media;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentHumanModel : ComponentCreatureModel
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemModelsRenderer m_subsystemModelsRenderer;

		public SubsystemNoise m_subsystemNoise;

		public SubsystemAudio m_subsystemAudio;

		public ComponentMiner m_componentMiner;

		public ComponentRider m_componentRider;

		public ComponentSleep m_componentSleep;

		public ComponentPlayer m_componentPlayer;

		public Random m_random = new Random();

		public DrawBlockEnvironmentData m_drawBlockEnvironmentData = new DrawBlockEnvironmentData();

		public ModelBone m_bodyBone;

		public ModelBone m_headBone;

		public ModelBone m_leg1Bone;

		public ModelBone m_leg2Bone;

		public ModelBone m_hand1Bone;

		public ModelBone m_hand2Bone;

		public float m_sneakFactor;

		public float m_lieDownFactorEye;

		public float m_lieDownFactorModel;

		public float m_walkAnimationSpeed;

		public float m_walkLegsAngle;

		public float m_walkBobHeight;

		public float m_headingOffset;

		public float m_punchFactor;

		public float m_punchPhase;

		public int m_punchCounter;

		public float m_footstepsPhase;

		public bool m_rowLeft;

		public bool m_rowRight;

		public float m_aimHandAngle;

		public Vector3 m_inHandItemOffset;

		public Vector3 m_inHandItemRotation;

		public Vector2 m_headAngles;

		public Vector2 m_handAngles1;

		public Vector2 m_handAngles2;

		public Vector2 m_legAngles1;

		public Vector2 m_legAngles2;

		public override void Update(float dt)
		{
			if (m_componentCreature.ComponentBody.IsSneaking)
			{
				m_sneakFactor = MathUtils.Min(m_sneakFactor + 2f * dt, 1f);
			}
			else
			{
				m_sneakFactor = MathUtils.Max(m_sneakFactor - 2f * dt, 0f);
			}
			if ((m_componentSleep != null && m_componentSleep.IsSleeping) || m_componentCreature.ComponentHealth.Health <= 0f)
			{
				m_lieDownFactorEye = MathUtils.Min(m_lieDownFactorEye + 1f * dt, 1f);
				m_lieDownFactorModel = MathUtils.Min(m_lieDownFactorModel + 3f * dt, 1f);
			}
			else
			{
				m_lieDownFactorEye = MathUtils.Max(m_lieDownFactorEye - 1f * dt, 0f);
				m_lieDownFactorModel = MathUtils.Max(m_lieDownFactorModel - 3f * dt, 0f);
			}
			bool flag = true;
			bool flag2 = true;
			float footstepsPhase = m_footstepsPhase;
			if (m_componentCreature.ComponentLocomotion.LadderValue.HasValue)
			{
				m_footstepsPhase += 1.5f * m_walkAnimationSpeed * m_componentCreature.ComponentBody.Velocity.Length() * dt;
				flag2 = false;
			}
			else if (!m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled)
			{
				float num = m_componentCreature.ComponentLocomotion.SlipSpeed ?? (m_componentCreature.ComponentBody.Velocity.XZ - m_componentCreature.ComponentBody.StandingOnVelocity.XZ).Length();
				if (num > 0.5f)
				{
					base.MovementAnimationPhase += num * dt * m_walkAnimationSpeed;
					m_footstepsPhase += 1f * m_walkAnimationSpeed * num * dt;
					flag = false;
					flag2 = false;
				}
			}
			if (flag)
			{
				float num2 = 0.5f * MathUtils.Floor(2f * base.MovementAnimationPhase);
				if (base.MovementAnimationPhase != num2)
				{
					if (base.MovementAnimationPhase - num2 > 0.25f)
					{
						base.MovementAnimationPhase = MathUtils.Min(base.MovementAnimationPhase + 2f * dt, num2 + 0.5f);
					}
					else
					{
						base.MovementAnimationPhase = MathUtils.Max(base.MovementAnimationPhase - 2f * dt, num2);
					}
				}
			}
			if (flag2)
			{
				m_footstepsPhase = 0f;
			}
			float num3 = 0f;
			ComponentMount componentMount = (m_componentRider != null) ? m_componentRider.Mount : null;
			if (componentMount != null)
			{
				ComponentCreatureModel componentCreatureModel = componentMount.Entity.FindComponent<ComponentCreatureModel>();
				if (componentCreatureModel != null)
				{
					base.Bob = componentCreatureModel.Bob;
					num3 = base.Bob;
				}
				m_headingOffset = 0f;
			}
			else
			{
				float x = MathUtils.Sin((float)Math.PI * 2f * base.MovementAnimationPhase);
				num3 = m_walkBobHeight * MathUtils.Sqr(x);
				float num4 = 0f;
				if (m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue && m_componentCreature.ComponentLocomotion.LastWalkOrder != Vector2.Zero)
				{
					num4 = Vector2.Angle(Vector2.UnitY, m_componentCreature.ComponentLocomotion.LastWalkOrder.Value);
				}
				m_headingOffset += MathUtils.NormalizeAngle(num4 - m_headingOffset) * MathUtils.Saturate(8f * m_subsystemTime.GameTimeDelta);
				m_headingOffset = MathUtils.NormalizeAngle(m_headingOffset);
			}
			float num5 = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
			base.Bob += num5 * (num3 - base.Bob);
			base.IsAttackHitMoment = false;
			if (base.AttackOrder)
			{
				m_punchFactor = MathUtils.Min(m_punchFactor + 4f * dt, 1f);
				float punchPhase = m_punchPhase;
				m_punchPhase = MathUtils.Remainder(m_punchPhase + dt * 2f, 1f);
				if (punchPhase < 0.5f && m_punchPhase >= 0.5f)
				{
					base.IsAttackHitMoment = true;
					m_punchCounter++;
				}
			}
			else
			{
				m_punchFactor = MathUtils.Max(m_punchFactor - 4f * dt, 0f);
				if (m_punchPhase != 0f)
				{
					if (m_punchPhase > 0.5f)
					{
						m_punchPhase = MathUtils.Remainder(MathUtils.Min(m_punchPhase + dt * 2f, 1f), 1f);
					}
					else if (m_punchPhase > 0f)
					{
						m_punchPhase = MathUtils.Max(m_punchPhase - dt * m_punchPhase, 0f);
					}
				}
			}
			m_rowLeft = base.RowLeftOrder;
			m_rowRight = base.RowRightOrder;
			if ((m_rowLeft || m_rowRight) && componentMount != null && componentMount.ComponentBody.ImmersionFactor > 0f && MathUtils.Floor(1.1000000238418579 * m_subsystemTime.GameTime) != MathUtils.Floor(1.1000000238418579 * (m_subsystemTime.GameTime - (double)m_subsystemTime.GameTimeDelta)))
			{
				m_subsystemAudio.PlayRandomSound("Audio/Rowing", m_random.Float(0.4f, 0.6f), m_random.Float(-0.3f, 0.2f), m_componentCreature.ComponentBody.Position, 3f, autoDelay: true);
			}
			float num6 = MathUtils.Floor(m_footstepsPhase);
			if (m_footstepsPhase > num6 && footstepsPhase <= num6)
			{
				if (!m_componentCreature.ComponentBody.IsSneaking)
				{
					m_subsystemNoise.MakeNoise(m_componentCreature.ComponentBody, 0.25f, 8f);
				}
				if (!m_componentCreature.ComponentCreatureSounds.PlayFootstepSound(1f))
				{
					m_footstepsPhase = 0f;
				}
			}
			m_aimHandAngle = base.AimHandAngleOrder;
			m_inHandItemOffset = Vector3.Lerp(m_inHandItemOffset, base.InHandItemOffsetOrder, 10f * dt);
			m_inHandItemRotation = Vector3.Lerp(m_inHandItemRotation, base.InHandItemRotationOrder, 10f * dt);
			base.AttackOrder = false;
			base.RowLeftOrder = false;
			base.RowRightOrder = false;
			base.AimHandAngleOrder = 0f;
			base.InHandItemOffsetOrder = Vector3.Zero;
			base.InHandItemRotationOrder = Vector3.Zero;
			base.Update(dt);
		}

		public override void Animate()
		{
			Vector3 position = m_componentCreature.ComponentBody.Position;
			Vector3 vector = m_componentCreature.ComponentBody.Rotation.ToYawPitchRoll();
			if (m_lieDownFactorModel == 0f)
			{
				ComponentMount componentMount = (m_componentRider != null) ? m_componentRider.Mount : null;
				float num = MathUtils.Sin((float)Math.PI * 2f * base.MovementAnimationPhase);
				position.Y += base.Bob;
				vector.X += m_headingOffset;
				float num2 = (float)MathUtils.Remainder(0.75 * m_subsystemGameInfo.TotalElapsedGameTime + (double)(GetHashCode() & 0xFFFF), 10000.0);
				float x = MathUtils.Clamp(MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(1.02f * num2 - 100f)) + m_componentCreature.ComponentLocomotion.LookAngles.X + 1f * m_componentCreature.ComponentLocomotion.LastTurnOrder.X + m_headingOffset, 0f - MathUtils.DegToRad(80f), MathUtils.DegToRad(80f));
				float y = MathUtils.Clamp(MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.Noise(0.96f * num2 - 200f)) + m_componentCreature.ComponentLocomotion.LookAngles.Y, 0f - MathUtils.DegToRad(45f), MathUtils.DegToRad(45f));
				float num3 = 0f;
				float y2 = 0f;
				float x2 = 0f;
				float y3 = 0f;
				float num4 = 0f;
				float num5 = 0f;
				float num6 = 0f;
				float num7 = 0f;
				if (componentMount != null)
				{
					if (componentMount.Entity.ValuesDictionary.DatabaseObject.Name == "Boat")
					{
						position.Y -= 0.2f;
						vector.X += (float)Math.PI;
						num4 = 0.4f;
						num6 = 0.4f;
						num5 = 0.2f;
						num7 = -0.2f;
						num3 = 1.1f;
						x2 = 1.1f;
						y2 = 0.2f;
						y3 = -0.2f;
					}
					else
					{
						num4 = 0.5f;
						num6 = 0.5f;
						num5 = 0.15f;
						num7 = -0.15f;
						y2 = 0.55f;
						y3 = -0.55f;
					}
				}
				else if (m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled)
				{
					float num8 = m_componentCreature.ComponentLocomotion.LastWalkOrder.HasValue ? MathUtils.Min(0.03f * m_componentCreature.ComponentBody.Velocity.XZ.LengthSquared(), 0.5f) : 0f;
					num3 = -0.1f - num8;
					x2 = num3;
					y2 = MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(1.07f * num2 + 400f));
					y3 = 0f - MathUtils.Lerp(0f, 0.25f, SimplexNoise.Noise(0.93f * num2 + 500f));
				}
				else if (base.MovementAnimationPhase != 0f)
				{
					num4 = -0.5f * num;
					num6 = 0.5f * num;
					num3 = m_walkLegsAngle * num;
					x2 = 0f - num3;
				}
				float num9 = 0f;
				if (m_componentMiner != null)
				{
					float num10 = MathUtils.Sin(MathUtils.Sqrt(m_componentMiner.PokingPhase) * (float)Math.PI);
					num9 = ((m_componentMiner.ActiveBlockValue == 0) ? (1f * num10) : (0.3f + 1f * num10));
				}
				float num11 = (m_punchPhase != 0f) ? ((0f - MathUtils.DegToRad(90f)) * MathUtils.Sin((float)Math.PI * 2f * MathUtils.Sigmoid(m_punchPhase, 4f))) : 0f;
				float num12 = ((m_punchCounter & 1) == 0) ? num11 : 0f;
				float num13 = ((m_punchCounter & 1) != 0) ? num11 : 0f;
				float num14 = 0f;
				float num15 = 0f;
				float num16 = 0f;
				float num17 = 0f;
				if (m_rowLeft || m_rowRight)
				{
					float num18 = 0.6f * (float)MathUtils.Sin(6.91150426864624 * m_subsystemTime.GameTime);
					float num19 = 0.2f + 0.2f * (float)MathUtils.Cos(6.91150426864624 * (m_subsystemTime.GameTime + 0.5));
					if (m_rowLeft)
					{
						num14 = num18;
						num15 = num19;
					}
					if (m_rowRight)
					{
						num16 = num18;
						num17 = 0f - num19;
					}
				}
				float num20 = 0f;
				float num21 = 0f;
				float num22 = 0f;
				float num23 = 0f;
				if (m_aimHandAngle != 0f)
				{
					num20 = 1.5f;
					num21 = -0.7f;
					num22 = m_aimHandAngle * 1f;
					num23 = 0f;
				}
				float num24 = (!m_componentCreature.ComponentLocomotion.IsCreativeFlyEnabled) ? 1 : 4;
				num4 += MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(num2)) + num12 + num14 + num20;
				num5 += MathUtils.Lerp(0f, num24 * 0.15f, SimplexNoise.Noise(1.1f * num2 + 100f)) + num15 + num21;
				num6 += num9 + MathUtils.Lerp(-0.1f, 0.1f, SimplexNoise.Noise(0.9f * num2 + 200f)) + num13 + num16 + num22;
				num7 += 0f - MathUtils.Lerp(0f, num24 * 0.15f, SimplexNoise.Noise(1.05f * num2 + 300f)) + num17 + num23;
				float s = MathUtils.Min(12f * m_subsystemTime.GameTimeDelta, 1f);
				m_headAngles += s * (new Vector2(x, y) - m_headAngles);
				m_handAngles1 += s * (new Vector2(num4, num5) - m_handAngles1);
				m_handAngles2 += s * (new Vector2(num6, num7) - m_handAngles2);
				m_legAngles1 += s * (new Vector2(num3, y2) - m_legAngles1);
				m_legAngles2 += s * (new Vector2(x2, y3) - m_legAngles2);
				SetBoneTransform(m_bodyBone.Index, Matrix.CreateRotationY(vector.X) * Matrix.CreateTranslation(position));
				SetBoneTransform(m_headBone.Index, Matrix.CreateRotationX(m_headAngles.Y) * Matrix.CreateRotationZ(0f - m_headAngles.X));
				SetBoneTransform(m_hand1Bone.Index, Matrix.CreateRotationY(m_handAngles1.Y) * Matrix.CreateRotationX(m_handAngles1.X));
				SetBoneTransform(m_hand2Bone.Index, Matrix.CreateRotationY(m_handAngles2.Y) * Matrix.CreateRotationX(m_handAngles2.X));
				SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationY(m_legAngles1.Y) * Matrix.CreateRotationX(m_legAngles1.X));
				SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationY(m_legAngles2.Y) * Matrix.CreateRotationX(m_legAngles2.X));
			}
			else
			{
				float num25 = MathUtils.Max(base.DeathPhase, m_lieDownFactorModel);
				float num26 = 1f - num25;
				Vector3 position2 = position + num25 * 0.5f * m_componentCreature.ComponentBody.BoxSize.Y * Vector3.Normalize(m_componentCreature.ComponentBody.Matrix.Forward * new Vector3(1f, 0f, 1f)) + num25 * Vector3.UnitY * m_componentCreature.ComponentBody.BoxSize.Z * 0.1f;
				SetBoneTransform(m_bodyBone.Index, Matrix.CreateFromYawPitchRoll(vector.X, (float)Math.PI / 2f * num25, 0f) * Matrix.CreateTranslation(position2));
				SetBoneTransform(m_headBone.Index, Matrix.Identity);
				SetBoneTransform(m_hand1Bone.Index, Matrix.CreateRotationY(m_handAngles1.Y * num26) * Matrix.CreateRotationX(m_handAngles1.X * num26));
				SetBoneTransform(m_hand2Bone.Index, Matrix.CreateRotationY(m_handAngles2.Y * num26) * Matrix.CreateRotationX(m_handAngles2.X * num26));
				SetBoneTransform(m_leg1Bone.Index, Matrix.CreateRotationY(m_legAngles1.Y * num26) * Matrix.CreateRotationX(m_legAngles1.X * num26));
				SetBoneTransform(m_leg2Bone.Index, Matrix.CreateRotationY(m_legAngles2.Y * num26) * Matrix.CreateRotationX(m_legAngles2.X * num26));
			}
			base.Animate();
		}

		public override void DrawExtras(Camera camera)
		{
			if (m_componentCreature.ComponentHealth.Health > 0f && m_componentMiner != null && m_componentMiner.ActiveBlockValue != 0)
			{
				int num = Terrain.ExtractContents(m_componentMiner.ActiveBlockValue);
				Block block = BlocksManager.Blocks[num];
				Matrix m = base.AbsoluteBoneTransformsForCamera[m_hand2Bone.Index];
				m *= camera.InvertedViewMatrix;
				m.Right = Vector3.Normalize(m.Right);
				m.Up = Vector3.Normalize(m.Up);
				m.Forward = Vector3.Normalize(m.Forward);
				Matrix matrix = Matrix.CreateRotationY(MathUtils.DegToRad(block.InHandRotation.Y) + m_inHandItemRotation.Y) * Matrix.CreateRotationZ(MathUtils.DegToRad(block.InHandRotation.Z) + m_inHandItemRotation.Z) * Matrix.CreateRotationX(MathUtils.DegToRad(block.InHandRotation.X) + m_inHandItemRotation.X) * Matrix.CreateTranslation(block.InHandOffset + m_inHandItemOffset) * Matrix.CreateTranslation(new Vector3(0.05f, 0.05f, -0.56f) * (m_componentCreature.ComponentBody.BoxSize.Y / 1.77f)) * m;
				int x = Terrain.ToCell(matrix.Translation.X);
				int y = Terrain.ToCell(matrix.Translation.Y);
				int z = Terrain.ToCell(matrix.Translation.Z);
				m_drawBlockEnvironmentData.InWorldMatrix = matrix;
				m_drawBlockEnvironmentData.Humidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
				m_drawBlockEnvironmentData.Temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
				m_drawBlockEnvironmentData.Light = m_subsystemTerrain.Terrain.GetCellLight(x, y, z);
				m_drawBlockEnvironmentData.BillboardDirection = -Vector3.UnitZ;
				m_drawBlockEnvironmentData.SubsystemTerrain = m_subsystemTerrain;
				Matrix matrix2 = matrix * camera.ViewMatrix;
				block.DrawBlock(m_subsystemModelsRenderer.PrimitivesRenderer, m_componentMiner.ActiveBlockValue, Color.White, block.InHandScale, ref matrix2, m_drawBlockEnvironmentData);
			}
			if (m_componentPlayer != null && camera.GameWidget.PlayerData != m_componentPlayer.PlayerData)
			{
				Vector3 position = Vector3.Transform(m_componentCreature.ComponentBody.Position + 1.02f * Vector3.UnitY * m_componentCreature.ComponentBody.BoxSize.Y, camera.ViewMatrix);
				if (position.Z < 0f)
				{
					Color color = Color.Lerp(Color.White, Color.Transparent, MathUtils.Saturate((position.Length() - 4f) / 3f));
					if (color.A > 8)
					{
						Vector3 right = Vector3.TransformNormal(0.005f * Vector3.Normalize(Vector3.Cross(camera.ViewDirection, Vector3.UnitY)), camera.ViewMatrix);
						Vector3 down = Vector3.TransformNormal(-0.005f * Vector3.UnitY, camera.ViewMatrix);
						BitmapFont font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
						m_subsystemModelsRenderer.PrimitivesRenderer.FontBatch(font, 1, DepthStencilState.DepthRead, RasterizerState.CullNoneScissor, BlendState.AlphaBlend, SamplerState.LinearClamp).QueueText(m_componentPlayer.PlayerData.Name, position, right, down, color, TextAnchor.HorizontalCenter | TextAnchor.Bottom);
					}
				}
			}
			base.DrawExtras(camera);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemModelsRenderer = base.Project.FindSubsystem<SubsystemModelsRenderer>(throwOnError: true);
			m_subsystemNoise = base.Project.FindSubsystem<SubsystemNoise>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_componentMiner = base.Entity.FindComponent<ComponentMiner>();
			m_componentRider = base.Entity.FindComponent<ComponentRider>();
			m_componentSleep = base.Entity.FindComponent<ComponentSleep>();
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>();
			m_walkAnimationSpeed = valuesDictionary.GetValue<float>("WalkAnimationSpeed");
			m_walkBobHeight = valuesDictionary.GetValue<float>("WalkBobHeight");
			m_walkLegsAngle = valuesDictionary.GetValue<float>("WalkLegsAngle");
		}

		protected override void SetModel(Model model)
		{
			base.SetModel(model);
			if (base.Model != null)
			{
				m_bodyBone = base.Model.FindBone("Body");
				m_headBone = base.Model.FindBone("Head");
				m_leg1Bone = base.Model.FindBone("Leg1");
				m_leg2Bone = base.Model.FindBone("Leg2");
				m_hand1Bone = base.Model.FindBone("Hand1");
				m_hand2Bone = base.Model.FindBone("Hand2");
			}
			else
			{
				m_bodyBone = null;
				m_headBone = null;
				m_leg1Bone = null;
				m_leg2Bone = null;
				m_hand1Bone = null;
				m_hand2Bone = null;
			}
		}

		protected override Vector3 CalculateEyePosition()
		{
			float f = MathUtils.Sigmoid(m_lieDownFactorEye, 1f);
			float num = MathUtils.Sigmoid(m_sneakFactor, 4f);
			float num2 = 0.875f * m_componentCreature.ComponentBody.BoxSize.Y;
			float num3 = MathUtils.Lerp(MathUtils.Lerp(num2, 0.45f * num2, num), 0.2f * num2, f);
			Matrix matrix = m_componentCreature.ComponentBody.Matrix;
			return m_componentCreature.ComponentBody.Position + matrix.Up * (num3 + 2f * base.Bob) + matrix.Forward * -0.2f * num;
		}

		protected override Quaternion CalculateEyeRotation()
		{
			float num = 0f;
			if (m_lieDownFactorEye != 0f)
			{
				num += MathUtils.DegToRad(80f) * MathUtils.Sigmoid(MathUtils.Max(m_lieDownFactorEye - 0.2f, 0f) / 0.8f, 4f);
			}
			return m_componentCreature.ComponentBody.Rotation * Quaternion.CreateFromYawPitchRoll(0f - m_componentCreature.ComponentLocomotion.LookAngles.X, m_componentCreature.ComponentLocomotion.LookAngles.Y, num);
		}
	}
}
