using Engine;
using Engine.Graphics;

namespace Game
{
	public class ScrollPanelWidget : ContainerWidget
	{
		public Vector2? m_lastDragPosition;

		public float m_dragSpeed;

		public float m_scrollBarAlpha;

		public float m_scrollAreaLength;

		public virtual LayoutDirection Direction
		{
			get;
			set;
		}

		public virtual float ScrollPosition
		{
			get;
			set;
		}

		public virtual float ScrollSpeed
		{
			get;
			set;
		}

		public ScrollPanelWidget()
		{
			base.ClampToBounds = true;
			StartInitialScroll();
		}

		public void StartInitialScroll()
		{
			ScrollPosition = 12f;
			ScrollSpeed = -70f;
		}

		public virtual float CalculateScrollAreaLength()
		{
			float num = 0f;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					num = ((Direction != 0) ? MathUtils.Max(num, child.ParentDesiredSize.Y + 2f * child.Margin.Y) : MathUtils.Max(num, child.ParentDesiredSize.X + 2f * child.Margin.X));
				}
			}
			return num;
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = true;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					if (Direction == LayoutDirection.Horizontal)
					{
						child.Measure(new Vector2(3.40282347E+38f, MathUtils.Max(parentAvailableSize.Y - 2f * child.Margin.Y, 0f)));
					}
					else
					{
						child.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - 2f * child.Margin.X, 0f), 3.40282347E+38f));
					}
				}
			}
		}

		public override void ArrangeOverride()
		{
			foreach (Widget child in Children)
			{
				Vector2 zero = Vector2.Zero;
				Vector2 actualSize = base.ActualSize;
				if (Direction == LayoutDirection.Horizontal)
				{
					zero.X -= ScrollPosition;
					actualSize.X = zero.X + child.ParentDesiredSize.X;
				}
				else
				{
					zero.Y -= ScrollPosition;
					actualSize.Y = zero.Y + child.ParentDesiredSize.Y;
				}
				ContainerWidget.ArrangeChildWidgetInCell(zero, actualSize, child);
			}
		}

		public override void Update()
		{
			float num = 50f;
			m_scrollAreaLength = CalculateScrollAreaLength();
			m_scrollBarAlpha = MathUtils.Max(m_scrollBarAlpha - 2f * Time.FrameDuration, 0f);
			if (base.Input.Tap.HasValue && HitTestPanel(base.Input.Tap.Value))
			{
				m_lastDragPosition = ScreenToWidget(base.Input.Tap.Value);
			}
			if (m_lastDragPosition.HasValue)
			{
				if (base.Input.Press.HasValue)
				{
					float num2 = 0f;
					Vector2 vector = ScreenToWidget(base.Input.Press.Value);
					Vector2 vector2 = vector - m_lastDragPosition.Value;
					if (Direction == LayoutDirection.Horizontal)
					{
						ScrollPosition += 0f - vector2.X;
						num2 = vector2.X / Time.FrameDuration;
					}
					else
					{
						ScrollPosition += 0f - vector2.Y;
						num2 = vector2.Y / Time.FrameDuration;
					}
					float num3 = (MathUtils.Abs(num2) < MathUtils.Abs(m_dragSpeed)) ? 20f : 16f;
					m_dragSpeed += MathUtils.Saturate(num3 * Time.FrameDuration) * (num2 - m_dragSpeed);
					m_scrollBarAlpha = 4f;
					m_lastDragPosition = vector;
					ScrollSpeed = 0f;
				}
				else
				{
					ScrollSpeed = 0f - m_dragSpeed;
					m_dragSpeed = 0f;
					m_lastDragPosition = null;
				}
			}
			if (ScrollSpeed != 0f)
			{
				ScrollSpeed *= MathUtils.Pow(0.33f, Time.FrameDuration);
				if (MathUtils.Abs(ScrollSpeed) < 40f)
				{
					ScrollSpeed = 0f;
				}
				ScrollPosition += ScrollSpeed * Time.FrameDuration;
				m_scrollBarAlpha = 3f;
			}
			if (base.Input.Scroll.HasValue && HitTestPanel(base.Input.Scroll.Value.XY))
			{
				ScrollPosition -= 40f * base.Input.Scroll.Value.Z;
				ScrollSpeed = 0f;
				num = 0f;
				m_scrollBarAlpha = 3f;
			}
			float num4 = MathUtils.Max(m_scrollAreaLength - base.ActualSize.Y, 0f);
			if (ScrollPosition < 0f)
			{
				if (!m_lastDragPosition.HasValue)
				{
					ScrollPosition = MathUtils.Min(ScrollPosition + 6f * Time.FrameDuration * (0f - ScrollPosition + 5f), 0f);
				}
				ScrollPosition = MathUtils.Max(ScrollPosition, 0f - num);
				ScrollSpeed = 0f;
			}
			if (ScrollPosition > num4)
			{
				if (!m_lastDragPosition.HasValue)
				{
					ScrollPosition = MathUtils.Max(ScrollPosition + 6f * Time.FrameDuration * (num4 - ScrollPosition - 5f), num4);
				}
				ScrollPosition = MathUtils.Min(ScrollPosition, num4 + num);
				ScrollSpeed = 0f;
			}
			if (m_lastDragPosition.HasValue && (base.Input.Drag.HasValue || base.Input.Hold.HasValue))
			{
				base.Input.Clear();
			}
		}

		public override void Draw(DrawContext dc)
		{
			Color color = new Color((byte)128, (byte)128, (byte)128) * base.GlobalColorTransform * MathUtils.Saturate(m_scrollBarAlpha);
			if (color.A > 0 && m_scrollAreaLength > 0f)
			{
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				int count = flatBatch2D.TriangleVertices.Count;
				if (Direction == LayoutDirection.Horizontal)
				{
					float scrollPosition = ScrollPosition;
					float x = base.ActualSize.X;
					Vector2 corner = new Vector2(scrollPosition / m_scrollAreaLength * x, base.ActualSize.Y - 5f);
					Vector2 corner2 = new Vector2((scrollPosition + x) / m_scrollAreaLength * x, base.ActualSize.Y - 1f);
					flatBatch2D.QueueQuad(corner, corner2, 0f, color);
				}
				else
				{
					float scrollPosition2 = ScrollPosition;
					float y = base.ActualSize.Y;
					Vector2 corner3 = new Vector2(base.ActualSize.X - 5f, scrollPosition2 / m_scrollAreaLength * y);
					Vector2 corner4 = new Vector2(base.ActualSize.X - 1f, (scrollPosition2 + y) / m_scrollAreaLength * y);
					flatBatch2D.QueueQuad(corner3, corner4, 0f, color);
				}
				flatBatch2D.TransformTriangles(base.GlobalTransform, count);
			}
		}

		protected bool HitTestPanel(Vector2 position)
		{
			bool found = false;
			HitTestGlobal(position, delegate(Widget widget)
			{
				found = (widget.IsChildWidgetOf(this) || widget == this);
				return true;
			});
			return found;
		}
	}
}
