using Engine;
using Engine.Graphics;

namespace Game
{
	public class ParticleSystem<T> : ParticleSystemBase where T : Particle, new()
	{
		public T[] m_particles;

		public Texture2D m_texture;

		public Vector3[] m_front = new Vector3[3];

		public Vector3[] m_right = new Vector3[3];

		public Vector3[] m_up = new Vector3[3];

		public TexturedBatch3D AdditiveBatch;

		public TexturedBatch3D AlphaBlendedBatch;

		protected T[] Particles => m_particles;

		protected Texture2D Texture
		{
			get
			{
				return m_texture;
			}
			set
			{
				if (value != m_texture)
				{
					m_texture = value;
					AdditiveBatch = null;
					AlphaBlendedBatch = null;
				}
			}
		}

		protected int TextureSlotsCount
		{
			get;
			set;
		}

		protected ParticleSystem(int particlesCount)
		{
			m_particles = new T[particlesCount];
			for (int i = 0; i < m_particles.Length; i++)
			{
				m_particles[i] = new T();
			}
		}

		public override void Draw(Camera camera)
		{
			if (AdditiveBatch == null || AlphaBlendedBatch == null)
			{
				AdditiveBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: true, 0, DepthStencilState.DepthRead, null, BlendState.Additive, SamplerState.PointClamp);
				AlphaBlendedBatch = SubsystemParticles.PrimitivesRenderer.TexturedBatch(m_texture, useAlphaTest: true, 0, DepthStencilState.Default, null, BlendState.AlphaBlend, SamplerState.PointClamp);
			}
			m_front[0] = camera.ViewDirection;
			m_right[0] = Vector3.Normalize(Vector3.Cross(m_front[0], Vector3.UnitY));
			m_up[0] = Vector3.Normalize(Vector3.Cross(m_right[0], m_front[0]));
			m_front[1] = camera.ViewDirection;
			m_right[1] = Vector3.Normalize(Vector3.Cross(m_front[1], Vector3.UnitY));
			m_up[1] = Vector3.UnitY;
			m_front[2] = Vector3.UnitY;
			m_right[2] = Vector3.UnitX;
			m_up[2] = Vector3.UnitZ;
			float s = 1f / (float)TextureSlotsCount;
			for (int i = 0; i < m_particles.Length; i++)
			{
				Particle particle = m_particles[i];
				if (particle.IsActive)
				{
					Vector3 position = particle.Position;
					Vector2 size = particle.Size;
					float rotation = particle.Rotation;
					int textureSlot = particle.TextureSlot;
					int billboardingMode = (int)particle.BillboardingMode;
					Vector3 p;
					Vector3 p2;
					Vector3 p3;
					Vector3 p4;
					if (rotation != 0f)
					{
						Vector3 v = (m_front[billboardingMode].X * m_front[billboardingMode].X > m_front[billboardingMode].Z * m_front[billboardingMode].Z) ? new Vector3(0f, MathUtils.Cos(rotation), MathUtils.Sin(rotation)) : new Vector3(MathUtils.Sin(rotation), MathUtils.Cos(rotation), 0f);
						Vector3 vector = Vector3.Normalize(Vector3.Cross(m_front[(uint)particle.BillboardingMode], v));
						v = Vector3.Normalize(Vector3.Cross(m_front[(uint)particle.BillboardingMode], vector));
						vector *= size.Y;
						v *= size.X;
						p = position + (-vector - v);
						p2 = position + (vector - v);
						p3 = position + (vector + v);
						p4 = position + (-vector + v);
					}
					else
					{
						Vector3 vector2 = m_right[billboardingMode] * size.X;
						Vector3 v2 = m_up[billboardingMode] * size.Y;
						p = position + (-vector2 - v2);
						p2 = position + (vector2 - v2);
						p3 = position + (vector2 + v2);
						p4 = position + (-vector2 + v2);
					}
					TexturedBatch3D obj = particle.UseAdditiveBlending ? AdditiveBatch : AlphaBlendedBatch;
					Vector2 v3 = new Vector2(textureSlot % TextureSlotsCount, textureSlot / TextureSlotsCount);
					float num = 0f;
					float num2 = 1f;
					float num3 = 1f;
					float num4 = 0f;
					if (particle.FlipX)
					{
						num = 1f - num;
						num2 = 1f - num2;
					}
					if (particle.FlipY)
					{
						num3 = 1f - num3;
						num4 = 1f - num4;
					}
					Vector2 texCoord = (v3 + new Vector2(num, num3)) * s;
					Vector2 texCoord2 = (v3 + new Vector2(num2, num3)) * s;
					Vector2 texCoord3 = (v3 + new Vector2(num2, num4)) * s;
					Vector2 texCoord4 = (v3 + new Vector2(num, num4)) * s;
					obj.QueueQuad(p, p2, p3, p4, texCoord, texCoord2, texCoord3, texCoord4, particle.Color);
				}
			}
		}

		public override bool Simulate(float dt)
		{
			return false;
		}
	}
}
