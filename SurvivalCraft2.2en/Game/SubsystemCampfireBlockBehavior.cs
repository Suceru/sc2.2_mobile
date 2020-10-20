using Engine;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemCampfireBlockBehavior : SubsystemBlockBehavior, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemParticles m_subsystemParticles;

		public SubsystemWeather m_subsystemWeather;

		public SubsystemAmbientSounds m_subsystemAmbientSounds;

		public Dictionary<Point3, FireParticleSystem> m_particleSystemsByCell = new Dictionary<Point3, FireParticleSystem>();

		public float m_fireSoundVolume;

		public Random m_random = new Random();

		public int m_updateIndex;

		public List<Point3> m_toReduce = new List<Point3>();

		public Dictionary<Point3, FireParticleSystem>.KeyCollection Campfires => m_particleSystemsByCell.Keys;

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public override int[] HandledBlocks => new int[0];

		public void Update(float dt)
		{
			if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
			{
				m_updateIndex++;
				foreach (Point3 key in m_particleSystemsByCell.Keys)
				{
					PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(key.X, key.Z);
					if ((precipitationShaftInfo.Intensity > 0f && key.Y >= precipitationShaftInfo.YLimit - 1) || m_updateIndex % 5 == 0)
					{
						m_toReduce.Add(key);
					}
				}
				foreach (Point3 item in m_toReduce)
				{
					ResizeCampfire(item.X, item.Y, item.Z, -1, playSound: true);
				}
				m_toReduce.Clear();
			}
			if (Time.PeriodicEvent(0.5, 0.0))
			{
				float num = 3.40282347E+38f;
				foreach (Point3 key2 in m_particleSystemsByCell.Keys)
				{
					float x = m_subsystemAmbientSounds.SubsystemAudio.CalculateListenerDistanceSquared(new Vector3(key2.X, key2.Y, key2.Z));
					num = MathUtils.Min(num, x);
				}
				m_fireSoundVolume = m_subsystemAmbientSounds.SubsystemAudio.CalculateVolume(MathUtils.Sqrt(num), 2f);
			}
			m_subsystemAmbientSounds.FireSoundVolume = MathUtils.Max(m_subsystemAmbientSounds.FireSoundVolume, m_fireSoundVolume);
		}

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			int cellContents = base.SubsystemTerrain.Terrain.GetCellContents(x, y - 1, z);
			if (BlocksManager.Blocks[cellContents].IsTransparent)
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}

		public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
		{
			AddCampfireParticleSystem(value, x, y, z);
		}

		public override void OnBlockRemoved(int value, int newValue, int x, int y, int z)
		{
			RemoveCampfireParticleSystem(x, y, z);
		}

		public override void OnBlockModified(int value, int oldValue, int x, int y, int z)
		{
			RemoveCampfireParticleSystem(x, y, z);
			AddCampfireParticleSystem(value, x, y, z);
		}

		public override void OnBlockGenerated(int value, int x, int y, int z, bool isLoaded)
		{
			AddCampfireParticleSystem(value, x, y, z);
		}

		public override void OnChunkDiscarding(TerrainChunk chunk)
		{
			List<Point3> list = new List<Point3>();
			foreach (Point3 key in m_particleSystemsByCell.Keys)
			{
				if (key.X >= chunk.Origin.X && key.X < chunk.Origin.X + 16 && key.Z >= chunk.Origin.Y && key.Z < chunk.Origin.Y + 16)
				{
					list.Add(key);
				}
			}
			foreach (Point3 item in list)
			{
				ResizeCampfire(item.X, item.Y, item.Z, -15, playSound: false);
				RemoveCampfireParticleSystem(item.X, item.Y, item.Z);
			}
		}

		public override void OnHitByProjectile(CellFace cellFace, WorldItem worldItem)
		{
			if (!worldItem.ToRemove && AddFuel(cellFace.X, cellFace.Y, cellFace.Z, worldItem.Value, (worldItem as Pickable)?.Count ?? 1))
			{
				worldItem.ToRemove = true;
			}
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			if (AddFuel(raycastResult.CellFace.X, raycastResult.CellFace.Y, raycastResult.CellFace.Z, componentMiner.ActiveBlockValue, 1))
			{
				componentMiner.RemoveActiveTool(1);
			}
			return true;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
			m_subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			m_subsystemAmbientSounds = base.Project.FindSubsystem<SubsystemAmbientSounds>(throwOnError: true);
		}

		public void AddCampfireParticleSystem(int value, int x, int y, int z)
		{
			int num = Terrain.ExtractData(value);
			if (num > 0)
			{
				Vector3 v = new Vector3(0.5f, 0.15f, 0.5f);
				float size = MathUtils.Lerp(0.2f, 0.5f, (float)num / 15f);
				FireParticleSystem fireParticleSystem = new FireParticleSystem(new Vector3(x, y, z) + v, size, 256f);
				m_subsystemParticles.AddParticleSystem(fireParticleSystem);
				m_particleSystemsByCell[new Point3(x, y, z)] = fireParticleSystem;
			}
		}

		public void RemoveCampfireParticleSystem(int x, int y, int z)
		{
			Point3 key = new Point3(x, y, z);
			if (m_particleSystemsByCell.TryGetValue(key, out FireParticleSystem value))
			{
				value.IsStopped = true;
				m_particleSystemsByCell.Remove(key);
			}
		}

		public bool AddFuel(int x, int y, int z, int value, int count)
		{
			if (Terrain.ExtractData(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z)) > 0)
			{
				int num = Terrain.ExtractContents(value);
				Block block = BlocksManager.Blocks[num];
				if (base.Project.FindSubsystem<SubsystemExplosions>(throwOnError: true).TryExplodeBlock(x, y, z, value))
				{
					return true;
				}
				if (block is SnowBlock || block is SnowballBlock || block is IceBlock)
				{
					return ResizeCampfire(x, y, z, -1, playSound: true);
				}
				if (block.FuelHeatLevel > 0f)
				{
					float num2 = (float)count * MathUtils.Min(block.FuelFireDuration, 20f) / 5f;
					int num3 = (int)num2;
					float num4 = num2 - (float)num3;
					if (m_random.Float(0f, 1f) < num4)
					{
						num3++;
					}
					if (num3 > 0)
					{
						return ResizeCampfire(x, y, z, num3, playSound: true);
					}
					return true;
				}
			}
			return false;
		}

		public bool ResizeCampfire(int x, int y, int z, int steps, bool playSound)
		{
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
			int num = Terrain.ExtractData(cellValue);
			if (num > 0)
			{
				int num2 = MathUtils.Clamp(num + steps, 0, 15);
				if (num2 != num)
				{
					int value = Terrain.ReplaceData(cellValue, num2);
					base.SubsystemTerrain.ChangeCell(x, y, z, value);
					if (playSound)
					{
						if (steps >= 0)
						{
							m_subsystemAmbientSounds.SubsystemAudio.PlaySound("Audio/BlockPlaced", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: false);
						}
						else
						{
							m_subsystemAmbientSounds.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, 0f, new Vector3(x, y, z), 3f, autoDelay: true);
						}
					}
					return true;
				}
			}
			return false;
		}
	}
}
