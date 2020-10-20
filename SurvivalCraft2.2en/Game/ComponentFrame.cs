using Engine;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentFrame : Component
	{
		public Vector3 m_position;

		public Quaternion m_rotation;

		public bool m_cachedMatrixValid;

		public Matrix m_cachedMatrix;

		public Vector3 Position
		{
			get
			{
				return m_position;
			}
			set
			{
				if (value != m_position)
				{
					m_cachedMatrixValid = false;
					m_position = value;
					this.PositionChanged?.Invoke(this);
				}
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return m_rotation;
			}
			set
			{
				value = Quaternion.Normalize(value);
				if (value != m_rotation)
				{
					m_cachedMatrixValid = false;
					m_rotation = value;
					this.RotationChanged?.Invoke(this);
				}
			}
		}

		public Matrix Matrix
		{
			get
			{
				if (!m_cachedMatrixValid)
				{
					m_cachedMatrix = Matrix.CreateFromQuaternion(Rotation);
					m_cachedMatrix.Translation = Position;
				}
				return m_cachedMatrix;
			}
		}

		public event Action<ComponentFrame> PositionChanged;

		public event Action<ComponentFrame> RotationChanged;

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			Position = valuesDictionary.GetValue<Vector3>("Position");
			Rotation = valuesDictionary.GetValue<Quaternion>("Rotation");
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("Position", Position);
			valuesDictionary.SetValue("Rotation", Rotation);
		}
	}
}
