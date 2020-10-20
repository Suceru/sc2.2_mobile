using Engine;
using Engine.Graphics;

namespace Game
{
	public class PanoramaWidget : Widget
	{
		public Vector2 m_position;

		public float m_timeOffset;

		public Texture2D Texture
		{
			get;
			set;
		}

		public PanoramaWidget()
		{
			Texture = ContentManager.Get<Texture2D>("Textures/Gui/Panorama");
			m_timeOffset = new Random().Float(0f, 1000f);
		}

		public void DrawImage(DrawContext dc)
		{
			float num = (float)MathUtils.Remainder(Time.FrameStartTime + (double)m_timeOffset, 10000.0);
			float x = 2f * SimplexNoise.OctavedNoise(num, 0.02f, 4, 2f, 0.5f) - 1f;
			float y = 2f * SimplexNoise.OctavedNoise(num + 100f, 0.02f, 4, 2f, 0.5f) - 1f;
			m_position += 0.03f * new Vector2(x, y) * MathUtils.Min(Time.FrameDuration, 0.1f);
			m_position.X = MathUtils.Remainder(m_position.X, 1f);
			m_position.Y = MathUtils.Remainder(m_position.Y, 1f);
			float f = 0.5f * MathUtils.PowSign(MathUtils.Sin(0.21f * num + 2f), 2f) + 0.5f;
			float num2 = MathUtils.Lerp(0.13f, 0.3f, f);
			float num3 = num2 / (float)Texture.Height * (float)Texture.Width / base.ActualSize.X * base.ActualSize.Y;
			float x2 = m_position.X;
			float y2 = m_position.Y;
			Vector2 zero = Vector2.Zero;
			Vector2 actualSize = base.ActualSize;
			Vector2 texCoord = new Vector2(x2 - num2, y2 - num3);
			Vector2 texCoord2 = new Vector2(x2 + num2, y2 + num3);
			TexturedBatch2D texturedBatch2D = dc.PrimitivesRenderer2D.TexturedBatch(Texture, useAlphaTest: false, 0, DepthStencilState.DepthWrite, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
			int count = texturedBatch2D.TriangleVertices.Count;
			texturedBatch2D.QueueQuad(zero, actualSize, 1f, texCoord, texCoord2, base.GlobalColorTransform);
			texturedBatch2D.TransformTriangles(base.GlobalTransform, count);
		}

		public void DrawSquares(DrawContext dc)
		{
			FlatBatch2D flatBatch2D = dc.PrimitivesRenderer2D.FlatBatch(1, DepthStencilState.None, null, BlendState.AlphaBlend);
			int count = flatBatch2D.LineVertices.Count;
			int count2 = flatBatch2D.TriangleVertices.Count;
			float num = (float)MathUtils.Remainder(Time.FrameStartTime + (double)m_timeOffset, 10000.0);
			float num2 = base.ActualSize.X / 12f;
			float num3 = (float)(int)base.GlobalColorTransform.A / 255f;
			for (float num4 = 0f; num4 < base.ActualSize.X; num4 += num2)
			{
				for (float num5 = 0f; num5 < base.ActualSize.Y; num5 += num2)
				{
					float num6 = 0.35f * MathUtils.Pow(MathUtils.Saturate(SimplexNoise.OctavedNoise(num4 + 1000f, num5, 0.7f * num, 0.5f, 1, 2f, 1f) - 0.1f), 1f) * num3;
					float num7 = 0.7f * MathUtils.Pow(SimplexNoise.OctavedNoise(num4, num5, 0.5f * num, 0.5f, 1, 2f, 1f), 3f) * num3;
					Vector2 corner = new Vector2(num4, num5);
					Vector2 corner2 = new Vector2(num4 + num2, num5 + num2);
					if (num6 > 0.01f)
					{
						flatBatch2D.QueueRectangle(corner, corner2, 0f, new Color(0f, 0f, 0f, num6));
					}
					if (num7 > 0.01f)
					{
						flatBatch2D.QueueQuad(corner, corner2, 0f, new Color(0f, 0f, 0f, num7));
					}
				}
			}
			flatBatch2D.TransformLines(base.GlobalTransform, count);
			flatBatch2D.TransformTriangles(base.GlobalTransform, count2);
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = true;
		}

		public override void Draw(DrawContext dc)
		{
			DrawImage(dc);
			DrawSquares(dc);
		}
	}
}
