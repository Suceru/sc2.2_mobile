using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;

namespace Game
{
	public class ListPanelWidget : ScrollPanelWidget
	{
		public List<object> m_items = new List<object>();

		public int? m_selectedItemIndex;

		public Dictionary<int, Widget> m_widgetsByIndex = new Dictionary<int, Widget>();

		public int m_firstVisibleIndex;

		public int m_lastVisibleIndex;

		public float m_itemSize;

		public bool m_widgetsDirty;

		public bool m_clickAllowed;

		public Vector2 lastActualSize = new Vector2(-1f);

		public Func<object, Widget> ItemWidgetFactory
		{
			get;
			set;
		}

		public override LayoutDirection Direction
		{
			get
			{
				return base.Direction;
			}
			set
			{
				if (value != Direction)
				{
					base.Direction = value;
					m_widgetsDirty = true;
				}
			}
		}

		public override float ScrollPosition
		{
			get
			{
				return base.ScrollPosition;
			}
			set
			{
				if (value != ScrollPosition)
				{
					base.ScrollPosition = value;
					m_widgetsDirty = true;
				}
			}
		}

		public float ItemSize
		{
			get
			{
				return m_itemSize;
			}
			set
			{
				if (value != m_itemSize)
				{
					m_itemSize = value;
					m_widgetsDirty = true;
				}
			}
		}

		public int? SelectedIndex
		{
			get
			{
				return m_selectedItemIndex;
			}
			set
			{
				if (value.HasValue && (value.Value < 0 || value.Value >= m_items.Count))
				{
					value = null;
				}
				if (value != m_selectedItemIndex)
				{
					m_selectedItemIndex = value;
					if (this.SelectionChanged != null)
					{
						this.SelectionChanged();
					}
				}
			}
		}

		public object SelectedItem
		{
			get
			{
				if (!m_selectedItemIndex.HasValue)
				{
					return null;
				}
				return m_items[m_selectedItemIndex.Value];
			}
			set
			{
				int num = m_items.IndexOf(value);
				SelectedIndex = ((num >= 0) ? new int?(num) : null);
			}
		}

		public ReadOnlyList<object> Items => new ReadOnlyList<object>(m_items);

		public Color SelectionColor
		{
			get;
			set;
		}

		public event Action<object> ItemClicked;

		public event Action SelectionChanged;

		public ListPanelWidget()
		{
			SelectionColor = Color.Gray;
			ItemWidgetFactory = ((object item) => new LabelWidget
			{
				Text = ((item != null) ? item.ToString() : string.Empty),
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			});
			ItemSize = 48f;
		}

		public void AddItem(object item)
		{
			m_items.Add(item);
			m_widgetsDirty = true;
		}

		public void RemoveItem(object item)
		{
			int num = m_items.IndexOf(item);
			if (num >= 0)
			{
				RemoveItemAt(num);
			}
		}

		public void RemoveItemAt(int index)
		{
			_ = m_items[index];
			m_items.RemoveAt(index);
			m_widgetsByIndex.Clear();
			m_widgetsDirty = true;
			if (index == SelectedIndex)
			{
				SelectedIndex = null;
			}
		}

		public void ClearItems()
		{
			m_items.Clear();
			m_widgetsByIndex.Clear();
			m_widgetsDirty = true;
			SelectedIndex = null;
		}

		public override float CalculateScrollAreaLength()
		{
			return (float)Items.Count * ItemSize;
		}

