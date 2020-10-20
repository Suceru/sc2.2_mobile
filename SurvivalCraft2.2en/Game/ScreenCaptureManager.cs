using Engine;
using Engine.Graphics;
using Engine.Media;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game
{
	public static class ScreenCaptureManager
	{
		public static readonly string ScreenshotDir = "system:" + Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + Path.DirectorySeparatorChar + "Survivalcraft";

		public static bool m_captureRequested;

		public static Action m_successHandler;

		public static Action<Exception> m_failureHandler;

		public static void Run()
		{
			if (m_captureRequested)
			{
				try
				{
					int num;
					int height;
					switch (SettingsManager.ScreenshotSize)
					{
					case ScreenshotSize.ScreenSize:
					{
						num = MathUtils.Max(Window.ScreenSize.X, Window.ScreenSize.Y);
						height = MathUtils.Min(Window.ScreenSize.X, Window.ScreenSize.Y);
						float num2 = (float)num / (float)height;
						num = MathUtils.Min(num, 2048);
						height = (int)MathUtils.Round((float)num / num2);
						break;
					}
					case ScreenshotSize.FullHD:
						num = 1920;
						height = 1080;
						break;
					default:
						num = 1920;
						height = 1080;
						break;
					}
					DateTime now = DateTime.Now;
					Capture(num, height, $"Survivalcraft {now.Year:D4}-{now.Month:D2}-{now.Day:D2} {now.Hour:D2}-{now.Minute:D2}-{now.Second:D2}.jpg");
					m_successHandler?.Invoke();
					GC.Collect();
				}
				catch (Exception ex)
				{
					Log.Error($"Error capturing screen. Reason: {ex.Message}");
					m_failureHandler?.Invoke(ex);
				}
				finally
				{
					m_captureRequested = false;
					m_successHandler = null;
					m_failureHandler = null;
				}
			}
		}

		public static void CapturePhoto(Action success, Action<Exception> failure)
		{
			if (!m_captureRequested)
			{
				m_captureRequested = true;
				m_successHandler = success;
				m_failureHandler = failure;
			}
		}

		public static void Capture(int width, int height, string filename)
		{
			if (GameManager.Project != null)
			{
				using (RenderTarget2D renderTarget2D = new RenderTarget2D(width, height, 1, ColorFormat.Rgba8888, DepthFormat.Depth24Stencil8))
				{
					RenderTarget2D renderTarget = Display.RenderTarget;
					Dictionary<ComponentGui, bool> dictionary = new Dictionary<ComponentGui, bool>();
					ResolutionMode resolutionMode = ResolutionMode.High;
					try
					{
						if (!SettingsManager.ShowGuiInScreenshots)
						{
							foreach (ComponentPlayer componentPlayer in GameManager.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).ComponentPlayers)
							{
								dictionary[componentPlayer.ComponentGui] = componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible;
								componentPlayer.ComponentGui.ControlsContainerWidget.IsVisible = false;
							}
						}
						resolutionMode = SettingsManager.ResolutionMode;
						SettingsManager.ResolutionMode = ResolutionMode.High;
						Display.RenderTarget = renderTarget2D;
						ScreensManager.Draw();
						if (SettingsManager.ShowLogoInScreenshots)
						{
							PrimitivesRenderer2D primitivesRenderer2D = new PrimitivesRenderer2D();
							Texture2D texture2D = ContentManager.Get<Texture2D>("Textures/Gui/ScreenCaptureOverlay");
							Vector2 vector = new Vector2((width - texture2D.Width) / 2, 0f);
							Vector2 corner = vector + new Vector2(texture2D.Width, texture2D.Height);
							primitivesRenderer2D.TexturedBatch(texture2D, useAlphaTest: false, 0, DepthStencilState.None).QueueQuad(vector, corner, 0f, new Vector2(0f, 0f), new Vector2(1f, 1f), Color.White);
							primitivesRenderer2D.Flush();
						}
					}
					finally
					{
						Display.RenderTarget = renderTarget;
						foreach (KeyValuePair<ComponentGui, bool> item in dictionary)
						{
							item.Key.ControlsContainerWidget.IsVisible = item.Value;
						}
						SettingsManager.ResolutionMode = resolutionMode;
					}
					Image image = new Image(renderTarget2D.Width, renderTarget2D.Height);
					renderTarget2D.GetData(image.Pixels, 0, new Rectangle(0, 0, renderTarget2D.Width, renderTarget2D.Height));
					if (!Storage.DirectoryExists(ScreenshotDir))
						Storage.CreateDirectory(ScreenshotDir);
					using (Stream stream = Storage.OpenFile(Storage.CombinePaths(ScreenshotDir, filename), OpenFileMode.CreateOrOpen))
					{
						Image.Save(image, stream, ImageFileFormat.Jpg, saveAlpha: false);
					}
				}
			}
		}
	}
}
