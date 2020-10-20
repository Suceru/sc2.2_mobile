using Engine;
using Engine.Graphics;

namespace Game
{
	public class RectangleWidget : Widget
	{
		public Subtexture m_subtexture;

		public bool m_textureWrap;

		public bool m_textureLinearFilter;

		public bool m_depthWriteEnabled;

		public Vector2 Size
		{
			get;
			set;
		}

		public float Depth
		{
			get;
			set;
		}

		public bool DepthWriteEnabled
		{
			get
			{
				return m_depthWriteEnabled;
			}
			set
			{
				if (value != m_depthWriteEnabled)
				{
					m_depthWriteEnabled = value;
				}
			}
		}

		public Subtexture Subtexture
		{
			get
			{
				return m_subtexture;
			}
			set
			{
				if (value != m_subtexture)
				{
					m_subtexture = value;
				}
			}
		}

		public bool TextureWrap
		{
			get
			{
				return m_textureWrap;
			}
			set
			{
				if (value != m_textureWrap)
				{
					m_textureWrap = value;
				}
			}
		}

		public bool TextureLinearFilter
		{
			get
			{
				return m_textureLinearFilter;
			}
			set
			{
				if (value != m_textureLinearFilter)
				{
					m_textureLinearFilter = value;
				}
			}
		}

		public bool FlipHorizontal
		{
			get;
			set;
		}

		public bool FlipVertical
		{
			get;
			set;
		}

		public Color FillColor
		{
			get;
			set;
		}

		public Color OutlineColor
		{
			get;
			set;
		}

		public float OutlineThickness
		{
			get;
			set;
		}

		public Vector2 Texcoord1
		{
			get;
			set;
		}

		public Vector2 Texcoord2
		{
			get;
			set;
		}

		public RectangleWidget()
		{
			Size = new Vector2(1f / 0f);
			TextureLinearFilter = true;
			FillColor = Color.Black;
			OutlineColor = Color.White;
			OutlineThickness = 1f;
			IsHitTestVisible = false;
			Texcoord1 = Vector2.Zero;
			Texcoord2 = Vector2.One;
		}

		public override void Draw(DrawContext dc)
		{
			if (FillColor.A == 0 && (OutlineColor.A == 0 || OutlineThickness <= 0f))
			{
				return;
			}
			DepthStencilState depthStencilState = DepthWriteEnabled ? DepthStencilState.DepthWrite : DepthStencilState.None;
			Matrix m = base.GlobalTransform;
			Vector2 v = Vector2.Zero;
			Vector2 v2 = new Vector2(base.ActualSize.X, 0f);
			Vector2 v3 = base.ActualSize;
			Vector2 v4 = new Vector2(0f, base.ActualSize.Y);
			Vector2.Transform(ref v, ref m, out Vector2 result);
			Vector2.Transform(ref v2, ref m, out Vector2 result2);
			Vector2.Transform(ref v3, ref m, out Vector2 result3);
			Vector2.Transform(ref v4, ref m, out Vector2 result4);
			Color color = FillColor * base.GlobalColorTransform;
			if (color.A != 0)
			{
				if (Subtexture != null)
				{
					SamplerState samplerState = (!TextureWrap) ? (TextureLinearFilter ? SamplerState.LinearClamp : SamplerState.PointClamp) : (TextureLinearFilter ? SamplerState.LinearWrap : SamplerState.PointWrap);
					TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Subtexture.Texture, useAlphaTest: false, 0, depthStencilState, null, null, samplerState);
					Vector2 zero = default(Vector2);
					Vector2 texCoord;
					Vector2 texCoord2 = default(Vector2);
					Vector2 texCoord3;
					if (TextureWrap)
					{
						zero = Vector2.Zero;
						texCoord = new Vector2(base.ActualSize.X / (float)Subtexture.Texture.Width, 0f);
						texCoord2 = new Vector2(base.ActualSize.X / (float)Subtexture.Texture.Width, base.ActualSize.Y / (float)Subtexture.Texture.Height);
						texCoord3 = new Vector2(0f, base.ActualSize.Y / (float)Subtexture.Texture.Height);
					}
					else
					{
						zero.X = MathUtils.Lerp(Subtexture.TopLeft.X, Subtexture.BottomRight.X, Texcoord1.X);
						zero.Y = MathUtils.Lerp(Subtexture.TopLeft.Y, Subtexture.BottomRight.Y, Texcoord1.Y);
						texCoord2.X = MathUtils.Lerp(Subtexture.TopLeft.X, Subtexture.BottomRight.X, Texcoord2.X);
						texCoord2.Y = MathUtils.Lerp(Subtexture.TopLeft.Y, Subtexture.BottomRight.Y, Texcoord2.Y);
						texCoord = new Vector2(texCoord2.X, zero.Y);
						texCoord3 = new Vector2(zero.X, texCoord2.Y);
					}
					if (FlipHorizontal)
					{
						Utilities.Swap(ref zero.X, ref texCoord.X);
						Utilities.Swap(ref texCoord2.X, ref texCoord3.X);
					}
					if (FlipVertical)
					{
						Utilities.Swap(ref zero.Y, ref texCoord2.Y);
						Utilities.Swap(ref texCoord.Y, ref texCoord3.Y);
					}
					texturedBatch2D.QueueQuad(result, result2, result3, result4, Depth, zero, texCoord, texCoord2, texCoord3, color);
				}
				else
				{
					dc.PrimitivesRenderer2D.FlatBatch(1, depthStencilState).QueueQuad(result, result2, result3, result4, Depth, color);
				}
			}
			Color color2 = OutlineColor * base.GlobalColorTransform;
			if (color2.A != 0 && OutlineThickness > 0f)
			{
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, depthStencilState);
				Vector2 vector = Vector2.Normalize(base.GlobalTransform.Right.XY);
				Vector2 v5 = -Vector2.Normalize(base.GlobalTransform.Up.XY);
				int num = (int)MathUtils.Max(MathUtils.Round(OutlineThickness * base.GlobalTransform.Right.Length()), 1f);
				for (int i = 0; i < num; i++)
				{
					flatBatch2D.QueueLine(result, result2, Depth, color2);
					flatBatch2D.QueueLine(result2, result3, Depth, color2);
					flatBatch2D.QueueLine(result3, result4, Depth, color2);
					flatBatch2D.QueueLine(result4, result, Depth, color2);
					result += vector - v5;
					result2 += -vector - v5;
					result3 += -vector + v5;
					result4 += vector + v5;
				}
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = (FillColor.A != 0 || (OutlineColor.A != 0 && OutlineThickness > 0f));
			base.DesiredSize = Size;
		}
	}
}
