using Engine;
using Engine.Graphics;

namespace Game
{
	public class StarRatingWidget : Widget
	{
		public Texture2D m_texture;

		public float m_rating;

		public float StarSize
		{
			get;
			set;
		}

		public Color ForeColor
		{
			get;
			set;
		}

		public Color BackColor
		{
			get;
			set;
		}

		public float Rating
		{
			get
			{
				return m_rating;
			}
			set
			{
				m_rating = MathUtils.Clamp(value, 0f, 5f);
			}
		}

		public StarRatingWidget()
		{
			m_texture = ContentManager.Get<Texture2D>("Textures/Gui/RatingStar");
			ForeColor = new Color(255, 192, 0);
			BackColor = new Color(96, 96, 96);
			StarSize = 64f;
		}

		public override void Update()
		{
			if (base.Input.Press.HasValue && HitTestGlobal(base.Input.Press.Value) == this)
			{
				Vector2 vector = ScreenToWidget(base.Input.Press.Value);
				Rating = (int)MathUtils.Floor(5f * vector.X / base.ActualSize.X + 1f);
			}
		}

		public override void Draw(DrawContext dc)
		{
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(m_texture, useAlphaTest: false, 0, DepthStencilState.None, null, null, SamplerState.LinearWrap);
			float x = 0f;
			float x2 = base.ActualSize.X * Rating / 5f;
			float x3 = base.ActualSize.X;
			float y = 0f;
			float y2 = base.ActualSize.Y;
			int count = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), 0f, new Vector2(0f, 0f), new Vector2(Rating, 1f), ForeColor * base.GlobalColorTransform);
			texturedBatch2D.QueueQuad(new Vector2(x2, y), new Vector2(x3, y2), 0f, new Vector2(Rating, 0f), new Vector2(5f, 1f), BackColor * base.GlobalColorTransform);
			texturedBatch2D.TransformTriangles(base.GlobalTransform, count);
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = true;
			base.DesiredSize = new Vector2(5f * StarSize, StarSize);
		}
	}
}
