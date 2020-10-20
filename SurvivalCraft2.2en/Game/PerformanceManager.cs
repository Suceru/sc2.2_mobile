using Engine;
using Engine.Graphics;
using Engine.Media;
using System;

namespace Game
{
	public static class PerformanceManager
	{
		public struct FrameData
		{
			public float CpuTime;

			public float TotalTime;
		}

		public static PrimitivesRenderer2D m_primitivesRenderer;

		public static RunningAverage m_averageFrameTime;

		public static RunningAverage m_averageCpuFrameTime;

		public static float? m_longTermAverageFrameTime;

		public static long m_totalMemoryUsed;

		public static long m_totalGpuMemoryUsed;

		public static StateMachine m_stateMachine;

		public static double m_totalGameTime;

		public static double m_totalFrameTime;

		public static double m_totalCpuFrameTime;

		public static int m_frameCount;

		public static string m_statsString;

		public static FrameData[] m_frameData;

		public static int m_frameDataIndex;

		public static float? LongTermAverageFrameTime => m_longTermAverageFrameTime;

		public static float AverageFrameTime => m_averageFrameTime.Value;

		public static float AverageCpuFrameTime => m_averageCpuFrameTime.Value;

		public static long TotalMemoryUsed => m_totalMemoryUsed;

		public static long TotalGpuMemoryUsed => m_totalGpuMemoryUsed;

		static PerformanceManager()
		{
			m_primitivesRenderer = new PrimitivesRenderer2D();
			m_averageFrameTime = new RunningAverage(1f);
			m_averageCpuFrameTime = new RunningAverage(1f);
			m_stateMachine = new StateMachine();
			m_statsString = string.Empty;
			m_stateMachine.AddState("PreMeasure", delegate
			{
				m_totalGameTime = 0.0;
			}, delegate
			{
				if (GameManager.Project != null)
				{
					m_totalGameTime += Time.FrameDuration;
					if (m_totalGameTime > 60.0)
					{
						m_stateMachine.TransitionTo("Measuring");
					}
				}
			}, null);
			m_stateMachine.AddState("Measuring", delegate
			{
				m_totalFrameTime = 0.0;
				m_totalCpuFrameTime = 0.0;
				m_frameCount = 0;
			}, delegate
			{
				if (GameManager.Project != null)
				{
					if (ScreensManager.CurrentScreen != null && ScreensManager.CurrentScreen.GetType() == typeof(GameScreen))
					{
						float lastFrameTime = Program.LastFrameTime;
						float lastCpuFrameTime = Program.LastCpuFrameTime;
						if (lastFrameTime > 0f && lastFrameTime < 1f && lastCpuFrameTime > 0f && lastCpuFrameTime < 1f)
						{
							m_totalFrameTime += lastFrameTime;
							m_totalCpuFrameTime += lastCpuFrameTime;
							m_frameCount++;
						}
						if (m_totalFrameTime > 180.0)
						{
							m_stateMachine.TransitionTo("PostMeasure");
						}
					}
				}
				else
				{
					m_stateMachine.TransitionTo("PreMeasure");
				}
			}, null);
			m_stateMachine.AddState("PostMeasure", delegate
			{
				if (m_frameCount > 0)
				{
					m_longTermAverageFrameTime = (float)(m_totalFrameTime / (double)m_frameCount);
					float num = (int)MathUtils.Round(MathUtils.Round(m_totalFrameTime / (double)m_frameCount / 0.004999999888241291) * 0.004999999888241291 * 1000.0);
					float num2 = (int)MathUtils.Round(MathUtils.Round(m_totalCpuFrameTime / (double)m_frameCount / 0.004999999888241291) * 0.004999999888241291 * 1000.0);
					AnalyticsManager.LogEvent("[PerformanceManager] Measurement", new AnalyticsParameter("FrameCount", m_frameCount.ToString()), new AnalyticsParameter("AverageFrameTime", num.ToString() + "ms"), new AnalyticsParameter("AverageFrameCpuTime", num2.ToString() + "ms"));
					Log.Information($"PerformanceManager Measurement: frames={m_frameCount.ToString()}, avgFrameTime={num.ToString()}ms, avgFrameCpuTime={num2.ToString()}ms");
				}
			}, delegate
			{
				if (GameManager.Project == null)
				{
					m_stateMachine.TransitionTo("PreMeasure");
				}
			}, null);
			m_stateMachine.TransitionTo("PreMeasure");
		}

