using GameEntitySystem;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBehaviorSelector : Component, IUpdateable
	{
		public ComponentCreature m_componentCreature;

		public List<ComponentBehavior> m_behaviors = new List<ComponentBehavior>();

		public static bool ShowAIBehavior;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void Update(float dt)
		{
			ComponentBehavior componentBehavior = null;
			if (m_componentCreature.ComponentHealth.Health > 0f)
			{
				float num = 0f;
				foreach (ComponentBehavior behavior in m_behaviors)
				{
					float importanceLevel = behavior.ImportanceLevel;
					if (importanceLevel > num)
					{
						num = importanceLevel;
						componentBehavior = behavior;
					}
				}
			}
			foreach (ComponentBehavior behavior2 in m_behaviors)
			{
				if (behavior2 == componentBehavior)
				{
					if (!behavior2.IsActive)
					{
						behavior2.IsActive = true;
					}
				}
				else if (behavior2.IsActive)
				{
					behavior2.IsActive = false;
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			foreach (ComponentBehavior item in base.Entity.FindComponents<ComponentBehavior>())
			{
				m_behaviors.Add(item);
			}
		}
	}
}
