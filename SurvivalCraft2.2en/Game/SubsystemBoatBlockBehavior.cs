using Engine;
using GameEntitySystem;
using System;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemBoatBlockBehavior : SubsystemBlockBehavior
	{
		public SubsystemAudio m_subsystemAudio;

		public SubsystemBodies m_subsystemBodies;

		public Random m_random = new Random();

		public override int[] HandledBlocks => new int[1]
		{
			178
		};

		public override bool OnUse(Ray3 ray, ComponentMiner componentMiner)
		{
			_ = componentMiner.Inventory;
			if (Terrain.ExtractContents(componentMiner.ActiveBlockValue) == 178)
			{
				TerrainRaycastResult? terrainRaycastResult = componentMiner.Raycast<TerrainRaycastResult>(ray, RaycastMode.Digging);
				if (terrainRaycastResult.HasValue)
				{
					Vector3 position = terrainRaycastResult.Value.HitPoint();
					DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
					m_subsystemBodies.FindBodiesInArea(new Vector2(position.X, position.Z) - new Vector2(8f), new Vector2(position.X, position.Z) + new Vector2(8f), dynamicArray);
					if (dynamicArray.Count((ComponentBody b) => b.Entity.ValuesDictionary.DatabaseObject.Name == "Boat") < 6)
					{
						Entity entity = DatabaseManager.CreateEntity(base.Project, "Boat", throwIfNotFound: true);
						entity.FindComponent<ComponentFrame>(throwOnError: true).Position = position;
						entity.FindComponent<ComponentFrame>(throwOnError: true).Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, m_random.Float(0f, (float)Math.PI * 2f));
						entity.FindComponent<ComponentSpawn>(throwOnError: true).SpawnDuration = 0f;
						base.Project.AddEntity(entity);
						componentMiner.RemoveActiveTool(1);
						m_subsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, position, 3f, autoDelay: true);
					}
					else
					{
						componentMiner.ComponentPlayer?.ComponentGui.DisplaySmallMessage("Too many boats", Color.White, blinking: true, playNotificationSound: false);
					}
					return true;
				}
			}
			return false;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>(throwOnError: true);
		}
	}
}
