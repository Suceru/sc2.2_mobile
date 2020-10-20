using Engine;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemBucketBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemAudio m_subsystemAudio;

		public SubsystemParticles m_subsystemParticles;

		public Random m_random = new Random();

		public override int[] HandledBlocks => new int[9]
		{
			90,
			91,
			93,
			110,
			245,
			251,
			252,
			129,
			128
		};

		public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
		{
			IInventory inventory = componentMiner.Inventory;
			int activeBlockValue = componentMiner.ActiveBlockValue;
			int num = Terrain.ExtractContents(activeBlockValue);
			if (num == 90)
			{
				object obj = componentMiner.Raycast(ray, RaycastMode.Gathering);
				if (obj is TerrainRaycastResult)
				{
					CellFace cellFace = ((TerrainRaycastResult)obj).CellFace;
					int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
					int num2 = Terrain.ExtractContents(cellValue);
					int data = Terrain.ExtractData(cellValue);
					Block block = BlocksManager.Blocks[num2];
					if (block is WaterBlock && FluidBlock.GetLevel(data) == 0)
					{
						int value = Terrain.ReplaceContents(activeBlockValue, 91);
						inventory.RemoveSlotItems(inventory.ActiveSlotIndex, inventory.GetSlotCount(inventory.ActiveSlotIndex));
						if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0)
						{
							inventory.AddSlotItems(inventory.ActiveSlotIndex, value, 1);
						}
						base.SubsystemTerrain.DestroyCell(0, cellFace.X, cellFace.Y, cellFace.Z, 0, noDrop: false, noParticleSystem: false);
						return true;
					}
					if (block is MagmaBlock && FluidBlock.GetLevel(data) == 0)
					{
						int value2 = Terrain.ReplaceContents(activeBlockValue, 93);
						inventory.RemoveSlotItems(inventory.ActiveSlotIndex, inventory.GetSlotCount(inventory.ActiveSlotIndex));
						if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0)
						{
							inventory.AddSlotItems(inventory.ActiveSlotIndex, value2, 1);
						}
						base.SubsystemTerrain.DestroyCell(0, cellFace.X, cellFace.Y, cellFace.Z, 0, noDrop: false, noParticleSystem: false);
						return true;
					}
				}
				else if (obj is BodyRaycastResult)
				{
					ComponentUdder componentUdder = ((BodyRaycastResult)obj).ComponentBody.Entity.FindComponent<ComponentUdder>();
					if (componentUdder != null && componentUdder.Milk(componentMiner))
					{
						int value3 = Terrain.ReplaceContents(activeBlockValue, 110);
						inventory.RemoveSlotItems(inventory.ActiveSlotIndex, inventory.GetSlotCount(inventory.ActiveSlotIndex));
						if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0)
						{
							inventory.AddSlotItems(inventory.ActiveSlotIndex, value3, 1);
						}
						m_subsystemAudio.PlaySound("Audio/Milked", 1f, 0f, ray.Position, 2f, autoDelay: true);
					}
					return true;
				}
			}
			if (num == 91)
			{
				TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
				if (terrainRaycastResult.HasValue && componentMiner.Place(terrainRaycastResult.Value, Terrain.MakeBlockValue(18)))
				{
					inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
					if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0)
					{
						int value4 = Terrain.ReplaceContents(activeBlockValue, 90);
						inventory.AddSlotItems(inventory.ActiveSlotIndex, value4, 1);
					}
					return true;
				}
			}
			if (num == 93)
			{
				TerrainRaycastResult? terrainRaycastResult2 = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Interaction);
				if (terrainRaycastResult2.HasValue)
				{
					if (componentMiner.Place(terrainRaycastResult2.Value, Terrain.MakeBlockValue(92)))
					{
						inventory.RemoveSlotItems(inventory.ActiveSlotIndex, 1);
						if (inventory.GetSlotCount(inventory.ActiveSlotIndex) == 0)
						{
							int value5 = Terrain.ReplaceContents(activeBlockValue, 90);
							inventory.AddSlotItems(inventory.ActiveSlotIndex, value5, 1);
						}
					}
					return true;
				}
			}
			switch (num)
			{
			case 110:
			case 245:
				return true;
			case 251:
			case 252:
				return true;
			case 128:
			case 129:
			{
				TerrainRaycastResult? terrainRaycastResult3 = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
				if (terrainRaycastResult3.HasValue)
				{
					CellFace cellFace2 = terrainRaycastResult3.Value.CellFace;
					int cellValue2 = base.SubsystemTerrain.Terrain.GetCellValue(cellFace2.X, cellFace2.Y, cellFace2.Z);
					int num3 = Terrain.ExtractContents(cellValue2);
					Block block2 = BlocksManager.Blocks[num3];
					if (block2 is IPaintableBlock)
					{
						Vector3 normal = CellFace.FaceToVector3(terrainRaycastResult3.Value.CellFace.Face);
						Vector3 position = terrainRaycastResult3.Value.HitPoint();
						int? num4 = (num == 128) ? null : new int?(PaintBucketBlock.GetColor(Terrain.ExtractData(activeBlockValue)));
						Color color = num4.HasValue ? SubsystemPalette.GetColor(base.SubsystemTerrain, num4) : new Color(128, 128, 128, 128);
						int value6 = ((IPaintableBlock)block2).Paint(base.SubsystemTerrain, cellValue2, num4);
						base.SubsystemTerrain.ChangeCell(cellFace2.X, cellFace2.Y, cellFace2.Z, value6);
						componentMiner.DamageActiveTool(1);
						m_subsystemAudio.PlayRandomSound("Audio/Paint", 0.4f, m_random.Float(-0.1f, 0.1f), componentMiner.ComponentCreature.ComponentBody.Position, 2f, autoDelay: true);
						m_subsystemParticles.AddParticleSystem(new PaintParticleSystem(base.SubsystemTerrain, position, normal, color));
					}
					return true;
				}
				break;
			}
			}
			return false;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		}
	}
}
