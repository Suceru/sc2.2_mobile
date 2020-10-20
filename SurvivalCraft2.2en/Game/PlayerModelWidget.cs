using Engine;
using Engine.Graphics;
using System;

namespace Game
{
	public class PlayerModelWidget : CanvasWidget
	{
		public enum Shot
		{
			Body,
			Bust
		}

		public ModelWidget m_modelWidget;

		public CharacterSkinsCache m_internalCharacterSkinsCache;

		public CharacterSkinsCache m_characterSkinsCache;

		public Vector2? m_lastDrag;

		public float m_rotation;

		public CharacterSkinsCache CharacterSkinsCache
		{
			get
			{
				return m_characterSkinsCache;
			}
			set
			{
				if (value != null)
				{
					m_internalCharacterSkinsCache.Clear();
					m_characterSkinsCache = value;
				}
				else
				{
					m_characterSkinsCache = m_internalCharacterSkinsCache;
				}
			}
		}

		public Shot CameraShot
		{
			get;
			set;
		}

		public int AnimateHeadSeed
		{
			get;
			set;
		}

		public int AnimateHandsSeed
		{
			get;
			set;
		}

		public bool OuterClothing
		{
			get;
			set;
		}

		public PlayerClass PlayerClass
		{
			get;
			set;
		}

		public string CharacterSkinName
		{
			get;
			set;
		}

		public Texture2D CharacterSkinTexture
		{
			get;
			set;
		}

		public Texture2D OuterClothingTexture
		{
			get;
			set;
		}

		public PlayerModelWidget()
		{
			m_modelWidget = new ModelWidget
			{
				UseAlphaThreshold = true,
				IsPerspective = true
			};
			Children.Add(m_modelWidget);
			IsHitTestVisible = false;
			m_internalCharacterSkinsCache = new CharacterSkinsCache();
			m_characterSkinsCache = m_internalCharacterSkinsCache;
		}

		public override void Update()
		{
			if (base.Input.Press.HasValue)
			{
				if (m_lastDrag.HasValue)
				{
					m_rotation += 0.01f * (base.Input.Press.Value.X - m_lastDrag.Value.X);
					m_lastDrag = base.Input.Press.Value;
					base.Input.Clear();
				}
				else if (HitTestGlobal(base.Input.Press.Value) == this)
				{
					m_lastDrag = base.Input.Press.Value;
				}
			}
			else
			{
				m_lastDrag = null;
				m_rotation = MathUtils.NormalizeAngle(m_rotation);
				if (MathUtils.Abs(m_rotation) > 0.01f)
				{
					m_rotation *= MathUtils.PowSign(0.1f, Time.FrameDuration);
				}
				else
				{
					m_rotation = 0f;
				}
			}
			m_modelWidget.ModelMatrix = ((m_rotation != 0f) ? Matrix.CreateRotationY(m_rotation) : Matrix.Identity);
		}

		public override void MeasureOverride(Vector2 parentAvailableSize)
		{
			if (OuterClothing)
			{
				m_modelWidget.Model = CharacterSkinsManager.GetOuterClothingModel(PlayerClass);
			}
			else
			{
				m_modelWidget.Model = CharacterSkinsManager.GetPlayerModel(PlayerClass);
			}
			if (CameraShot == Shot.Body)
			{
				m_modelWidget.ViewPosition = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.46f, -3.2f) : new Vector3(0f, 1.39f, -3.04f));
				m_modelWidget.ViewTarget = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 0.9f, 0f) : new Vector3(0f, 0.86f, 0f));
				m_modelWidget.ViewFov = 0.57f;
			}
			else
			{
				if (CameraShot != Shot.Bust)
				{
					throw new InvalidOperationException("Unknown shot.");
				}
				m_modelWidget.ViewPosition = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.5f, -1.05f) : new Vector3(0f, 1.43f, -1f));
				m_modelWidget.ViewTarget = ((PlayerClass == PlayerClass.Male) ? new Vector3(0f, 1.5f, 0f) : new Vector3(0f, 1.43f, 0f));
				m_modelWidget.ViewFov = 0.57f;
			}
			if (OuterClothing)
			{
				m_modelWidget.TextureOverride = OuterClothingTexture;
			}
			else
			{
				m_modelWidget.TextureOverride = ((CharacterSkinName != null) ? CharacterSkinsCache.GetTexture(CharacterSkinName) : CharacterSkinTexture);
			}
			if (AnimateHeadSeed != 0)
			{
				int num = (AnimateHeadSeed < 0) ? GetHashCode() : AnimateHeadSeed;
				float num2 = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * (double)num, 10000.0);
				Vector2 vector = default(Vector2);
				vector.X = MathUtils.Lerp(-0.75f, 0.75f, SimplexNoise.OctavedNoise(num2 + 100f, 0.2f, 1, 2f, 0.5f));
				vector.Y = MathUtils.Lerp(-0.5f, 0.5f, SimplexNoise.OctavedNoise(num2 + 200f, 0.17f, 1, 2f, 0.5f));
				Matrix value = Matrix.CreateRotationX(vector.Y) * Matrix.CreateRotationZ(vector.X);
				m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Head").Index, value);
			}
			if (!OuterClothing && AnimateHandsSeed != 0)
			{
				int num3 = (AnimateHandsSeed < 0) ? GetHashCode() : AnimateHandsSeed;
				float num4 = (float)MathUtils.Remainder(Time.FrameStartTime + 1000.0 * (double)num3, 10000.0);
				Vector2 vector2 = default(Vector2);
				vector2.X = MathUtils.Lerp(0.2f, 0f, SimplexNoise.OctavedNoise(num4 + 100f, 0.7f, 1, 2f, 0.5f));
				vector2.Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num4 + 200f, 0.7f, 1, 2f, 0.5f));
				Vector2 vector3 = default(Vector2);
				vector3.X = MathUtils.Lerp(-0.2f, 0f, SimplexNoise.OctavedNoise(num4 + 300f, 0.7f, 1, 2f, 0.5f));
				vector3.Y = MathUtils.Lerp(-0.3f, 0.3f, SimplexNoise.OctavedNoise(num4 + 400f, 0.7f, 1, 2f, 0.5f));
				Matrix value2 = Matrix.CreateRotationX(vector2.Y) * Matrix.CreateRotationY(vector2.X);
				Matrix value3 = Matrix.CreateRotationX(vector3.Y) * Matrix.CreateRotationY(vector3.X);
				m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Hand1").Index, value2);
				m_modelWidget.SetBoneTransform(m_modelWidget.Model.FindBone("Hand2").Index, value3);
			}
			base.MeasureOverride(parentAvailableSize);
		}

		public override void UpdateCeases()
		{
			if (base.RootWidget == null)
			{
				if (m_internalCharacterSkinsCache.ContainsTexture(m_modelWidget.TextureOverride))
				{
					m_modelWidget.TextureOverride = null;
				}
				m_internalCharacterSkinsCache.Clear();
			}
			base.UpdateCeases();
		}
	}
}
