using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemDispenserBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemBlockEntities m_subsystemBlockEntities;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemAudio m_subsystemAudio;

		public override int[] HandledBlocks => new int[1]
		{
			216
		};

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemBlockEntities = base.Project.FindSubsystem<SubsystemBlockEntities>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
		}

		public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
		{
			DatabaseObject databaseObject = base.Project.GameDatabase.Database.FindDatabaseObject("Dispenser", base.Project.GameDatabase.EntityTemplateType, throwIfNotFound: true);
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			valuesDictionary.PopulateFromDatabaseObject(databaseObject);
			valuesDictionary.GetValue<ValuesDictionary>("BlockEntity").SetValue("Coordinates", new Point3(x, y, z));
			Entity entity = base.Project.CreateEntity(valuesDictionary);
			base.Project.AddEntity(entity);
		}

		public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
		{
			ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(x, y, z);
			if (blockEntity != null)
			{
				Vector3 position = new Vector3(x, y, z) + new Vector3(0.5f);
				foreach (IInventory item in blockEntity.Entity.FindComponents<IInventory>())
				{
					item.DropAllItems(position);
				}
				base.Project.RemoveEntity(blockEntity.Entity, disposeEntity: true);
			}
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			if (m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Adventure)
			{
				ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z);
				if (blockEntity != null && componentMiner.ComponentPlayer != null)
				{
					ComponentDispenser componentDispenser = blockEntity.Entity.FindComponent<ComponentDispenser>(throwOnError: true);
					componentMiner.ComponentPlayer.ComponentGui.ModalPanelWidget = new DispenserWidget(componentMiner.Inventory, componentDispenser);
					AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
					return true;
				}
			}
			return false;
		}

		public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
		{
			if (worldItem.ToRemove)
			{
				return;
			}
			ComponentBlockEntity blockEntity = m_subsystemBlockEntities.GetBlockEntity(cellFace.X, cellFace.Y, cellFace.Z);
			if (blockEntity != null && DispenserBlock.GetAcceptsDrops(Terrain.ExtractData(m_subsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z))))
			{
				ComponentDispenser inventory = blockEntity.Entity.FindComponent<ComponentDispenser>(throwOnError: true);
				Pickable pickable = worldItem as Pickable;
				int num = pickable?.Count ?? 1;
				int num2 = ComponentInventoryBase.AcquireItems(inventory, worldItem.Value, num);
				if (num2 < num)
				{
					m_subsystemAudio.PlaySound("Audio/PickableCollected", 1f, 0f, worldItem.Position, 3f, autoDelay: true);
				}
				if (num2 <= 0)
				{
					worldItem.ToRemove = true;
				}
				else if (pickable != null)
				{
					pickable.Count = num2;
				}
			}
		}
	}
}
