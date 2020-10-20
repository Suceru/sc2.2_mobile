using Engine;
using GameEntitySystem;
using System;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentCraftingTable : ComponentInventoryBase
	{
		public int m_craftingGridSize;

		public string[] m_matchedIngredients = new string[9];

		public CraftingRecipe m_matchedRecipe;

		public int RemainsSlotIndex => SlotsCount - 1;

		public int ResultSlotIndex => SlotsCount - 2;

		public override int GetSlotCapacity(int slotIndex, int value)
		{
			if (slotIndex < SlotsCount - 2)
			{
				return base.GetSlotCapacity(slotIndex, value);
			}
			return 0;
		}

		public override void AddSlotItems(int slotIndex, int value, int count)
		{
			base.AddSlotItems(slotIndex, value, count);
			UpdateCraftingResult();
		}

		public override int RemoveSlotItems(int slotIndex, int count)
		{
			int num = 0;
			if (slotIndex == ResultSlotIndex)
			{
				if (m_matchedRecipe != null)
				{
					if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
					{
						if (m_slots[RemainsSlotIndex].Count == 0 || m_slots[RemainsSlotIndex].Value == m_matchedRecipe.RemainsValue)
						{
							int num2 = BlocksManager.Blocks[Terrain.ExtractContents(m_matchedRecipe.RemainsValue)].MaxStacking - m_slots[RemainsSlotIndex].Count;
							count = MathUtils.Min(count, num2 / m_matchedRecipe.RemainsCount * m_matchedRecipe.ResultCount);
						}
						else
						{
							count = 0;
						}
					}
					count = count / m_matchedRecipe.ResultCount * m_matchedRecipe.ResultCount;
					num = base.RemoveSlotItems(slotIndex, count);
					if (num > 0)
					{
						for (int i = 0; i < 9; i++)
						{
							if (!string.IsNullOrEmpty(m_matchedIngredients[i]))
							{
								int index = i % 3 + m_craftingGridSize * (i / 3);
								m_slots[index].Count = MathUtils.Max(m_slots[index].Count - num / m_matchedRecipe.ResultCount, 0);
							}
						}
						if (m_matchedRecipe.RemainsValue != 0 && m_matchedRecipe.RemainsCount > 0)
						{
							m_slots[RemainsSlotIndex].Value = m_matchedRecipe.RemainsValue;
							m_slots[RemainsSlotIndex].Count += num / m_matchedRecipe.ResultCount * m_matchedRecipe.RemainsCount;
						}
						ComponentPlayer componentPlayer = FindInteractingPlayer();
						if (componentPlayer != null && componentPlayer.PlayerStats != null)
						{
							componentPlayer.PlayerStats.ItemsCrafted += num;
						}
					}
				}
			}
			else
			{
				num = base.RemoveSlotItems(slotIndex, count);
			}
			UpdateCraftingResult();
			return num;
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_craftingGridSize = (int)MathUtils.Sqrt(SlotsCount - 2);
			if (m_craftingGridSize < 1 || m_craftingGridSize > 3)
			{
				throw new InvalidOperationException("Invalid crafting grid size.");
			}
			UpdateCraftingResult();
		}

		public void UpdateCraftingResult()
		{
			int num = 2147483647;
			for (int i = 0; i < m_craftingGridSize; i++)
			{
				for (int j = 0; j < m_craftingGridSize; j++)
				{
					int num2 = i + j * 3;
					int slotIndex = i + j * m_craftingGridSize;
					int slotValue = GetSlotValue(slotIndex);
					int num3 = Terrain.ExtractContents(slotValue);
					int num4 = Terrain.ExtractData(slotValue);
					int slotCount = GetSlotCount(slotIndex);
					if (slotCount > 0)
					{
						Block block = BlocksManager.Blocks[num3];
						m_matchedIngredients[num2] = block.CraftingId + ":" + num4.ToString(CultureInfo.InvariantCulture);
						num = MathUtils.Min(num, slotCount);
					}
					else
					{
						m_matchedIngredients[num2] = null;
					}
				}
			}
			ComponentPlayer componentPlayer = FindInteractingPlayer();
			float playerLevel = componentPlayer?.PlayerData.Level ?? 1f;
			CraftingRecipe craftingRecipe = CraftingRecipesManager.FindMatchingRecipe(base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true), m_matchedIngredients, 0f, playerLevel);
			if (craftingRecipe != null && craftingRecipe.ResultValue != 0)
			{
				m_matchedRecipe = craftingRecipe;
				m_slots[ResultSlotIndex].Value = craftingRecipe.ResultValue;
				m_slots[ResultSlotIndex].Count = craftingRecipe.ResultCount * num;
			}
			else
			{
				m_matchedRecipe = null;
				m_slots[ResultSlotIndex].Value = 0;
				m_slots[ResultSlotIndex].Count = 0;
			}
			if (craftingRecipe != null && !string.IsNullOrEmpty(craftingRecipe.Message))
			{
				componentPlayer?.ComponentGui.DisplaySmallMessage(craftingRecipe.Message, Color.White, blinking: true, playNotificationSound: true);
			}
		}
	}
}
