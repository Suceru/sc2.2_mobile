using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Media;
using System;

namespace Game
{
	public class TextBoxWidget : Widget
	{
		public BitmapFont m_font;

		public string m_text = string.Empty;

		public int m_maximumLength = 32;

		public bool m_hasFocus;

		public int m_caretPosition;

		public double m_focusStartTime;

		public float m_scroll;

		public Vector2? m_size;

		public Vector2 Size
		{
			get
			{
				if (!m_size.HasValue)
				{
					return Vector2.Zero;
				}
				return m_size.Value;
			}
			set
			{
				m_size = value;
			}
		}

		public string Title
		{
			get;
			set;
		}

		public string Description
		{
			get;
			set;
		}

		public string Text
		{
			get
			{
				return m_text;
			}
			set
			{
				string text = (value.Length > MaximumLength) ? value.Substring(0, MaximumLength) : value;
				if (text != m_text)
				{
					m_text = text;
					CaretPosition = CaretPosition;
					this.TextChanged?.Invoke(this);
				}
			}
		}

		public int MaximumLength
		{
			get
			{
				return m_maximumLength;
			}
			set
			{
				m_maximumLength = MathUtils.Max(value, 0);
				if (Text.Length > m_maximumLength)
				{
					Text = Text.Substring(0, m_maximumLength);
				}
			}
		}

		public bool OverwriteMode
		{
			get;
			set;
		}

		public bool HasFocus
		{
			get
			{
				return m_hasFocus;
			}
			set
			{
				if (value != m_hasFocus)
				{
					m_hasFocus = value;
					if (value)
					{
						CaretPosition = m_text.Length;
						Keyboard.ShowKeyboard(Title, Description, Text, passwordMode: false, delegate(string text)
						{
							Text = text;
						}, null);
					}
					else
					{
						this.FocusLost?.Invoke(this);
					}
				}
			}
		}

		public BitmapFont Font
		{
			get
			{
				return m_font;
			}
			set
			{
				m_font = value;
			}
		}

		public float FontScale
		{
			get;
			set;
		}

		public Vector2 FontSpacing
		{
			get;
			set;
		}

		public Color Color
		{
			get;
			set;
		}

		public bool TextureLinearFilter
		{
			get;
			set;
		}

		public int CaretPosition
		{
			get
			{
				return m_caretPosition;
			}
			set
			{
				m_caretPosition = MathUtils.Clamp(value, 0, Text.Length);
				m_focusStartTime = Time.RealTime;
			}
		}

		public event Action<TextBoxWidget> TextChanged;

		public event Action<TextBoxWidget> Enter;

		public event Action<TextBoxWidget> Escape;

		public event Action<TextBoxWidget> FocusLost;

		public TextBoxWidget()
		{
			base.ClampToBounds = true;
			Color = Color.White;
			TextureLinearFilter = true;
			Font = ContentManager.Get<BitmapFont>("Fonts/Pericles");
			FontScale = 1f;
			Title = string.Empty;
			Description = string.Empty;
		}

