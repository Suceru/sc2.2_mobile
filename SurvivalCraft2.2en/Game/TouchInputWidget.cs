using Engine;
using Engine.Input;

namespace Game
{
	public class TouchInputWidget : Widget
	{
		public int? m_touchId;

		public Vector2 m_touchLastPosition;

		public Vector2 m_touchOrigin;

		public Vector2 m_touchOriginLimited;

		public bool m_touchMoved;

		public double m_touchTime;

		public int m_touchFrameIndex;

		public TouchInput? m_touchInput;

		public float m_radius = 30f;

		public float Radius
		{
			get
			{
				return m_radius;
			}
			set
			{
				m_radius = MathUtils.Max(value, 1f);
			}
		}

		public TouchInput? TouchInput
		{
			get
			{
				if (base.IsEnabledGlobal && base.IsVisibleGlobal)
				{
					return m_touchInput;
				}
				return null;
			}
		}

		public override void Update()
		{
			m_touchInput = null;
			double frameStartTime = Time.FrameStartTime;
			int frameIndex = Time.FrameIndex;
			foreach (TouchLocation touchLocation in base.Input.TouchLocations)
			{
				if (touchLocation.State == TouchLocationState.Pressed)
				{
					if (HitTestGlobal(touchLocation.Position) == this)
					{
						m_touchId = touchLocation.Id;
						m_touchLastPosition = touchLocation.Position;
						m_touchOrigin = touchLocation.Position;
						m_touchOriginLimited = touchLocation.Position;
						m_touchTime = frameStartTime;
						m_touchFrameIndex = frameIndex;
						m_touchMoved = false;
					}
				}
				else if (touchLocation.State == TouchLocationState.Moved)
				{
					if (m_touchId.HasValue && touchLocation.Id == m_touchId.Value)
					{
						m_touchMoved |= (Vector2.Distance(touchLocation.Position, m_touchOrigin) > SettingsManager.MinimumDragDistance * base.GlobalScale);
						TouchInput value = default(TouchInput);
						value.InputType = ((!m_touchMoved) ? TouchInputType.Hold : TouchInputType.Move);
						value.Duration = (float)(frameStartTime - m_touchTime);
						value.DurationFrames = frameIndex - m_touchFrameIndex;
						value.Position = touchLocation.Position;
						value.Move = touchLocation.Position - m_touchLastPosition;
						value.TotalMove = touchLocation.Position - m_touchOrigin;
						value.TotalMoveLimited = touchLocation.Position - m_touchOriginLimited;
						if (MathUtils.Abs(value.TotalMoveLimited.X) > m_radius)
						{
							m_touchOriginLimited.X = touchLocation.Position.X - MathUtils.Sign(value.TotalMoveLimited.X) * m_radius;
						}
						if (MathUtils.Abs(value.TotalMoveLimited.Y) > m_radius)
						{
							m_touchOriginLimited.Y = touchLocation.Position.Y - MathUtils.Sign(value.TotalMoveLimited.Y) * m_radius;
						}
						m_touchInput = value;
						m_touchLastPosition = touchLocation.Position;
					}
				}
				else if (touchLocation.State == TouchLocationState.Released && m_touchId.HasValue && touchLocation.Id == m_touchId.Value)
				{
					if (frameStartTime - m_touchTime <= (double)SettingsManager.MinimumHoldDuration && Vector2.Distance(touchLocation.Position, m_touchOrigin) < SettingsManager.MinimumDragDistance * base.GlobalScale)
					{
						TouchInput value2 = default(TouchInput);
						value2.InputType = TouchInputType.Tap;
						value2.Duration = (float)(frameStartTime - m_touchTime);
						value2.DurationFrames = frameIndex - m_touchFrameIndex;
						value2.Position = touchLocation.Position;
						m_touchInput = value2;
					}
					m_touchId = null;
				}
			}
		}
	}
}
