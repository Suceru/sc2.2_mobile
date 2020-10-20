using Engine;
using GameEntitySystem;
using System.Collections.Generic;

namespace Game
{
	public class SubsystemBlockEntities : Subsystem
	{
		public Dictionary<Point3, ComponentBlockEntity> m_blockEntities = new Dictionary<Point3, ComponentBlockEntity>();

		public ComponentBlockEntity GetBlockEntity(int x, int y, int z)
		{
			m_blockEntities.TryGetValue(new Point3(x, y, z), out ComponentBlockEntity value);
			return value;
		}

		public override void OnEntityAdded(Entity entity)
		{
			ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
			if (componentBlockEntity != null)
			{
				m_blockEntities.Add(componentBlockEntity.Coordinates, componentBlockEntity);
			}
		}

		public override void OnEntityRemoved(Entity entity)
		{
			ComponentBlockEntity componentBlockEntity = entity.FindComponent<ComponentBlockEntity>();
			if (componentBlockEntity != null)
			{
				m_blockEntities.Remove(componentBlockEntity.Coordinates);
			}
		}
	}
}
