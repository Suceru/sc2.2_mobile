using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentSimpleModel : ComponentModel
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public ComponentSpawn m_componentSpawn;

		public override void Animate()
		{
			if (m_componentSpawn != null)
			{
				base.Opacity = ((m_componentSpawn.SpawnDuration > 0f) ? ((float)MathUtils.Saturate((m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.SpawnTime) / (double)m_componentSpawn.SpawnDuration)) : 1f);
				if (m_componentSpawn.DespawnTime.HasValue)
				{
					base.Opacity = MathUtils.Min(base.Opacity.Value, (float)MathUtils.Saturate(1.0 - (m_subsystemGameInfo.TotalElapsedGameTime - m_componentSpawn.DespawnTime.Value) / (double)m_componentSpawn.DespawnDuration));
				}
			}
			SetBoneTransform(base.Model.RootBone.Index, m_componentFrame.Matrix);
			base.Animate();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_componentSpawn = base.Entity.FindComponent<ComponentSpawn>();
			base.Load(valuesDictionary, idToEntityMap);
		}
	}
}
