using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentDispenser : ComponentInventoryBase
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemPickables m_subsystemPickables;

		public SubsystemProjectiles m_subsystemProjectiles;

		public ComponentBlockEntity m_componentBlockEntity;

		public Random m_random = new Random();

		public void Dispense()
		{
			Point3 coordinates = m_componentBlockEntity.Coordinates;
			int data = Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(coordinates.X, coordinates.Y, coordinates.Z));
			int direction = DispenserBlock.GetDirection(data);
			DispenserBlock.Mode mode = DispenserBlock.GetMode(data);
			int num = 0;
			int slotValue;
			while (true)
			{
				if (num < SlotsCount)
				{
					slotValue = GetSlotValue(num);
					int slotCount = GetSlotCount(num);
					if (slotValue != 0 && slotCount > 0)
					{
						break;
					}
					num++;
					continue;
				}
				return;
			}
			int num2 = RemoveSlotItems(num, 1);
			for (int i = 0; i < num2; i++)
			{
				DispenseItem(coordinates, direction, slotValue, mode);
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_subsystemProjectiles = base.Project.FindSubsystem<SubsystemProjectiles>(throwOnError: true);
			m_componentBlockEntity = base.Entity.FindComponent<ComponentBlockEntity>(throwOnError: true);
		}

		public void DispenseItem(Point3 point, int face, int value, DispenserBlock.Mode mode)
		{
			Vector3 vector = CellFace.FaceToVector3(face);
			Vector3 position = new Vector3((float)point.X + 0.5f, (float)point.Y + 0.5f, (float)point.Z + 0.5f) + 0.6f * vector;
			if (mode == DispenserBlock.Mode.Dispense)
			{
				float s = 1.8f;
				m_subsystemPickables.AddPickable(value, 1, position, s * (vector + m_random.Vector3(0.2f)), null);
				m_subsystemAudio.PlaySound("Audio/DispenserDispense", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 3f, autoDelay: true);
				return;
			}
			float s2 = m_random.Float(39f, 41f);
			if (m_subsystemProjectiles.FireProjectile(value, position, s2 * (vector + m_random.Vector3(0.025f) + new Vector3(0f, 0.05f, 0f)), Vector3.Zero, null) != null)
			{
				m_subsystemAudio.PlaySound("Audio/DispenserShoot", 1f, 0f, new Vector3(position.X, position.Y, position.Z), 4f, autoDelay: true);
			}
			else
			{
				DispenseItem(point, face, value, DispenserBlock.Mode.Dispense);
			}
		}
	}
}