		public static void Update()
		{
			m_averageFrameTime.AddSample(Program.LastFrameTime);
			m_averageCpuFrameTime.AddSample(Program.LastCpuFrameTime);
			if (Time.PeriodicEvent(1.0, 0.0))
			{
				m_totalMemoryUsed = GC.GetTotalMemory(forceFullCollection: false);
				m_totalGpuMemoryUsed = Display.GetGpuMemoryUsage();
			}
			m_stateMachine.Update();
		}

		public static void Draw()
		{
			Vector2 scale = new Vector2(MathUtils.Round(MathUtils.Clamp(ScreensManager.RootWidget.GlobalScale, 1f, 4f)));
			Viewport viewport = Display.Viewport;
			if (SettingsManager.DisplayFpsCounter)
			{
				if (Time.PeriodicEvent(1.0, 0.0))
				{
					m_statsString = $"CPUMEM {(float)TotalMemoryUsed / 1024f / 1024f:0}MB, GPUMEM {(float)TotalGpuMemoryUsed / 1024f / 1024f:0}MB, CPU {AverageCpuFrameTime / AverageFrameTime * 100f:0}%, FPS {1f / AverageFrameTime:0.0}";
				}
				m_primitivesRenderer.FontBatch(BitmapFont.DebugFont, 0, null, null, null, SamplerState.PointClamp).QueueText(m_statsString, new Vector2(viewport.Width, 0f), 0f, Color.White, TextAnchor.Right, scale, Vector2.Zero);
			}
			if (SettingsManager.DisplayFpsRibbon)
			{
				float num = ((float)viewport.Width / scale.X > 480f) ? (scale.X * 2f) : scale.X;
				float num2 = (float)viewport.Height / -0.1f;
				float num3 = viewport.Height - 1;
				float s = 0.5f;
				int num4 = MathUtils.Max((int)((float)viewport.Width / num), 1);
				if (m_frameData == null || m_frameData.Length != num4)
				{
					m_frameData = new FrameData[num4];
					m_frameDataIndex = 0;
				}
				m_frameData[m_frameDataIndex] = new FrameData
				{
					CpuTime = Program.LastCpuFrameTime,
					TotalTime = Program.LastFrameTime
				};
				m_frameDataIndex = (m_frameDataIndex + 1) % m_frameData.Length;
				FlatBatch2D flatBatch2D = m_primitivesRenderer.FlatBatch();
				Color color = Color.Orange * s;
				Color color2 = Color.Red * s;
				for (int num5 = m_frameData.Length - 1; num5 >= 0; num5--)
				{
					int num6 = (num5 - m_frameData.Length + 1 + m_frameDataIndex + m_frameData.Length) % m_frameData.Length;
					FrameData frameData = m_frameData[num6];
					float x = (float)num5 * num;
					float x2 = (float)(num5 + 1) * num;
					flatBatch2D.QueueQuad(new Vector2(x, num3), new Vector2(x2, num3 + frameData.CpuTime * num2), 0f, color);
					flatBatch2D.QueueQuad(new Vector2(x, num3 + frameData.CpuTime * num2), new Vector2(x2, num3 + frameData.TotalTime * num2), 0f, color2);
				}
				flatBatch2D.QueueLine(new Vector2(0f, num3 + 0.0166666675f * num2), new Vector2(viewport.Width, num3 + 0.0166666675f * num2), 0f, Color.Green);
			}
			else
			{
				m_frameData = null;
			}
			m_primitivesRenderer.Flush();
		}
	}
}
