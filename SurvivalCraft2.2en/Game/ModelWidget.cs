using Engine;
using Engine.Graphics;

namespace Game
{
	public class ModelWidget : Widget
	{
		public static LitShader m_shader = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: false);

		public static LitShader m_shaderAlpha = new LitShader(1, useEmissionColor: false, useVertexColor: false, useTexture: true, useFog: false, useAlphaThreshold: true);

		public Model m_model;

		public Matrix?[] m_boneTransforms;

		public Matrix[] m_absoluteBoneTransforms;

		public Vector2 Size
		{
			get;
			set;
		}

		public Color Color
		{
			get;
			set;
		}

		public bool UseAlphaThreshold
		{
			get;
			set;
		}

		public bool IsPerspective
		{
			get;
			set;
		}

		public Vector3 OrthographicFrustumSize
		{
			get;
			set;
		}

		public Vector3 ViewPosition
		{
			get;
			set;
		}

		public Vector3 ViewTarget
		{
			get;
			set;
		}

		public float ViewFov
		{
			get;
			set;
		}

		public Matrix ModelMatrix
		{
			get;
			set;
		} = Matrix.Identity;


		public Vector3 AutoRotationVector
		{
			get;
			set;
		}

		public Model Model
		{
			get
			{
				return m_model;
			}
			set
			{
				if (value != m_model)
				{
					m_model = value;
					if (m_model != null)
					{
						m_boneTransforms = new Matrix?[m_model.Bones.Count];
						m_absoluteBoneTransforms = new Matrix[m_model.Bones.Count];
					}
					else
					{
						m_boneTransforms = null;
						m_absoluteBoneTransforms = null;
					}
				}
			}
		}

		public Texture2D TextureOverride
		{
			get;
			set;
		}

		public ModelWidget()
		{
			Size = new Vector2(1f / 0f);
			IsHitTestVisible = false;
			Color = Color.White;
			UseAlphaThreshold = false;
			IsPerspective = true;
			ViewPosition = new Vector3(0f, 0f, -5f);
			ViewTarget = new Vector3(0f, 0f, 0f);
			ViewFov = 1f;
			OrthographicFrustumSize = new Vector3(0f, 10f, 10f);
		}

		public Matrix? GetBoneTransform(int boneIndex)
		{
			return m_boneTransforms[boneIndex];
		}

		public void SetBoneTransform(int boneIndex, Matrix? transformation)
		{
			m_boneTransforms[boneIndex] = transformation;
		}

		public override void Draw(DrawContext dc)
		{
			if (Model == null)
			{
				return;
			}
			LitShader litShader = UseAlphaThreshold ? m_shaderAlpha : m_shader;
			litShader.Texture = TextureOverride;
			litShader.SamplerState = SamplerState.PointClamp;
			litShader.MaterialColor = new Vector4(Color * base.GlobalColorTransform);
			litShader.AmbientLightColor = new Vector3(0.66f, 0.66f, 0.66f);
			litShader.DiffuseLightColor1 = new Vector3(1f, 1f, 1f);
			litShader.LightDirection1 = Vector3.Normalize(new Vector3(1f, 1f, 1f));
			if (UseAlphaThreshold)
			{
				litShader.AlphaThreshold = 0f;
			}
			litShader.Transforms.View = Matrix.CreateLookAt(ViewPosition, ViewTarget, Vector3.UnitY);
			Viewport viewport = Display.Viewport;
			float num = base.ActualSize.X / base.ActualSize.Y;
			if (IsPerspective)
			{
				litShader.Transforms.Projection = Matrix.CreatePerspectiveFieldOfView(ViewFov, num, 0.1f, 100f) * MatrixUtils.CreateScaleTranslation(0.5f * base.ActualSize.X, -0.5f * base.ActualSize.Y, base.ActualSize.X / 2f, base.ActualSize.Y / 2f) * base.GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
			}
			else
			{
				Vector3 orthographicFrustumSize = OrthographicFrustumSize;
				if (orthographicFrustumSize.X < 0f)
				{
					orthographicFrustumSize.X = orthographicFrustumSize.Y / num;
				}
				else if (orthographicFrustumSize.Y < 0f)
				{
					orthographicFrustumSize.Y = orthographicFrustumSize.X * num;
				}
				litShader.Transforms.Projection = Matrix.CreateOrthographic(orthographicFrustumSize.X, orthographicFrustumSize.Y, 0f, OrthographicFrustumSize.Z) * MatrixUtils.CreateScaleTranslation(0.5f * base.ActualSize.X, -0.5f * base.ActualSize.Y, base.ActualSize.X / 2f, base.ActualSize.Y / 2f) * base.GlobalTransform * MatrixUtils.CreateScaleTranslation(2f / (float)viewport.Width, -2f / (float)viewport.Height, -1f, 1f);
			}
			Display.DepthStencilState = DepthStencilState.Default;
			Display.BlendState = BlendState.AlphaBlend;
			Display.RasterizerState = RasterizerState.CullNoneScissor;
			ProcessBoneHierarchy(Model.RootBone, Matrix.Identity, m_absoluteBoneTransforms);
			float num2 = (float)Time.RealTime + (float)(GetHashCode() % 1000) / 100f;
			Matrix m = (AutoRotationVector.LengthSquared() > 0f) ? Matrix.CreateFromAxisAngle(Vector3.Normalize(AutoRotationVector), AutoRotationVector.Length() * num2) : Matrix.Identity;
			foreach (ModelMesh mesh in Model.Meshes)
			{
				litShader.Transforms.World[0] = m_absoluteBoneTransforms[mesh.ParentBone.Index] * ModelMatrix * m;
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					if (meshPart.IndicesCount > 0)
					{
						Display.DrawIndexed(PrimitiveType.TriangleList, litShader, meshPart.VertexBuffer, meshPart.IndexBuffer, meshPart.StartIndex, meshPart.IndicesCount);
					}
				}
			}
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			base.IsDrawRequired = (Model != null);
			base.DesiredSize = Size;
		}

		public void ProcessBoneHierarchy(ModelBone modelBone, Matrix currentTransform, Matrix[] transforms)
		{
			Matrix m = modelBone.Transform;
			if (m_boneTransforms[modelBone.Index].HasValue)
			{
				Vector3 translation = m.Translation;
				m.Translation = Vector3.Zero;
				m *= m_boneTransforms[modelBone.Index].Value;
				m.Translation += translation;
				Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
			}
			else
			{
				Matrix.MultiplyRestricted(ref m, ref currentTransform, out transforms[modelBone.Index]);
			}
			foreach (ModelBone childBone in modelBone.ChildBones)
			{
				ProcessBoneHierarchy(childBone, transforms[modelBone.Index], transforms);
			}
		}
	}
}
