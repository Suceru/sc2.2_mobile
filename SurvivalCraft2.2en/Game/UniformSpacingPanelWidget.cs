using Engine;

namespace Game
{
	public class UniformSpacingPanelWidget : ContainerWidget
	{
		public LayoutDirection m_direction;

		public int m_count;

		public LayoutDirection Direction
		{
			get
			{
				return m_direction;
			}
			set
			{
				m_direction = value;
			}
		}

		public override void ArrangeOverride()
		{
			Vector2 zero = Vector2.Zero;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					if (m_direction == LayoutDirection.Horizontal)
					{
						float num = (m_count > 0) ? (base.ActualSize.X / (float)m_count) : 0f;
						ContainerWidget.ArrangeChildWidgetInCell(zero, new Vector2(zero.X + num, zero.Y + base.ActualSize.Y), child);
						zero.X += num;
					}
					else
					{
						float num2 = (m_count > 0) ? (base.ActualSize.Y / (float)m_count) : 0f;
						ContainerWidget.ArrangeChildWidgetInCell(zero, new Vector2(zero.X + base.ActualSize.X, zero.Y + num2), child);
						zero.Y += num2;
					}
				}
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			m_count = 0;
			foreach (Widget child in Children)
			{
				if (child.IsVisible)
				{
					m_count++;
				}
			}
			parentAvailableSize = ((m_direction != 0) ? Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X, parentAvailableSize.Y / (float)m_count)) : Vector2.Min(parentAvailableSize, new Vector2(parentAvailableSize.X / (float)m_count, parentAvailableSize.Y)));
			float num = 0f;
			foreach (Widget child2 in Children)
			{
				if (child2.IsVisible)
				{
					child2.Measure(Vector2.Max(parentAvailableSize - 2f * child2.Margin, Vector2.Zero));
					num = ((m_direction != 0) ? MathUtils.Max(num, child2.ParentDesiredSize.X + 2f * child2.Margin.X) : MathUtils.Max(num, child2.ParentDesiredSize.Y + 2f * child2.Margin.Y));
				}
			}
			if (m_direction == LayoutDirection.Horizontal)
			{
				base.DesiredSize = new Vector2(1f / 0f, num);
			}
			else
			{
				base.DesiredSize = new Vector2(num, 1f / 0f);
			}
		}
	}
}
