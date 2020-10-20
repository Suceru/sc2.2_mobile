using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public abstract class ComponentInventoryBase : Component, IInventory
	{
		protected class Slot
		{
			public int Value;

			public int Count;
		}

		protected List<Slot> m_slots = new List<Slot>();

		public Random m_random = new Random();

		Project IInventory.Project => base.Project;

		public virtual int SlotsCount => m_slots.Count;

		public virtual int VisibleSlotsCount
		{
			get
			{
				return SlotsCount;
			}
			set
			{
			}
		}

		public virtual int ActiveSlotIndex
		{
			get
			{
				return -1;
			}
			set
			{
			}
		}

		public static int FindAcquireSlotForItem(IInventory inventory, int value)
		{
			for (int i = 0; i < inventory.SlotsCount; i++)
			{
				if (inventory.GetSlotCount(i) > 0 && inventory.GetSlotValue(i) == value && inventory.GetSlotCount(i) < inventory.GetSlotCapacity(i, value))
				{
					return i;
				}
			}
			for (int j = 0; j < inventory.SlotsCount; j++)
			{
				if (inventory.GetSlotCount(j) == 0 && inventory.GetSlotCapacity(j, value) > 0)
				{
					return j;
				}
			}
			return -1;
		}

		public static int AcquireItems(IInventory inventory, int value, int count)
		{
			while (count > 0)
			{
				int num = FindAcquireSlotForItem(inventory, value);
				if (num < 0)
				{
					break;
				}
				inventory.AddSlotItems(num, value, 1);
				count--;
			}
			return count;
		}

		public ComponentPlayer FindInteractingPlayer()
		{
			ComponentPlayer componentPlayer = base.Entity.FindComponent<ComponentPlayer>();
			if (componentPlayer == null)
			{
				ComponentBlockEntity componentBlockEntity = base.Entity.FindComponent<ComponentBlockEntity>();
				if (componentBlockEntity != null)
				{
					Vector3 position = new Vector3(componentBlockEntity.Coordinates);
					componentPlayer = base.Project.FindSubsystem<SubsystemPlayers>(throwOnError: true).FindNearestPlayer(position);
				}
			}
			return componentPlayer;
		}

		public static void DropSlotItems(IInventory inventory, int slotIndex, Vector3 position, Vector3 velocity)
		{
			int slotCount = inventory.GetSlotCount(slotIndex);
			if (slotCount > 0)
			{
				int slotValue = inventory.GetSlotValue(slotIndex);
				int num = inventory.RemoveSlotItems(slotIndex, slotCount);
				if (num > 0)
				{
					inventory.Project.FindSubsystem<SubsystemPickables>(throwOnError: true).AddPickable(slotValue, num, position, velocity, null);
				}
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			int value = valuesDictionary.GetValue<int>("SlotsCount");
			for (int i = 0; i < value; i++)
			{
				m_slots.Add(new Slot());
			}
			ValuesDictionary value2 = valuesDictionary.GetValue<ValuesDictionary>("Slots");
			for (int j = 0; j < m_slots.Count; j++)
			{
				ValuesDictionary value3 = value2.GetValue<ValuesDictionary>("Slot" + j.ToString(CultureInfo.InvariantCulture), null);
				if (value3 != null)
				{
					Slot slot = m_slots[j];
					slot.Value = value3.GetValue<int>("Contents");
					slot.Count = value3.GetValue<int>("Count");
				}
			}
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Slots", valuesDictionary2);
			for (int i = 0; i < m_slots.Count; i++)
			{
				Slot slot = m_slots[i];
				if (slot.Count > 0)
				{
					ValuesDictionary valuesDictionary3 = new ValuesDictionary();
					valuesDictionary2.SetValue("Slot" + i.ToString(CultureInfo.InvariantCulture), valuesDictionary3);
					valuesDictionary3.SetValue("Contents", slot.Value);
					valuesDictionary3.SetValue("Count", slot.Count);
				}
			}
		}

		public virtual int GetSlotValue(int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				if (m_slots[slotIndex].Count <= 0)
				{
					return 0;
				}
				return m_slots[slotIndex].Value;
			}
			return 0;
		}

		public virtual int GetSlotCount(int slotIndex)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				return m_slots[slotIndex].Count;
			}
			return 0;
		}

		public virtual int GetSlotCapacity(int slotIndex, int value)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				return BlocksManager.Blocks[Terrain.ExtractContents(value)].MaxStacking;
			}
			return 0;
		}

		public virtual int GetSlotProcessCapacity(int slotIndex, int value)
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
			return 0;
		}

		public virtual void AddSlotItems(int slotIndex, int value, int count)
		{
			if (count > 0 && slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				Slot slot = m_slots[slotIndex];
				if ((GetSlotCount(slotIndex) != 0 && GetSlotValue(slotIndex) != value) || GetSlotCount(slotIndex) + count > GetSlotCapacity(slotIndex, value))
				{
					throw new InvalidOperationException("Cannot add slot items.");
				}
				slot.Value = value;
				slot.Count += count;
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
			processedValue = value;
			processedCount = count;
		}

		public virtual int RemoveSlotItems(int slotIndex, int count)
		{
			if (slotIndex >= 0 && slotIndex < m_slots.Count)
			{
				Slot slot = m_slots[slotIndex];
				count = MathUtils.Min(count, GetSlotCount(slotIndex));
				slot.Count -= count;
				return count;
			}
			return 0;
		}

		public void DropAllItems(Vector3 position)
		{
			for (int i = 0; i < SlotsCount; i++)
			{
				DropSlotItems(this, i, position, m_random.Float(5f, 10f) * Vector3.Normalize(new Vector3(m_random.Float(-1f, 1f), m_random.Float(1f, 2f), m_random.Float(-1f, 1f))));
			}
		}
	}
}