		public void ScrollToItem(object item)
		{
			int num = m_items.IndexOf(item);
			if (num >= 0)
			{
				float num2 = (float)num * ItemSize;
				float num3 = (Direction == LayoutDirection.Horizontal) ? base.ActualSize.X : base.ActualSize.Y;
				if (num2 < ScrollPosition)
				{
					ScrollPosition = num2;
				}
				else if (num2 > ScrollPosition + num3 - ItemSize)
				{
					ScrollPosition = num2 - num3 + ItemSize;
				}
			}
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
						child.Measure(new Vector2(ItemSize, MathUtils.Max(parentAvailableSize.Y - 2f * child.Margin.Y, 0f)));
					}
					else
					{
						child.Measure(new Vector2(MathUtils.Max(parentAvailableSize.X - 2f * child.Margin.X, 0f), ItemSize));
					}
				}
			}
			if (m_widgetsDirty)
			{
				m_widgetsDirty = false;
				CreateListWidgets((Direction == LayoutDirection.Horizontal) ? base.ActualSize.X : base.ActualSize.Y);
			}
		}

		public override void ArrangeOverride()
		{
			if (base.ActualSize != lastActualSize)
			{
				m_widgetsDirty = true;
			}
			lastActualSize = base.ActualSize;
			int num = m_firstVisibleIndex;
			foreach (Widget child in Children)
			{
				if (Direction == LayoutDirection.Horizontal)
				{
					Vector2 vector = new Vector2((float)num * ItemSize - ScrollPosition, 0f);
					ContainerWidget.ArrangeChildWidgetInCell(vector, vector + new Vector2(ItemSize, base.ActualSize.Y), child);
				}
				else
				{
					Vector2 vector2 = new Vector2(0f, (float)num * ItemSize - ScrollPosition);
					ContainerWidget.ArrangeChildWidgetInCell(vector2, vector2 + new Vector2(base.ActualSize.X, ItemSize), child);
				}
				num++;
			}
		}

		public override void Update()
		{
			bool flag = ScrollSpeed != 0f;
			base.Update();
			if (base.Input.Tap.HasValue && HitTestPanel(base.Input.Tap.Value))
			{
				m_clickAllowed = !flag;
			}
			if (base.Input.Click.HasValue && m_clickAllowed && HitTestPanel(base.Input.Click.Value.Start) && HitTestPanel(base.Input.Click.Value.End))
			{
				int num = PositionToItemIndex(base.Input.Click.Value.End);
				if (this.ItemClicked != null && num >= 0 && num < m_items.Count)
				{
					this.ItemClicked(Items[num]);
				}
				SelectedIndex = num;
				if (SelectedIndex.HasValue)
				{
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
				}
			}
		}

		public override void Draw(DrawContext dc)
		{
			if (SelectedIndex.HasValue && SelectedIndex.Value >= m_firstVisibleIndex && SelectedIndex.Value <= m_lastVisibleIndex)
			{
				Vector2 vector = (Direction == LayoutDirection.Horizontal) ? new Vector2((float)SelectedIndex.Value * ItemSize - ScrollPosition, 0f) : new Vector2(0f, (float)SelectedIndex.Value * ItemSize - ScrollPosition);
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				int count = flatBatch2D.TriangleVertices.Count;
				Vector2 v = (Direction == LayoutDirection.Horizontal) ? new Vector2(ItemSize, base.ActualSize.Y) : new Vector2(base.ActualSize.X, ItemSize);
				flatBatch2D.QueueQuad(vector, vector + v, 0f, SelectionColor * base.GlobalColorTransform);
				flatBatch2D.TransformTriangles(base.GlobalTransform, count);
			}
			base.Draw(dc);
		}

		public int PositionToItemIndex(Vector2 position)
		{
			Vector2 vector = ScreenToWidget(position);
			if (Direction == LayoutDirection.Horizontal)
			{
				return (int)((vector.X + ScrollPosition) / ItemSize);
			}
			return (int)((vector.Y + ScrollPosition) / ItemSize);
		}

		public void CreateListWidgets(float size)
		{
			Children.Clear();
			if (m_items.Count <= 0)
			{
				return;
			}
			int x = (int)MathUtils.Floor(ScrollPosition / ItemSize);
			int x2 = (int)MathUtils.Floor((ScrollPosition + size) / ItemSize);
			m_firstVisibleIndex = MathUtils.Max(x, 0);
			m_lastVisibleIndex = MathUtils.Min(x2, m_items.Count - 1);
			for (int i = m_firstVisibleIndex; i <= m_lastVisibleIndex; i++)
			{
				object obj = m_items[i];
				if (!m_widgetsByIndex.TryGetValue(i, out Widget value))
				{
					value = ItemWidgetFactory(obj);
					value.Tag = obj;
					m_widgetsByIndex.Add(i, value);
				}
				Children.Add(value);
			}
		}
	}
}
