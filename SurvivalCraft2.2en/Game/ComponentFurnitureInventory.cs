using Engine;
using GameEntitySystem;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ComponentFurnitureInventory : Component, IInventory
	{
		public SubsystemFurnitureBlockBehavior m_subsystemFurnitureBlockBehavior;

		public List<int> m_slots = new List<int>();

		public const int m_largeNumber = 9999;

		public int PageIndex
		{
			get;
			set;
		}

		public FurnitureSet FurnitureSet
		{
			get;
			set;
		}

		Project IInventory.Project => base.Project;

		public int ActiveSlotIndex
		{
			get
			{
				return -1;
			}
			set
			{
			}
		}

		public int SlotsCount => m_slots.Count;

		public int VisibleSlotsCount
		{
			get
			{
				return SlotsCount;
			}
			set
			{
			}
		}

		public void FillSlots()
		{
			m_subsystemFurnitureBlockBehavior.GarbageCollectDesigns();
			m_slots.Clear();
			for (int i = 0; i < 1024; i++)
			{
				FurnitureDesign design = m_subsystemFurnitureBlockBehavior.GetDesign(i);
				if (design != null)
				{
					int num = (from f in design.ListChain()
						select f.Index).Min();
					if (design.Index == num)
					{
						int data = FurnitureBlock.SetDesignIndex(0, i, design.ShadowStrengthFactor, design.IsLightEmitter);
						int item = Terrain.MakeBlockValue(227, 0, data);
						m_slots.Add(item);
					}
				}
			}
		}

		public void ClearSlots()
		{
			m_slots.Clear();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemFurnitureBlockBehavior = base.Project.FindSubsystem<SubsystemFurnitureBlockBehavior>(throwOnError: true);
			string furnitureSetName = valuesDictionary.GetValue<string>("FurnitureSet");
			FurnitureSet = m_subsystemFurnitureBlockBehavior.FurnitureSets.FirstOrDefault((FurnitureSet f) => f.Name == furnitureSetName);
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("FurnitureSet", (FurnitureSet != null) ? FurnitureSet.Name : string.Empty);
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
			return 9999;
		}

		public void AddSlotItems(int slotIndex, int value, int count)
		{
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
			processedValue = 0;
			processedCount = 0;
		}

		public int RemoveSlotItems(int slotIndex, int count)
		{
			return 1;
		}

		public void DropAllItems(Vector3 position)
		{
		}
	}
}
