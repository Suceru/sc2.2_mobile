using Engine;
using GameEntitySystem;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ComponentCreativeInventory : Component, IInventory
	{
		public List<int> m_slots = new List<int>();

		public int m_activeSlotIndex;

		public int m_visibleSlotsCount = 10;

		public const int m_largeNumber = 9999;

		public int OpenSlotsCount
		{
			get;
			set;
		}

		public int CategoryIndex
		{
			get;
			set;
		}

		public int PageIndex
		{
			get;
			set;
		}

		Project IInventory.Project => base.Project;

		public int ActiveSlotIndex
		{
			get
			{
				return m_activeSlotIndex;
			}
			set
			{
				m_activeSlotIndex = MathUtils.Clamp(value, 0, VisibleSlotsCount - 1);
			}
		}

		public int SlotsCount => m_slots.Count;

		public int VisibleSlotsCount
		{
			get
			{
				return m_visibleSlotsCount;
			}
			set
			{
				value = MathUtils.Clamp(value, 0, 10);
				if (value == m_visibleSlotsCount)
				{
					return;
				}
				m_visibleSlotsCount = value;
				ActiveSlotIndex = ActiveSlotIndex;
				ComponentFrame componentFrame = base.Entity.FindComponent<ComponentFrame>();
				if (componentFrame != null)
				{
					Vector3 position = componentFrame.Position + new Vector3(0f, 0.5f, 0f);
					Vector3 velocity = 1f * componentFrame.Rotation.GetForwardVector();
					for (int i = m_visibleSlotsCount; i < 10; i++)
					{
						ComponentInventoryBase.DropSlotItems(this, i, position, velocity);
					}
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_activeSlotIndex = valuesDictionary.GetValue<int>("ActiveSlotIndex");
			OpenSlotsCount = valuesDictionary.GetValue<int>("OpenSlotsCount");
			CategoryIndex = valuesDictionary.GetValue<int>("CategoryIndex");
			PageIndex = valuesDictionary.GetValue<int>("PageIndex");
			for (int i = 0; i < OpenSlotsCount; i++)
			{
				m_slots.Add(0);
			}
			foreach (Block item in BlocksManager.Blocks.OrderBy((Block b) => b.DisplayOrder))
			{
				foreach (int creativeValue in item.GetCreativeValues())
				{
					m_slots.Add(creativeValue);
				}
			}
			ValuesDictionary value = valuesDictionary.GetValue<ValuesDictionary>("Slots", null);
			if (value == null)
			{
				return;
			}
			for (int j = 0; j < OpenSlotsCount; j++)
			{
				ValuesDictionary value2 = value.GetValue<ValuesDictionary>("Slot" + j.ToString(CultureInfo.InvariantCulture), null);
				if (value2 != null)
				{
					m_slots[j] = value2.GetValue<int>("Contents");
				}
			}
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("ActiveSlotIndex", m_activeSlotIndex);
			valuesDictionary.SetValue("CategoryIndex", CategoryIndex);
			valuesDictionary.SetValue("PageIndex", PageIndex);
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Slots", valuesDictionary2);
			for (int i = 0; i < OpenSlotsCount; i++)
			{
				if (m_slots[i] != 0)
				{
					ValuesDictionary valuesDictionary3 = new ValuesDictionary();
					valuesDictionary2.SetValue("Slot" + i.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
					valuesDictionary3.SetValue("Contents", m_slots[i]);
				}
			}
		}

		public int GetSlotValue(int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				return m_slots[slotIndex];
			}
			return 0;
		}

		public int GetSlotCount(int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				if (m_slots[slotIndex] == 0)
				{
					return 0;
				}
				return 9999;
			}
			return 0;
		}

		public int GetSlotCapacity(int slotIndex, int value)
		{
			if (slotIndex >= VisibleSlotsCount && slotIndex < 10)
			{
				return 0;
			}
			if (slotIndex >= 0 && slotIndex < OpenSlotsCount)
			{
				return 99980001;
			}
			int num = Terrain.ExtractContents(value);
			if (BlocksManager.Blocks[num].IsNonDuplicable)
			{
				return 9999;
			}
			return 99980001;
		}

		public int GetSlotProcessCapacity(int slotIndex, int value)
		{
			int slotCount = GetSlotCount(slotIndex);
			int slotValue = GetSlotValue(slotIndex);
			if (slotCount > 0 && slotValue != 0)
			{
				SubsystemBlockBehavior[] blockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(slotValue));
				for (int i = 0; i < blockBehaviors.Length; i++)
				{
					int processInventoryItemCapacity = blockBehaviors[i].GetProcessInventoryItemCapacity(this, slotIndex, value);
					if (processInventoryItemCapacity > 0)
					{
						return processInventoryItemCapacity;
					}
				}
			}
			if (slotIndex < OpenSlotsCount)
			{
				return 0;
			}
			return 9999;
		}

		public void AddSlotItems(int slotIndex, int value, int count)
		{
			if (slotIndex >= 0 && slotIndex < OpenSlotsCount)
			{
				m_slots[slotIndex] = value;
			}
		}

		public virtual void ProcessSlotItems(int slotIndex, int value, int count, int processCount, out int processedValue, out int processedCount)
		{
			int slotCount = GetSlotCount(slotIndex);
			int slotValue = GetSlotValue(slotIndex);
			if (slotCount > 0 && slotValue != 0)
			{
				SubsystemBlockBehavior[] blockBehaviors = base.Project.FindSubsystem<SubsystemBlockBehaviors>(throwOnError: true).GetBlockBehaviors(Terrain.ExtractContents(slotValue));
				foreach (SubsystemBlockBehavior subsystemBlockBehavior in blockBehaviors)
				{
					int processInventoryItemCapacity = subsystemBlockBehavior.GetProcessInventoryItemCapacity(this, slotIndex, value);
					if (processInventoryItemCapacity > 0)
					{
						subsystemBlockBehavior.ProcessInventoryItem(this, slotIndex, value, count, MathUtils.Min(processInventoryItemCapacity, processCount), out processedValue, out processedCount);
						return;
					}
				}
			}
			if (slotIndex >= OpenSlotsCount)
			{
				processedValue = 0;
				processedCount = 0;
			}
			else
			{
				processedValue = value;
				processedCount = count;
			}
		}

		public int RemoveSlotItems(int slotIndex, int count)
		{
			if (slotIndex >= 0 && slotIndex < OpenSlotsCount)
			{
				int num = Terrain.ExtractContents(m_slots[slotIndex]);
				if (BlocksManager.Blocks[num].IsNonDuplicable)
				{
					m_slots[slotIndex] = 0;
					return 1;
				}
				if (count >= 9999)
				{
					m_slots[slotIndex] = 0;
					return 1;
				}
			}
			return 1;
		}

		public void DropAllItems(Vector3 position)
		{
		}
	}
}
