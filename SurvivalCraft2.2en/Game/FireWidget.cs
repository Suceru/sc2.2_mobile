using Engine;

namespace Game
{
	public class FireWidget : CanvasWidget
	{
		public ScreenSpaceFireRenderer m_fireRenderer = new ScreenSpaceFireRenderer(100);

		public float ParticlesPerSecond
		{
			get
			{
				return m_fireRenderer.ParticlesPerSecond;
			}
			set
			{
				m_fireRenderer.ParticlesPerSecond = value;
			}
		}

		public FireWidget()
		{
			base.ClampToBounds = true;
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = true;
			base.MeasureOverride(parentAvailableSize);
		}

		public override void Draw(DrawContext dc)
		{
			m_fireRenderer.Draw(dc.PrimitivesRenderer2D, 0f, base.GlobalTransform, base.GlobalColorTransform);
		}

		public override void Update()
		{
			float dt = MathUtils.Clamp(Time.FrameDuration, 0f, 0.1f);
			m_fireRenderer.Origin = new Vector2(0f, base.ActualSize.Y);
			m_fireRenderer.CutoffPosition = -1f / 0f;
			m_fireRenderer.ParticleSize = 32f;
			m_fireRenderer.ParticleSpeed = 32f;
			m_fireRenderer.Width = base.ActualSize.X;
			m_fireRenderer.MinTimeToLive = 0.5f;
			m_fireRenderer.MaxTimeToLive = 2f;
			m_fireRenderer.ParticleAnimationPeriod = 1.25f;
			m_fireRenderer.Update(dt);
		}
	}
}
