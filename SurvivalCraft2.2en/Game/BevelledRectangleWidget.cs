using Engine;
using Engine.Graphics;

namespace Game
{
	public class BevelledRectangleWidget : Widget
	{
		public Texture2D m_texture;

		public bool m_textureLinearFilter;

		public Vector2 Size
		{
			get;
			set;
		}

		public float BevelSize
		{
			get;
			set;
		}

		public float DirectionalLight
		{
			get;
			set;
		}

		public float AmbientLight
		{
			get;
			set;
		}

		public Texture2D Texture
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
				}
			}
		}

		public float TextureScale
		{
			get;
			set;
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

		public Color CenterColor
		{
			get;
			set;
		}

		public Color BevelColor
		{
			get;
			set;
		}

		public Color ShadowColor
		{
			get;
			set;
		}

		public BevelledRectangleWidget()
		{
			Size = new Vector2(1f / 0f);
			BevelSize = 2f;
			AmbientLight = 0.6f;
			DirectionalLight = 0.4f;
			TextureScale = 1f;
			TextureLinearFilter = false;
			CenterColor = new Color(181, 172, 154);
			BevelColor = new Color(181, 172, 154);
			ShadowColor = new Color(0, 0, 0, 80);
			IsHitTestVisible = false;
		}

		public override void Draw(DrawContext dc)
		{
			if (Texture != null)
			{
				SamplerState samplerState = TextureLinearFilter ? SamplerState.LinearWrap : SamplerState.PointWrap;
				FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.None, null, null, samplerState);
				int count = flatBatch2D.TriangleVertices.Count;
				int count2 = texturedBatch2D.TriangleVertices.Count;
				QueueBevelledRectangle(texturedBatch2D, flatBatch2D, Vector2.Zero, base.ActualSize, 0f, BevelSize, CenterColor * base.GlobalColorTransform, BevelColor * base.GlobalColorTransform, ShadowColor * base.GlobalColorTransform, AmbientLight, DirectionalLight, TextureScale);
				flatBatch2D.TransformTriangles(base.GlobalTransform, count);
				texturedBatch2D.TransformTriangles(base.GlobalTransform, count2);
			}
			else
			{
				FlatBatch2D flatBatch2D2 = dc.PrimitivesRenderer2D.FlatBatch(0, DepthStencilState.None);
				int count3 = flatBatch2D2.TriangleVertices.Count;
				QueueBevelledRectangle(null, flatBatch2D2, Vector2.Zero, base.ActualSize, 0f, BevelSize, CenterColor * base.GlobalColorTransform, BevelColor * base.GlobalColorTransform, ShadowColor * base.GlobalColorTransform, AmbientLight, DirectionalLight, 0f);
				flatBatch2D2.TransformTriangles(base.GlobalTransform, count3);
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = (BevelColor.A != 0 || CenterColor.A != 0);
			base.DesiredSize = Size;
		}

		public static void QueueBevelledRectangle(TexturedBatch2D texturedBatch, FlatBatch2D flatBatch, Vector2 c1, Vector2 c2, float depth, float bevelSize, Color color, Color bevelColor, Color shadowColor, float ambientLight, float directionalLight, float textureScale)
		{
			float num = MathUtils.Abs(bevelSize);
			Vector2 vector = c1;
			Vector2 vector2 = c1 + new Vector2(num);
			Vector2 vector3 = c2 - new Vector2(num);
			Vector2 vector4 = c2;
			Vector2 vector5 = c2 + new Vector2(1.5f * num);
			float x = vector.X;
			float x2 = vector2.X;
			float x3 = vector3.X;
			float x4 = vector4.X;
			float x5 = vector5.X;
			float y = vector.Y;
			float y2 = vector2.Y;
			float y3 = vector3.Y;
			float y4 = vector4.Y;
			float y5 = vector5.Y;
			float num2 = MathUtils.Saturate(((bevelSize > 0f) ? 1f : (-0.75f)) * directionalLight + ambientLight);
			float num3 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.75f) : 1f) * directionalLight + ambientLight);
			float num4 = MathUtils.Saturate(((bevelSize > 0f) ? (-0.375f) : 0.5f) * directionalLight + ambientLight);
			float num5 = MathUtils.Saturate(((bevelSize > 0f) ? 0.5f : (-0.375f)) * directionalLight + ambientLight);
			float num6 = MathUtils.Saturate(0f * directionalLight + ambientLight);
			Color color2 = new Color((byte)(num4 * (float)(int)bevelColor.R), (byte)(num4 * (float)(int)bevelColor.G), (byte)(num4 * (float)(int)bevelColor.B), bevelColor.A);
			Color color3 = new Color((byte)(num5 * (float)(int)bevelColor.R), (byte)(num5 * (float)(int)bevelColor.G), (byte)(num5 * (float)(int)bevelColor.B), bevelColor.A);
			Color color4 = new Color((byte)(num2 * (float)(int)bevelColor.R), (byte)(num2 * (float)(int)bevelColor.G), (byte)(num2 * (float)(int)bevelColor.B), bevelColor.A);
			Color color5 = new Color((byte)(num3 * (float)(int)bevelColor.R), (byte)(num3 * (float)(int)bevelColor.G), (byte)(num3 * (float)(int)bevelColor.B), bevelColor.A);
			Color color6 = new Color((byte)(num6 * (float)(int)color.R), (byte)(num6 * (float)(int)color.G), (byte)(num6 * (float)(int)color.B), color.A);
			if (texturedBatch != null)
			{
				float num7 = textureScale / (float)texturedBatch.Texture.Width;
				float num8 = textureScale / (float)texturedBatch.Texture.Height;
				float num9 = x * num7;
				float num10 = y * num8;
				float x6 = num9;
				float x7 = (x2 - x) * num7 + num9;
				float x8 = (x3 - x) * num7 + num9;
				float x9 = (x4 - x) * num7 + num9;
				float y6 = num10;
				float y7 = (y2 - y) * num8 + num10;
				float y8 = (y3 - y) * num8 + num10;
				float y9 = (y4 - y) * num8 + num10;
				if (bevelColor.A > 0)
				{
					texturedBatch.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x4, y), depth, new Vector2(x6, y6), new Vector2(x7, y7), new Vector2(x8, y7), new Vector2(x9, y6), color4);
					texturedBatch.QueueQuad(new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x4, y4), new Vector2(x4, y), depth, new Vector2(x8, y7), new Vector2(x8, y8), new Vector2(x9, y9), new Vector2(x9, y6), color3);
					texturedBatch.QueueQuad(new Vector2(x, y4), new Vector2(x4, y4), new Vector2(x3, y3), new Vector2(x2, y3), depth, new Vector2(x6, y9), new Vector2(x9, y9), new Vector2(x8, y8), new Vector2(x7, y8), color5);
					texturedBatch.QueueQuad(new Vector2(x, y), new Vector2(x, y4), new Vector2(x2, y3), new Vector2(x2, y2), depth, new Vector2(x6, y6), new Vector2(x6, y9), new Vector2(x7, y8), new Vector2(x7, y7), color2);
				}
				if (color6.A > 0)
				{
					texturedBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, new Vector2(x7, y7), new Vector2(x8, y8), color6);
				}
			}
			else if (flatBatch != null)
			{
				if (bevelColor.A > 0)
				{
					flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x2, y2), new Vector2(x3, y2), new Vector2(x4, y), depth, color4);
					flatBatch.QueueQuad(new Vector2(x3, y2), new Vector2(x3, y3), new Vector2(x4, y4), new Vector2(x4, y), depth, color3);
					flatBatch.QueueQuad(new Vector2(x, y4), new Vector2(x4, y4), new Vector2(x3, y3), new Vector2(x2, y3), depth, color5);
					flatBatch.QueueQuad(new Vector2(x, y), new Vector2(x, y4), new Vector2(x2, y3), new Vector2(x2, y2), depth, color2);
				}
				if (color6.A > 0)
				{
					flatBatch.QueueQuad(new Vector2(x2, y2), new Vector2(x3, y3), depth, color6);
				}
			}
			if (bevelSize > 0f && flatBatch != null && shadowColor.A > 0)
			{
				Color color7 = shadowColor;
				Color color8 = new Color(0, 0, 0, 0);
				flatBatch.QueueTriangle(new Vector2(x, y4), new Vector2(x2, y5), new Vector2(x2, y4), depth, color8, color8, color7);
				flatBatch.QueueTriangle(new Vector2(x4, y), new Vector2(x4, y2), new Vector2(x5, y2), depth, color8, color7, color8);
				flatBatch.QueueTriangle(new Vector2(x4, y4), new Vector2(x4, y5), new Vector2(x5, y4), depth, color7, color8, color8);
				flatBatch.QueueQuad(new Vector2(x2, y4), new Vector2(x2, y5), new Vector2(x4, y5), new Vector2(x4, y4), depth, color7, color8, color8, color7);
				flatBatch.QueueQuad(new Vector2(x4, y2), new Vector2(x4, y4), new Vector2(x5, y4), new Vector2(x5, y2), depth, color7, color7, color8, color8);
			}
		}
	}
}