		public override void Update()
		{
			if (m_hasFocus)
			{
				if (base.Input.LastChar.HasValue && !base.Input.IsKeyDown(Key.Control) && !char.IsControl(base.Input.LastChar.Value))
				{
					EnterText(new string(base.Input.LastChar.Value, 1));
					base.Input.Clear();
				}
				if (base.Input.LastKey.HasValue)
				{
					bool flag = false;
					Key value = base.Input.LastKey.Value;
					if (value == Key.V && base.Input.IsKeyDown(Key.Control))
					{
						EnterText(ClipboardManager.ClipboardString);
						flag = true;
					}
					else if (value == Key.BackSpace && CaretPosition > 0)
					{
						CaretPosition--;
						Text = Text.Remove(CaretPosition, 1);
						flag = true;
					}
					else
					{
						switch (value)
						{
						case Key.Delete:
							if (CaretPosition < m_text.Length)
							{
								Text = Text.Remove(CaretPosition, 1);
								flag = true;
							}
							break;
						case Key.LeftArrow:
							CaretPosition--;
							flag = true;
							break;
						case Key.RightArrow:
							CaretPosition++;
							flag = true;
							break;
						case Key.Home:
							CaretPosition = 0;
							flag = true;
							break;
						case Key.End:
							CaretPosition = m_text.Length;
							flag = true;
							break;
						case Key.Enter:
							flag = true;
							HasFocus = false;
							this.Enter?.Invoke(this);
							break;
						case Key.Escape:
							flag = true;
							HasFocus = false;
							this.Escape?.Invoke(this);
							break;
						}
					}
					if (flag)
					{
						base.Input.Clear();
					}
				}
			}
			if (base.Input.Click.HasValue)
			{
				HasFocus = (HitTestGlobal(base.Input.Click.Value.Start) == this && HitTestGlobal(base.Input.Click.Value.End) == this);
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = true;
			if (m_size.HasValue)
			{
				base.DesiredSize = m_size.Value;
				return;
			}
			if (Text.Length == 0)
			{
				base.DesiredSize = Font.MeasureText(" ", new Vector2(FontScale), FontSpacing);
			}
			else
			{
				base.DesiredSize = Font.MeasureText(Text, new Vector2(FontScale), FontSpacing);
			}
			base.DesiredSize += new Vector2(1f * FontScale * Font.Scale, 0f);
		}

		public override void Draw(DrawContext dc)
		{
			Color color = Color * base.GlobalColorTransform;
			if (!string.IsNullOrEmpty(m_text))
			{
				Vector2 position = new Vector2(0f - m_scroll, base.ActualSize.Y / 2f);
				SamplerState samplerState = TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp;
				FontBatch2D fontBatch2D = dc.PrimitivesRenderer2D.FontBatch(Font, 1, DepthStencilState.None, null, null, samplerState);
				int count = fontBatch2D.TriangleVertices.Count;
				fontBatch2D.QueueText(Text, position, 0f, color, TextAnchor.VerticalCenter, new Vector2(FontScale), FontSpacing);
				fontBatch2D.TransformTriangles(base.GlobalTransform, count);
			}
			if (!m_hasFocus || !(MathUtils.Remainder(Time.RealTime - m_focusStartTime, 0.5) < 0.25))
			{
				return;
			}
			float num = Font.CalculateCharacterPosition(Text, CaretPosition, new Vector2(FontScale), FontSpacing);
			Vector2 v = new Vector2(0f, base.ActualSize.Y / 2f) + new Vector2(num - m_scroll, 0f);
			if (m_hasFocus)
			{
				if (v.X < 0f)
				{
					m_scroll = MathUtils.Max(m_scroll + v.X, 0f);
				}
				if (v.X > base.ActualSize.X)
				{
					m_scroll += v.X - base.ActualSize.X + 1f;
				}
			}
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None);
			int count2 = flatBatch2D.TriangleVertices.Count;
			flatBatch2D.QueueQuad(v - new Vector2(0f, Font.GlyphHeight / 2f * FontScale * Font.Scale), v + new Vector2(1f, Font.GlyphHeight / 2f * FontScale * Font.Scale), 0f, color);
			flatBatch2D.TransformTriangles(base.GlobalTransform, count2);
		}

		public void EnterText(string s)
		{
			if (OverwriteMode)
			{
				if (CaretPosition + s.Length <= MaximumLength)
				{
					if (CaretPosition < m_text.Length)
					{
						string text = Text;
						text = text.Remove(CaretPosition, s.Length);
						text = (Text = text.Insert(CaretPosition, s));
					}
					else
					{
						Text = m_text + s;
					}
					CaretPosition += s.Length;
				}
			}
			else if (m_text.Length + s.Length <= MaximumLength)
			{
				if (CaretPosition < m_text.Length)
				{
					Text = Text.Insert(CaretPosition, s);
				}
				else
				{
					Text = m_text + s;
				}
				CaretPosition += s.Length;
			}
		}
	}
}
