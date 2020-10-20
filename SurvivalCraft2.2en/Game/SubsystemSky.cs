using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemSky : Subsystem, IDrawable, IUpdateable
	{
		public struct SkyVertex
		{
			public Vector3 Position;

			public Color Color;
		}

		public class SkyDome : IDisposable
		{
			public const int VerticesCountX = 10;

			public const int VerticesCountY = 8;

			public float? LastUpdateTimeOfDay;

			public float? LastUpdatePrecipitationIntensity;

			public int? LastUpdateTemperature;

			public float LastUpdateLightningStrikeBrightness;

			public SkyVertex[] Vertices = new SkyVertex[80];

			public ushort[] Indices = new ushort[444];

			public VertexBuffer VertexBuffer;

			public IndexBuffer IndexBuffer;

			public void Dispose()
			{
				Utilities.Dispose(ref VertexBuffer);
				Utilities.Dispose(ref IndexBuffer);
			}
		}

		public struct StarVertex
		{
			public Vector3 Position;

			public Vector2 TextureCoordinate;

			public Color Color;
		}

		public SubsystemTimeOfDay m_subsystemTimeOfDay;

		public SubsystemTime m_subsystemTime;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemWeather m_subsystemWeather;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemBodies m_subsystemBodies;

		public SubsystemFluidBlockBehavior m_subsystemFluidBlockBehavior;

		public PrimitivesRenderer2D m_primitivesRenderer2d = new PrimitivesRenderer2D();

		public PrimitivesRenderer3D m_primitivesRenderer3d = new PrimitivesRenderer3D();

		public Random m_random = new Random();

		public Color m_viewFogColor;

		public Vector2 m_viewFogRange;

		public bool m_viewIsSkyVisible;

		public Texture2D m_sunTexture;

		public Texture2D m_glowTexture;

		public Texture2D m_cloudsTexture;

		public Texture2D[] m_moonTextures = new Texture2D[8];

		public static UnlitShader m_shaderFlat = new UnlitShader(useVertexColor: true, useTexture: false, useAlphaThreshold: false);

		public static UnlitShader m_shaderTextured = new UnlitShader(useVertexColor: true, useTexture: true, useAlphaThreshold: false);

		public VertexDeclaration m_skyVertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));

		public Dictionary<GameWidget, SkyDome> m_skyDomes = new Dictionary<GameWidget, SkyDome>();

		public VertexBuffer m_starsVertexBuffer;

		public IndexBuffer m_starsIndexBuffer;

		public VertexDeclaration m_starsVertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector3, VertexElementSemantic.Position), new VertexElement(12, VertexElementFormat.Vector2, VertexElementSemantic.TextureCoordinate), new VertexElement(20, VertexElementFormat.NormalizedByte4, VertexElementSemantic.Color));

		public const int m_starsCount = 150;

		public Vector3? m_lightningStrikePosition;

		public float m_lightningStrikeBrightness;

		public double m_lastLightningStrikeTime;

		public const float DawnStart = 0.2f;

		public const float DayStart = 0.3f;

		public const float DuskStart = 0.7f;

		public const float NightStart = 0.8f;

		public bool DrawSkyEnabled = true;

		public bool DrawCloudsWireframe;

		public bool FogEnabled = true;

		public int[] m_drawOrders = new int[3]
		{
			-100,
			5,
			105
		};

		public float[] m_cloudsLayerRadii = new float[4]
		{
			0f,
			0.8f,
			0.95f,
			1f
		};

		public Color[] m_cloudsLayerColors = new Color[5];

		public static int[] m_lightValuesMoonless = new int[6]
		{
			0,
			3,
			6,
			9,
			12,
			15
		};

		public static int[] m_lightValuesNormal = new int[6]
		{
			3,
			5,
			8,
			10,
			13,
			15
		};

		public float SkyLightIntensity
		{
			get;
			set;
		}

		public int MoonPhase
		{
			get;
			set;
		}

		public int SkyLightValue
		{
			get;
			set;
		}

		public float VisibilityRange
		{
			get;
			set;
		}

		public float VisibilityRangeYMultiplier
		{
			get;
			set;
		}

		public float ViewUnderWaterDepth
		{
			get;
			set;
		}

		public float ViewUnderMagmaDepth
		{
			get;
			set;
		}

		public Color ViewFogColor => m_viewFogColor;

		public Vector2 ViewFogRange => m_viewFogRange;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public int[] DrawOrders => m_drawOrders;

		public void MakeLightningStrike(Vector3 targetPosition)
		{
			if (m_lightningStrikePosition.HasValue || !(m_subsystemTime.GameTime - m_lastLightningStrikeTime > 1.0))
			{
				return;
			}
			m_lastLightningStrikeTime = m_subsystemTime.GameTime;
			m_lightningStrikePosition = targetPosition;
			m_lightningStrikeBrightness = 1f;
			float num = 3.40282347E+38f;
			foreach (Vector3 listenerPosition in m_subsystemAudio.ListenerPositions)
			{
				float num2 = Vector2.Distance(new Vector2(listenerPosition.X, listenerPosition.Z), new Vector2(targetPosition.X, targetPosition.Z));
				if (num2 < num)
				{
					num = num2;
				}
			}
			float delay = m_subsystemAudio.CalculateDelay(num);
			if (num < 40f)
			{
				m_subsystemAudio.PlayRandomSound("Audio/ThunderNear", 1f, m_random.Float(-0.2f, 0.2f), 0f, delay);
			}
			else if (num < 200f)
			{
				m_subsystemAudio.PlayRandomSound("Audio/ThunderFar", 0.8f, m_random.Float(-0.2f, 0.2f), 0f, delay);
			}
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode != 0)
			{
				return;
			}
			DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
			m_subsystemBodies.FindBodiesAroundPoint(new Vector2(targetPosition.X, targetPosition.Z), 4f, dynamicArray);
			for (int i = 0; i < dynamicArray.Count; i++)
			{
				ComponentBody componentBody = dynamicArray.Array[i];
				if (componentBody.Position.Y > targetPosition.Y - 1.5f && Vector2.Distance(new Vector2(componentBody.Position.X, componentBody.Position.Z), new Vector2(targetPosition.X, targetPosition.Z)) < 4f)
				{
					componentBody.Entity.FindComponent<ComponentOnFire>()?.SetOnFire(null, m_random.Float(12f, 15f));
				}
				ComponentCreature componentCreature = componentBody.Entity.FindComponent<ComponentCreature>();
				if (componentCreature != null && componentCreature.PlayerStats != null)
				{
					componentCreature.PlayerStats.StruckByLightning++;
				}
			}
			int x = Terrain.ToCell(targetPosition.X);
			int num3 = Terrain.ToCell(targetPosition.Y);
			int z = Terrain.ToCell(targetPosition.Z);
			float pressure = (m_random.Float(0f, 1f) < 0.2f) ? 39 : 19;
			base.Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).AddExplosion(x, num3 + 1, z, pressure, isIncendiary: false, noExplosionSound: true);
		}

		public void Update(float dt)
		{
			MoonPhase = ((int)MathUtils.Floor(m_subsystemTimeOfDay.Day - 0.5 + 5.0) % 8 + 8) % 8;
			UpdateLightAndViewParameters();
		}

		public void Draw(Camera camera, int drawOrder)
		{
			if (drawOrder == m_drawOrders[0])
			{
				ViewUnderWaterDepth = 0f;
				ViewUnderMagmaDepth = 0f;
				Vector3 viewPosition = camera.ViewPosition;
				int x = Terrain.ToCell(viewPosition.X);
				int y = Terrain.ToCell(viewPosition.Y);
				int z = Terrain.ToCell(viewPosition.Z);
				FluidBlock surfaceFluidBlock;
				float? surfaceHeight = m_subsystemFluidBlockBehavior.GetSurfaceHeight(x, y, z, out surfaceFluidBlock);
				if (surfaceHeight.HasValue)
				{
					if (surfaceFluidBlock is WaterBlock)
					{
						ViewUnderWaterDepth = surfaceHeight.Value + 0.1f - viewPosition.Y;
					}
					else if (surfaceFluidBlock is MagmaBlock)
					{
						ViewUnderMagmaDepth = surfaceHeight.Value + 1f - viewPosition.Y;
					}
				}
				if (ViewUnderWaterDepth > 0f)
				{
					int seasonalHumidity = m_subsystemTerrain.Terrain.GetSeasonalHumidity(x, z);
					int temperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z) + SubsystemWeather.GetTemperatureAdjustmentAtHeight(y);
					Color c = BlockColorsMap.WaterColorsMap.Lookup(temperature, seasonalHumidity);
					float num = MathUtils.Lerp(1f, 0.5f, (float)seasonalHumidity / 15f);
					float num2 = MathUtils.Lerp(1f, 0.2f, MathUtils.Saturate(0.075f * (ViewUnderWaterDepth - 2f)));
					float num3 = MathUtils.Lerp(0.33f, 1f, SkyLightIntensity);
					m_viewFogRange.X = 0f;
					m_viewFogRange.Y = MathUtils.Lerp(4f, 10f, num * num2 * num3);
					m_viewFogColor = Color.MultiplyColorOnly(c, 0.66f * num2 * num3);
					VisibilityRangeYMultiplier = 1f;
					m_viewIsSkyVisible = false;
				}
				else if (ViewUnderMagmaDepth > 0f)
				{
					m_viewFogRange.X = 0f;
					m_viewFogRange.Y = 0.1f;
					m_viewFogColor = new Color(255, 80, 0);
					VisibilityRangeYMultiplier = 1f;
					m_viewIsSkyVisible = false;
				}
				else
				{
					float num4 = 1024f;
					float num5 = 128f;
					int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(Terrain.ToCell(viewPosition.X), Terrain.ToCell(viewPosition.Z));
					float num6 = MathUtils.Lerp(0.5f, 0f, m_subsystemWeather.GlobalPrecipitationIntensity);
					float num7 = MathUtils.Lerp(1f, 0.8f, m_subsystemWeather.GlobalPrecipitationIntensity);
					m_viewFogRange.X = VisibilityRange * num6;
					m_viewFogRange.Y = VisibilityRange * num7;
					m_viewFogColor = CalculateSkyColor(new Vector3(camera.ViewDirection.X, 0f, camera.ViewDirection.Z), m_subsystemTimeOfDay.TimeOfDay, m_subsystemWeather.GlobalPrecipitationIntensity, seasonalTemperature);
					VisibilityRangeYMultiplier = MathUtils.Lerp(VisibilityRange / num4, VisibilityRange / num5, MathUtils.Pow(m_subsystemWeather.GlobalPrecipitationIntensity, 4f));
					m_viewIsSkyVisible = true;
				}
				if (!FogEnabled)
				{
					m_viewFogRange = new Vector2(100000f, 100000f);
				}
				if (!DrawSkyEnabled || !m_viewIsSkyVisible || SettingsManager.SkyRenderingMode == SkyRenderingMode.Disabled)
				{
					FlatBatch2D flatBatch2D = m_primitivesRenderer2d.FlatBatch(-1, DepthStencilState.None, RasterizerState.CullNoneScissor, BlendState.Opaque);
					int count = flatBatch2D.TriangleVertices.Count;
					flatBatch2D.QueueQuad(Vector2.Zero, camera.ViewportSize, 0f, m_viewFogColor);
					flatBatch2D.TransformTriangles(camera.ViewportMatrix, count);
					m_primitivesRenderer2d.Flush();
				}
			}
			else if (drawOrder == m_drawOrders[1])
			{
				if (DrawSkyEnabled && m_viewIsSkyVisible && SettingsManager.SkyRenderingMode != SkyRenderingMode.Disabled)
				{
					DrawSkydome(camera);
					DrawStars(camera);
					DrawSunAndMoon(camera);
					DrawClouds(camera);
					m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
				}
			}
			else
			{
				DrawLightning(camera);
				m_primitivesRenderer3d.Flush(camera.ViewProjectionMatrix);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTimeOfDay = base.Project.FindSubsystem<SubsystemTimeOfDay>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
			m_subsystemFluidBlockBehavior = base.Project.FindSubsystem<SubsystemFluidBlockBehavior>(throwOnError: true);
			m_sunTexture = ContentManager.Get<Texture2D>("Textures/Sun");
			m_glowTexture = ContentManager.Get<Texture2D>("Textures/SkyGlow");
			m_cloudsTexture = ContentManager.Get<Texture2D>("Textures/Clouds");
			for (int i = 0; i < 8; i++)
			{
				m_moonTextures[i] = ContentManager.Get<Texture2D>("Textures/Moon" + (i + 1).ToString(CultureInfo.InvariantCulture));
			}
			UpdateLightAndViewParameters();
			Display.DeviceReset += Display_DeviceReset;
		}

		public override void Dispose()
		{
			Display.DeviceReset -= Display_DeviceReset;
			Utilities.Dispose(ref m_starsVertexBuffer);
			Utilities.Dispose(ref m_starsIndexBuffer);
			foreach (SkyDome value in m_skyDomes.Values)
			{
				value.Dispose();
			}
			m_skyDomes.Clear();
		}

		public void Display_DeviceReset()
		{
			Utilities.Dispose(ref m_starsVertexBuffer);
			Utilities.Dispose(ref m_starsIndexBuffer);
			foreach (SkyDome value in m_skyDomes.Values)
			{
				value.Dispose();
			}
			m_skyDomes.Clear();
		}

		public void DrawSkydome(Camera camera)
		{
			if (!m_skyDomes.TryGetValue(camera.GameWidget, out SkyDome value))
			{
				value = new SkyDome();
				m_skyDomes.Add(camera.GameWidget, value);
			}
			if (value.VertexBuffer == null || value.IndexBuffer == null)
			{
				Utilities.Dispose(ref value.VertexBuffer);
				Utilities.Dispose(ref value.IndexBuffer);
				value.VertexBuffer = new VertexBuffer(m_skyVertexDeclaration, value.Vertices.Length);
				value.IndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, value.Indices.Length);
				FillSkyIndexBuffer(value);
				value.LastUpdateTimeOfDay = null;
			}
			int x = Terrain.ToCell(camera.ViewPosition.X);
			int z = Terrain.ToCell(camera.ViewPosition.Z);
			float globalPrecipitationIntensity = m_subsystemWeather.GlobalPrecipitationIntensity;
			float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
			int seasonalTemperature = m_subsystemTerrain.Terrain.GetSeasonalTemperature(x, z);
			if (!value.LastUpdateTimeOfDay.HasValue || MathUtils.Abs(timeOfDay - value.LastUpdateTimeOfDay.Value) > 0.001f || !value.LastUpdatePrecipitationIntensity.HasValue || MathUtils.Abs(globalPrecipitationIntensity - value.LastUpdatePrecipitationIntensity.Value) > 0.02f || ((globalPrecipitationIntensity == 0f || globalPrecipitationIntensity == 1f) && value.LastUpdatePrecipitationIntensity.Value != globalPrecipitationIntensity) || m_lightningStrikeBrightness != value.LastUpdateLightningStrikeBrightness || !value.LastUpdateTemperature.HasValue || seasonalTemperature != value.LastUpdateTemperature)
			{
				value.LastUpdateTimeOfDay = timeOfDay;
				value.LastUpdatePrecipitationIntensity = globalPrecipitationIntensity;
				value.LastUpdateLightningStrikeBrightness = m_lightningStrikeBrightness;
				value.LastUpdateTemperature = seasonalTemperature;
				FillSkyVertexBuffer(value, timeOfDay, globalPrecipitationIntensity, seasonalTemperature);
			}
			Display.DepthStencilState = DepthStencilState.DepthRead;
			Display.RasterizerState = RasterizerState.CullNoneScissor;
			Display.BlendState = BlendState.Opaque;
			m_shaderFlat.Transforms.World[0] = Matrix.CreateTranslation(camera.ViewPosition) * camera.ViewProjectionMatrix;
			m_shaderFlat.Color = Vector4.One;
			Display.DrawIndexed(PrimitiveType.TriangleList, m_shaderFlat, value.VertexBuffer, value.IndexBuffer, 0, value.IndexBuffer.IndicesCount);
		}

		public void DrawStars(Camera camera)
		{
			float globalPrecipitationIntensity = m_subsystemWeather.GlobalPrecipitationIntensity;
			float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
			if (m_starsVertexBuffer == null || m_starsIndexBuffer == null)
			{
				Utilities.Dispose(ref m_starsVertexBuffer);
				Utilities.Dispose(ref m_starsIndexBuffer);
				m_starsVertexBuffer = new VertexBuffer(m_starsVertexDeclaration, 600);
				m_starsIndexBuffer = new IndexBuffer(IndexFormat.SixteenBits, 900);
				FillStarsBuffers();
			}
			Display.DepthStencilState = DepthStencilState.DepthRead;
			Display.RasterizerState = RasterizerState.CullNoneScissor;
			float num = MathUtils.Sqr((1f - CalculateLightIntensity(timeOfDay)) * (1f - globalPrecipitationIntensity));
			if (num > 0.01f)
			{
				Display.BlendState = BlendState.Additive;
				m_shaderTextured.Transforms.World[0] = Matrix.CreateRotationZ(-2f * timeOfDay * (float)Math.PI) * Matrix.CreateTranslation(camera.ViewPosition) * camera.ViewProjectionMatrix;
				m_shaderTextured.Color = new Vector4(1f, 1f, 1f, num);
				m_shaderTextured.Texture = ContentManager.Get<Texture2D>("Textures/Star");
				m_shaderTextured.SamplerState = SamplerState.LinearClamp;
				Display.DrawIndexed(PrimitiveType.TriangleList, m_shaderTextured, m_starsVertexBuffer, m_starsIndexBuffer, 0, m_starsIndexBuffer.IndicesCount);
			}
		}

		public void DrawSunAndMoon(Camera camera)
		{
			float globalPrecipitationIntensity = m_subsystemWeather.GlobalPrecipitationIntensity;
			float timeOfDay = m_subsystemTimeOfDay.TimeOfDay;
			float f = MathUtils.Max(CalculateDawnGlowIntensity(timeOfDay), CalculateDuskGlowIntensity(timeOfDay));
			float num = 2f * timeOfDay * (float)Math.PI;
			float angle = num + (float)Math.PI;
			float num2 = MathUtils.Lerp(90f, 160f, f);
			float num3 = MathUtils.Lerp(60f, 80f, f);
			Color color = Color.Lerp(new Color(255, 255, 255), new Color(255, 255, 160), f);
			Color white = Color.White;
			white *= 1f - SkyLightIntensity;
			color *= MathUtils.Lerp(1f, 0f, globalPrecipitationIntensity);
			white *= MathUtils.Lerp(1f, 0f, globalPrecipitationIntensity);
			Color color2 = color * 0.6f * MathUtils.Lerp(1f, 0f, globalPrecipitationIntensity);
			Color color3 = color * 0.2f * MathUtils.Lerp(1f, 0f, globalPrecipitationIntensity);
			TexturedBatch3D batch = m_primitivesRenderer3d.TexturedBatch(m_glowTexture, useAlphaTest: false, 0, DepthStencilState.DepthRead, null, BlendState.Additive);
			TexturedBatch3D batch2 = m_primitivesRenderer3d.TexturedBatch(m_sunTexture, useAlphaTest: false, 1, DepthStencilState.DepthRead, null, BlendState.AlphaBlend);
			TexturedBatch3D batch3 = m_primitivesRenderer3d.TexturedBatch(m_moonTextures[MoonPhase], useAlphaTest: false, 1, DepthStencilState.DepthRead, null, BlendState.AlphaBlend);
			QueueCelestialBody(batch, camera.ViewPosition, color2, 900f, 3.5f * num2, num);
			QueueCelestialBody(batch, camera.ViewPosition, color3, 900f, 3.5f * num3, angle);
			QueueCelestialBody(batch2, camera.ViewPosition, color, 900f, num2, num);
			QueueCelestialBody(batch3, camera.ViewPosition, white, 900f, num3, angle);
		}

		public void DrawLightning(Camera camera)
		{
			if (!m_lightningStrikePosition.HasValue)
			{
				return;
			}
			FlatBatch3D flatBatch3D = m_primitivesRenderer3d.FlatBatch(0, DepthStencilState.DepthRead, null, BlendState.Additive);
			Vector3 value = m_lightningStrikePosition.Value;
			Vector3 unitY = Vector3.UnitY;
			Vector3 v = Vector3.Normalize(Vector3.Cross(camera.ViewDirection, unitY));
			Viewport viewport = Display.Viewport;
			float num = Vector4.Transform(new Vector4(value, 1f), camera.ViewProjectionMatrix).W * 2f / ((float)viewport.Width * camera.ProjectionMatrix.M11);
			for (int i = 0; i < (int)(m_lightningStrikeBrightness * 30f); i++)
			{
				float s = m_random.NormalFloat(0f, 1f * num);
				float s2 = m_random.NormalFloat(0f, 1f * num);
				Vector3 v2 = s * v + s2 * unitY;
				float num2 = 260f;
				while (num2 > value.Y)
				{
					uint num3 = MathUtils.Hash((uint)(m_lightningStrikePosition.Value.X + 100f * m_lightningStrikePosition.Value.Z + 200f * num2));
					float num4 = MathUtils.Lerp(4f, 10f, (float)(double)(num3 & 0xFF) / 255f);
					float s3 = ((num3 & 1) == 0) ? 1 : (-1);
					float s4 = MathUtils.Lerp(0.05f, 0.2f, (float)(double)((num3 >> 8) & 0xFF) / 255f);
					float num5 = num2;
					float num6 = num5 - num4 * MathUtils.Lerp(0.45f, 0.55f, (float)(double)((num3 >> 16) & 0xFF) / 255f);
					float num7 = num5 - num4 * MathUtils.Lerp(0.45f, 0.55f, (float)(double)((num3 >> 24) & 0xFF) / 255f);
					float num8 = num5 - num4;
					Vector3 p = new Vector3(value.X, num5, value.Z) + v2;
					Vector3 vector = new Vector3(value.X, num6, value.Z) + v2 - num4 * v * s3 * s4;
					Vector3 vector2 = new Vector3(value.X, num7, value.Z) + v2 + num4 * v * s3 * s4;
					Vector3 p2 = new Vector3(value.X, num8, value.Z) + v2;
					Color color = Color.White * 0.2f * MathUtils.Saturate((260f - num5) * 0.2f);
					Color color2 = Color.White * 0.2f * MathUtils.Saturate((260f - num6) * 0.2f);
					Color color3 = Color.White * 0.2f * MathUtils.Saturate((260f - num7) * 0.2f);
					Color color4 = Color.White * 0.2f * MathUtils.Saturate((260f - num8) * 0.2f);
					flatBatch3D.QueueLine(p, vector, color, color2);
					flatBatch3D.QueueLine(vector, vector2, color2, color3);
					flatBatch3D.QueueLine(vector2, p2, color3, color4);
					num2 -= num4;
				}
			}
			float num9 = MathUtils.Lerp(0.3f, 0.75f, 0.5f * (float)MathUtils.Sin(MathUtils.Remainder(1.0 * m_subsystemTime.GameTime, 6.2831854820251465)) + 0.5f);
			m_lightningStrikeBrightness -= m_subsystemTime.GameTimeDelta / num9;
			if (m_lightningStrikeBrightness <= 0f)
			{
				m_lightningStrikePosition = null;
				m_lightningStrikeBrightness = 0f;
			}
		}

		public void DrawClouds(Camera camera)
		{
			if (SettingsManager.SkyRenderingMode == SkyRenderingMode.NoClouds)
			{
				return;
			}
			float globalPrecipitationIntensity = m_subsystemWeather.GlobalPrecipitationIntensity;
			float num = MathUtils.Lerp(0.03f, 1f, MathUtils.Sqr(SkyLightIntensity)) * MathUtils.Lerp(1f, 0.2f, globalPrecipitationIntensity);
			m_cloudsLayerColors[0] = Color.White * (num * 0.75f);
			m_cloudsLayerColors[1] = Color.White * (num * 0.66f);
			m_cloudsLayerColors[2] = ViewFogColor;
			m_cloudsLayerColors[3] = Color.Transparent;
			double gameTime = m_subsystemTime.GameTime;
			Vector3 viewPosition = camera.ViewPosition;
			Vector2 v = new Vector2((float)MathUtils.Remainder(0.0020000000949949026 * gameTime - (double)(viewPosition.X / 1900f * 1.75f), 1.0) + viewPosition.X / 1900f * 1.75f, (float)MathUtils.Remainder(0.0020000000949949026 * gameTime - (double)(viewPosition.Z / 1900f * 1.75f), 1.0) + viewPosition.Z / 1900f * 1.75f);
			TexturedBatch3D texturedBatch3D = m_primitivesRenderer3d.TexturedBatch(m_cloudsTexture, useAlphaTest: false, 2, DepthStencilState.DepthRead, null, BlendState.AlphaBlend, SamplerState.LinearWrap);
			DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
			DynamicArray<ushort> triangleIndices = texturedBatch3D.TriangleIndices;
			int count = triangleVertices.Count;
			int count2 = triangleVertices.Count;
			int count3 = triangleIndices.Count;
			triangleVertices.Count += 49;
			triangleIndices.Count += 216;
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 7; j++)
				{
					int num2 = j - 3;
					int num3 = i - 3;
					int num4 = MathUtils.Max(MathUtils.Abs(num2), MathUtils.Abs(num3));
					float num5 = m_cloudsLayerRadii[num4];
					float num6 = (num4 > 0) ? (num5 / MathUtils.Sqrt(num2 * num2 + num3 * num3)) : 0f;
					float num7 = (float)num2 * num6;
					float num8 = (float)num3 * num6;
					float y = MathUtils.Lerp(600f, 60f, num5 * num5);
					Vector3 position = new Vector3(viewPosition.X + num7 * 1900f, y, viewPosition.Z + num8 * 1900f);
					Vector2 texCoord = new Vector2(position.X, position.Z) / 1900f * 1.75f - v;
					Color color = m_cloudsLayerColors[num4];
					texturedBatch3D.TriangleVertices.Array[count2++] = new VertexPositionColorTexture(position, color, texCoord);
					if (j > 0 && i > 0)
					{
						ushort num9 = (ushort)(count + j + i * 7);
						ushort num10 = (ushort)(count + (j - 1) + i * 7);
						ushort num11 = (ushort)(count + (j - 1) + (i - 1) * 7);
						ushort num12 = (ushort)(count + j + (i - 1) * 7);
						if ((num2 <= 0 && num3 <= 0) || (num2 > 0 && num3 > 0))
						{
							texturedBatch3D.TriangleIndices.Array[count3++] = num9;
							texturedBatch3D.TriangleIndices.Array[count3++] = num10;
							texturedBatch3D.TriangleIndices.Array[count3++] = num11;
							texturedBatch3D.TriangleIndices.Array[count3++] = num11;
							texturedBatch3D.TriangleIndices.Array[count3++] = num12;
							texturedBatch3D.TriangleIndices.Array[count3++] = num9;
						}
						else
						{
							texturedBatch3D.TriangleIndices.Array[count3++] = num9;
							texturedBatch3D.TriangleIndices.Array[count3++] = num10;
							texturedBatch3D.TriangleIndices.Array[count3++] = num12;
							texturedBatch3D.TriangleIndices.Array[count3++] = num10;
							texturedBatch3D.TriangleIndices.Array[count3++] = num11;
							texturedBatch3D.TriangleIndices.Array[count3++] = num12;
						}
					}
				}
			}
			_ = DrawCloudsWireframe;
		}

		public void QueueCelestialBody(TexturedBatch3D batch, Vector3 viewPosition, Color color, float distance, float radius, float angle)
		{
			if (color.A > 0)
			{
				Vector3 vector = default(Vector3);
				vector.X = 0f - MathUtils.Sin(angle);
				vector.Y = 0f - MathUtils.Cos(angle);
				vector.Z = 0f;
				Vector3 vector2 = vector;
				Vector3 unitZ = Vector3.UnitZ;
				Vector3 v = Vector3.Cross(unitZ, vector2);
				Vector3 p = viewPosition + vector2 * distance - radius * unitZ - radius * v;
				Vector3 p2 = viewPosition + vector2 * distance + radius * unitZ - radius * v;
				Vector3 p3 = viewPosition + vector2 * distance + radius * unitZ + radius * v;
				Vector3 p4 = viewPosition + vector2 * distance - radius * unitZ + radius * v;
				batch.QueueQuad(p, p2, p3, p4, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), color);
			}
		}

		public void UpdateLightAndViewParameters()
		{
			VisibilityRange = SettingsManager.VisibilityRange;
			SkyLightIntensity = CalculateLightIntensity(m_subsystemTimeOfDay.TimeOfDay);
			if (MoonPhase == 4)
			{
				SkyLightValue = m_lightValuesMoonless[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))];
			}
			else
			{
				SkyLightValue = m_lightValuesNormal[(int)MathUtils.Round(MathUtils.Lerp(0f, 5f, SkyLightIntensity))];
			}
		}

		public float CalculateLightIntensity(float timeOfDay)
		{
			if (timeOfDay <= 0.2f || timeOfDay > 0.8f)
			{
				return 0f;
			}
			if (timeOfDay > 0.2f && timeOfDay <= 0.3f)
			{
				return (timeOfDay - 0.2f) / (71f / (226f * (float)Math.PI));
			}
			if (timeOfDay > 0.3f && timeOfDay <= 0.7f)
			{
				return 1f;
			}
			return 1f - (timeOfDay - 0.7f) / 0.100000024f;
		}

		public Color CalculateSkyColor(Vector3 direction, float timeOfDay, float precipitationIntensity, int temperature)
		{
			direction = Vector3.Normalize(direction);
			Vector2 vector = Vector2.Normalize(new Vector2(direction.X, direction.Z));
			float s = CalculateLightIntensity(timeOfDay);
			float f = (float)temperature / 15f;
			Vector3 v = new Vector3(0.65f, 0.68f, 0.7f) * s;
			Vector3 v2 = Vector3.Lerp(new Vector3(0.28f, 0.38f, 0.52f), new Vector3(0.15f, 0.3f, 0.56f), f);
			Vector3 v3 = Vector3.Lerp(new Vector3(0.7f, 0.79f, 0.88f), new Vector3(0.64f, 0.77f, 0.91f), f);
			Vector3 v4 = Vector3.Lerp(v2, v, precipitationIntensity) * s;
			Vector3 v5 = Vector3.Lerp(v3, v, precipitationIntensity) * s;
			Vector3 v6 = new Vector3(1f, 0.3f, -0.2f);
			Vector3 v7 = new Vector3(1f, 0.3f, -0.2f);
			if (m_lightningStrikePosition.HasValue)
			{
				v4 = Vector3.Max(new Vector3(m_lightningStrikeBrightness), v4);
			}
			float num = MathUtils.Lerp(CalculateDawnGlowIntensity(timeOfDay), 0f, precipitationIntensity);
			float num2 = MathUtils.Lerp(CalculateDuskGlowIntensity(timeOfDay), 0f, precipitationIntensity);
			float f2 = MathUtils.Saturate((direction.Y - 0.1f) / 0.4f);
			float s2 = num * MathUtils.Sqr(MathUtils.Saturate(0f - vector.X));
			float s3 = num2 * MathUtils.Sqr(MathUtils.Saturate(vector.X));
			return new Color(Vector3.Lerp(v5 + v6 * s2 + v7 * s3, v4, f2));
		}

		public void FillSkyVertexBuffer(SkyDome skyDome, float timeOfDay, float precipitationIntensity, int temperature)
		{
			for (int i = 0; i < 8; i++)
			{
				float x = (float)Math.PI / 2f * MathUtils.Sqr((float)i / 7f);
				for (int j = 0; j < 10; j++)
				{
					int num = j + i * 10;
					float x2 = (float)Math.PI * 2f * (float)j / 10f;
					float num2 = 1800f * MathUtils.Cos(x);
					skyDome.Vertices[num].Position.X = num2 * MathUtils.Sin(x2);
					skyDome.Vertices[num].Position.Z = num2 * MathUtils.Cos(x2);
					skyDome.Vertices[num].Position.Y = 1800f * MathUtils.Sin(x) - ((i == 0) ? 450f : 0f);
					skyDome.Vertices[num].Color = CalculateSkyColor(skyDome.Vertices[num].Position, timeOfDay, precipitationIntensity, temperature);
				}
			}
			skyDome.VertexBuffer.SetData(skyDome.Vertices, 0, skyDome.Vertices.Length);
		}

		public void FillSkyIndexBuffer(SkyDome skyDome)
		{
			int num = 0;
			for (int i = 0; i < 7; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					int num2 = j;
					int num3 = (j + 1) % 10;
					int num4 = i;
					int num5 = i + 1;
					skyDome.Indices[num++] = (ushort)(num2 + num4 * 10);
					skyDome.Indices[num++] = (ushort)(num3 + num4 * 10);
					skyDome.Indices[num++] = (ushort)(num3 + num5 * 10);
					skyDome.Indices[num++] = (ushort)(num3 + num5 * 10);
					skyDome.Indices[num++] = (ushort)(num2 + num5 * 10);
					skyDome.Indices[num++] = (ushort)(num2 + num4 * 10);
				}
			}
			for (int k = 2; k < 10; k++)
			{
				skyDome.Indices[num++] = 0;
				skyDome.Indices[num++] = (ushort)(k - 1);
				skyDome.Indices[num++] = (ushort)k;
			}
			skyDome.IndexBuffer.SetData(skyDome.Indices, 0, skyDome.Indices.Length);
		}

		public void FillStarsBuffers()
		{
			Random random = new Random(7);
			StarVertex[] array = new StarVertex[600];
			for (int i = 0; i < 150; i++)
			{
				Vector3 v;
				do
				{
					v = new Vector3(random.Float(-1f, 1f), random.Float(-1f, 1f), random.Float(-1f, 1f));
				}
				while (v.LengthSquared() > 1f);
				v = Vector3.Normalize(v);
				float s = 9f * random.NormalFloat(1f, 0.1f);
				float w = MathUtils.Saturate(random.NormalFloat(0.6f, 0.4f));
				Color color = new Color(new Vector4(random.Float(0.6f, 1f), 0.7f, random.Float(0.8f, 1f), w));
				Vector3 v2 = 900f * v;
				Vector3 vector = Vector3.Normalize(Vector3.Cross((v.X > v.Y) ? Vector3.UnitY : Vector3.UnitX, v));
				Vector3 v3 = Vector3.Normalize(Vector3.Cross(vector, v));
				Vector3 position = v2 + s * (-vector - v3);
				Vector3 position2 = v2 + s * (vector - v3);
				Vector3 position3 = v2 + s * (vector + v3);
				Vector3 position4 = v2 + s * (-vector + v3);
				StarVertex starVertex = array[i * 4] = new StarVertex
				{
					Position = position,
					TextureCoordinate = new Vector2(0f, 0f),
					Color = color
				};
				starVertex = (array[i * 4 + 1] = new StarVertex
				{
					Position = position2,
					TextureCoordinate = new Vector2(1f, 0f),
					Color = color
				});
				starVertex = (array[i * 4 + 2] = new StarVertex
				{
					Position = position3,
					TextureCoordinate = new Vector2(1f, 1f),
					Color = color
				});
				starVertex = (array[i * 4 + 3] = new StarVertex
				{
					Position = position4,
					TextureCoordinate = new Vector2(0f, 1f),
					Color = color
				});
			}
			m_starsVertexBuffer.SetData(array, 0, array.Length);
			ushort[] array2 = new ushort[900];
			for (int j = 0; j < 150; j++)
			{
				array2[j * 6] = (ushort)(j * 4);
				array2[j * 6 + 1] = (ushort)(j * 4 + 1);
				array2[j * 6 + 2] = (ushort)(j * 4 + 2);
				array2[j * 6 + 3] = (ushort)(j * 4 + 2);
				array2[j * 6 + 4] = (ushort)(j * 4 + 3);
				array2[j * 6 + 5] = (ushort)(j * 4);
			}
			m_starsIndexBuffer.SetData(array2, 0, array2.Length);
		}

		public static float CalculateDawnGlowIntensity(float timeOfDay)
		{
			return MathUtils.Max(1f - MathUtils.Abs(timeOfDay - 0.25f) / (71f / (226f * (float)Math.PI)) * 2f, 0f);
		}

		public static float CalculateDuskGlowIntensity(float timeOfDay)
		{
			return MathUtils.Max(1f - MathUtils.Abs(timeOfDay - 0.75f) / 0.100000024f * 2f, 0f);
		}
	}
}
