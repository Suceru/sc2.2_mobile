using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentLoot : Component, IUpdateable
	{
		public struct Loot
		{
			public int Value;

			public int MinCount;

			public int MaxCount;

			public float Probability;
		}

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemPickables m_subsystemPickables;

		public ComponentCreature m_componentCreature;

		public List<Loot> m_lootList;

		public List<Loot> m_lootOnFireList;

		public Random m_random = new Random();

		public bool m_lootDropped;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public static List<Loot> ParseLootList(ValuesDictionary lootVd)
		{
			List<Loot> list = new List<Loot>();
			foreach (string value in lootVd.Values)
			{
				list.Add(ParseLoot(value));
			}
			list.Sort((Loot l1, Loot l2) => l1.Value - l2.Value);
			return list;
		}

		public void Update(float dt)
		{
			if (!m_lootDropped && m_componentCreature.ComponentHealth.DeathTime.HasValue && m_subsystemGameInfo.TotalElapsedGameTime >= m_componentCreature.ComponentHealth.DeathTime.Value + (double)m_componentCreature.ComponentHealth.CorpseDuration)
			{
				bool num = m_componentCreature.Entity.FindComponent<ComponentOnFire>()?.IsOnFire ?? false;
				m_lootDropped = true;
				foreach (Loot item in num ? m_lootOnFireList : m_lootList)
				{
					if (m_random.Float(0f, 1f) < item.Probability)
					{
						int num2 = m_random.Int(item.MinCount, item.MaxCount);
						for (int i = 0; i < num2; i++)
						{
							Vector3 position = (m_componentCreature.ComponentBody.BoundingBox.Min + m_componentCreature.ComponentBody.BoundingBox.Max) / 2f;
							m_subsystemPickables.AddPickable(item.Value, 1, position, null, null);
						}
					}
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemPickables = base.Project.FindSubsystem<SubsystemPickables>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
			m_lootDropped = valuesDictionary.GetValue<bool>("LootDropped");
			m_lootList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("Loot"));
			m_lootOnFireList = ParseLootList(valuesDictionary.GetValue<ValuesDictionary>("LootOnFire"));
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("LootDropped", m_lootDropped);
		}

		public static Loot ParseLoot(string lootString)
		{
			string[] array = lootString.Split(';');
			if (array.Length >= 3)
			{
				Block block = BlocksManager.FindBlockByTypeName(array[0], throwIfNotFound: true);
				Loot result = default(Loot);
				result.Value = block.BlockIndex;
				result.MinCount = int.Parse(array[1], CultureInfo.InvariantCulture);
				result.MaxCount = int.Parse(array[2], CultureInfo.InvariantCulture);
				result.Probability = ((array.Length >= 4) ? float.Parse(array[3], CultureInfo.InvariantCulture) : 1f);
				return result;
			}
			throw new InvalidOperationException("Invalid loot string.");
		}
	}
}
