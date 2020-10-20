using Engine;
using Engine.Graphics;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemModelsRenderer : Subsystem, IDrawable
	{
		public class ModelData : IComparable<ModelData>
		{
			public ComponentModel ComponentModel;

			public ComponentBody ComponentBody;

			public float Light;

			public double NextLightTime;

			public int LastAnimateFrame;

			public int CompareTo(ModelData other)
			{
				int num = (ComponentModel != null) ? ComponentModel.PrepareOrder : 0;
				int num2 = (other.ComponentModel != null) ? other.ComponentModel.PrepareOrder : 0;
				return num - num2;
			}
		}

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemSky m_subsystemSky;

		public SubsystemShadows m_subsystemShadows;

		public PrimitivesRenderer3D m_primitivesRenderer = new PrimitivesRenderer3D();

		public static ModelShader m_shaderOpaque = new ModelShader(useAlphaThreshold: false, 7);

		public static ModelShader m_shaderAlphaTested = new ModelShader(useAlphaThreshold: true, 7);

		public Dictionary<ComponentModel, ModelData> m_componentModels = new Dictionary<ComponentModel, ModelData>();

		public List<ModelData> m_modelsToPrepare = new List<ModelData>();

		public List<ModelData>[] m_modelsToDraw = new List<ModelData>[4]
		{
			new List<ModelData>(),
			new List<ModelData>(),
			new List<ModelData>(),
			new List<ModelData>()
		};

		public static bool DisableDrawingModels = false;

		public int ModelsDrawn;

		public int[] m_drawOrders = new int[4]
		{
			-10000,
			1,
			99,
			201
		};

		public PrimitivesRenderer3D PrimitivesRenderer => m_primitivesRenderer;

		public int[] DrawOrders => m_drawOrders;

		public void Draw(Camera camera, int drawOrder)
		{
			if (drawOrder == m_drawOrders[0])
			{
				ModelsDrawn = 0;
				List<ModelData>[] modelsToDraw = m_modelsToDraw;
				for (int i = 0; i < modelsToDraw.Length; i++)
				{
					modelsToDraw[i].Clear();
				}
				m_modelsToPrepare.Clear();
				foreach (ModelData value in m_componentModels.Values)
				{
					if (value.ComponentModel.Model != null)
					{
						value.ComponentModel.CalculateIsVisible(camera);
						if (value.ComponentModel.IsVisibleForCamera)
						{
							m_modelsToPrepare.Add(value);
						}
					}
				}
				m_modelsToPrepare.Sort();
				foreach (ModelData item in m_modelsToPrepare)
				{
					PrepareModel(item, camera);
					m_modelsToDraw[(int)item.ComponentModel.RenderingMode].Add(item);
				}
			}
			else if (!DisableDrawingModels)
			{
				if (drawOrder == m_drawOrders[1])
				{
					Display.DepthStencilState = DepthStencilState.Default;
					Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
					Display.BlendState = BlendState.Opaque;
					DrawModels(camera, m_modelsToDraw[0], null);
					Display.RasterizerState = RasterizerState.CullNoneScissor;
					DrawModels(camera, m_modelsToDraw[1], 0f);
					Display.RasterizerState = RasterizerState.CullCounterClockwiseScissor;
					m_primitivesRenderer.Flush(camera.ProjectionMatrix, clearAfterFlush: true, 0);
				}
				else if (drawOrder == m_drawOrders[2])
				{
					Display.DepthStencilState = DepthStencilState.Default;
					Display.RasterizerState = RasterizerState.CullNoneScissor;
					Display.BlendState = BlendState.AlphaBlend;
					DrawModels(camera, m_modelsToDraw[2], null);
				}
				else if (drawOrder == m_drawOrders[3])
				{
					Display.DepthStencilState = DepthStencilState.Default;
					Display.RasterizerState = RasterizerState.CullNoneScissor;
					Display.BlendState = BlendState.AlphaBlend;
					DrawModels(camera, m_modelsToDraw[3], null);
					m_primitivesRenderer.Flush(camera.ProjectionMatrix);
				}
			}
			else
			{
				m_primitivesRenderer.Clear();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_subsystemShadows = base.Project.FindSubsystem<SubsystemShadows>(throwOnError: true);
		}

		public override void OnEntityAdded(Entity entity)
		{
			foreach (ComponentModel item in entity.FindComponents<ComponentModel>())
			{
				ModelData value = new ModelData
				{
					ComponentModel = item,
					ComponentBody = item.Entity.FindComponent<ComponentBody>(),
					Light = m_subsystemSky.SkyLightIntensity
				};
				m_componentModels.Add(item, value);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			foreach (ComponentModel item in entity.FindComponents<ComponentModel>())
			{
				m_componentModels.Remove(item);
			}
		}

		public void PrepareModel(ModelData modelData, Camera camera)
		{
			if (Time.FrameIndex > modelData.LastAnimateFrame)
			{
				modelData.ComponentModel.Animate();
				modelData.LastAnimateFrame = Time.FrameIndex;
			}
			if (Time.FrameStartTime >= modelData.NextLightTime)
			{
				float? num = CalculateModelLight(modelData);
				if (num.HasValue)
				{
					modelData.Light = num.Value;
				}
				modelData.NextLightTime = Time.FrameStartTime + 0.1;
			}
			modelData.ComponentModel.CalculateAbsoluteBonesTransforms(camera);
		}

		public void DrawModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold)
		{
			DrawInstancedModels(camera, modelsData, alphaThreshold);
			DrawModelsExtras(camera, modelsData);
		}

		public void DrawInstancedModels(Camera camera, List<ModelData> modelsData, float? alphaThreshold)
		{
			ModelShader modelShader = alphaThreshold.HasValue ? m_shaderAlphaTested : m_shaderOpaque;
			modelShader.LightDirection1 = -Vector3.TransformNormal(LightingManager.DirectionToLight1, camera.ViewMatrix);
			modelShader.LightDirection2 = -Vector3.TransformNormal(LightingManager.DirectionToLight2, camera.ViewMatrix);
			modelShader.FogColor = new Vector3(m_subsystemSky.ViewFogColor);
			modelShader.FogStartInvLength = new Vector2(m_subsystemSky.ViewFogRange.X, 1f / (m_subsystemSky.ViewFogRange.Y - m_subsystemSky.ViewFogRange.X));
			modelShader.FogYMultiplier = m_subsystemSky.VisibilityRangeYMultiplier;
			modelShader.WorldUp = Vector3.TransformNormal(Vector3.UnitY, camera.ViewMatrix);
			modelShader.Transforms.View = Matrix.Identity;
			modelShader.Transforms.Projection = camera.ProjectionMatrix;
			modelShader.SamplerState = SamplerState.PointClamp;
			if (alphaThreshold.HasValue)
			{
				modelShader.AlphaThreshold = alphaThreshold.Value;
			}
			foreach (ModelData modelsDatum in modelsData)
			{
				ComponentModel componentModel = modelsDatum.ComponentModel;
				Vector3 v = componentModel.DiffuseColor.HasValue ? componentModel.DiffuseColor.Value : Vector3.One;
				float num = componentModel.Opacity.HasValue ? componentModel.Opacity.Value : 1f;
				modelShader.InstancesCount = componentModel.AbsoluteBoneTransformsForCamera.Length;
				modelShader.MaterialColor = new Vector4(v * num, num);
				modelShader.EmissionColor = (componentModel.EmissionColor.HasValue ? componentModel.EmissionColor.Value : Vector4.Zero);
				modelShader.AmbientLightColor = new Vector3(LightingManager.LightAmbient * modelsDatum.Light);
				modelShader.DiffuseLightColor1 = new Vector3(modelsDatum.Light);
				modelShader.DiffuseLightColor2 = new Vector3(modelsDatum.Light);
				modelShader.Texture = componentModel.TextureOverride;
				Array.Copy(componentModel.AbsoluteBoneTransformsForCamera, modelShader.Transforms.World, componentModel.AbsoluteBoneTransformsForCamera.Length);
				InstancedModelData instancedModelData = InstancedModelsManager.GetInstancedModelData(componentModel.Model, componentModel.MeshDrawOrders);
				Display.DrawIndexed(PrimitiveType.TriangleList, modelShader, instancedModelData.VertexBuffer, instancedModelData.IndexBuffer, 0, instancedModelData.IndexBuffer.IndicesCount);
				ModelsDrawn++;
			}
		}

		public void DrawModelsExtras(Camera camera, List<ModelData> modelsData)
		{
			foreach (ModelData modelsDatum in modelsData)
			{
				if (modelsDatum.ComponentBody != null && modelsDatum.ComponentModel.CastsShadow)
				{
					Vector3 shadowPosition = modelsDatum.ComponentBody.Position + new Vector3(0f, 0.1f, 0f);
					BoundingBox boundingBox = modelsDatum.ComponentBody.BoundingBox;
					float shadowDiameter = 2.25f * (boundingBox.Max.X - boundingBox.Min.X);
					m_subsystemShadows.QueueShadow(camera, shadowPosition, shadowDiameter, modelsDatum.ComponentModel.Opacity ?? 1f);
				}
				modelsDatum.ComponentModel.DrawExtras(camera);
			}
		}

		public float? CalculateModelLight(ModelData modelData)
		{
			Vector3 p;
			if (modelData.ComponentBody != null)
			{
				p = modelData.ComponentBody.Position;
				p.Y += 0.95f * (modelData.ComponentBody.BoundingBox.Max.Y - modelData.ComponentBody.BoundingBox.Min.Y);
			}
			else
			{
				Matrix? boneTransform = modelData.ComponentModel.GetBoneTransform(modelData.ComponentModel.Model.RootBone.Index);
				p = ((!boneTransform.HasValue) ? Vector3.Zero : (boneTransform.Value.Translation + new Vector3(0f, 0.9f, 0f)));
			}
			return LightingManager.CalculateSmoothLight(m_subsystemTerrain, p);
		}
	}
}
