using Engine;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemMagmaBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable
	{
		public Random m_random = new Random();

		public SubsystemFireBlockBehavior m_subsystemFireBlockBehavior;

		public SubsystemParticles m_subsystemParticles;

		public float m_soundVolume;

		public override int[] HandledBlocks => new int[1]
		{
			92
		};

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public SubsystemMagmaBlockBehavior()
			: base(BlocksManager.FluidBlocks[92], generateSources: false)
		{
		}

		public void Update(float dt)
		{
			if (base.SubsystemTime.PeriodicGameTimeEvent(2.0, 0.0))
			{
				SpreadFluid();
			}
			if (base.SubsystemTime.PeriodicGameTimeEvent(1.0, 0.75))
			{
				float num = 3.40282347E+38f;
				foreach (Vector3 listenerPosition in base.SubsystemAudio.ListenerPositions)
				{
					float? num2 = CalculateDistanceToFluid(listenerPosition, 8, flowingFluidOnly: false);
					if (num2.HasValue && num2.Value < num)
					{
						num = num2.Value;
					}
				}
				m_soundVolume = base.SubsystemAudio.CalculateVolume(num, 2f, 3.5f);
			}
			base.SubsystemAmbientSounds.MagmaSoundVolume = MathUtils.Max(base.SubsystemAmbientSounds.MagmaSoundVolume, m_soundVolume);
		}

		public override void OnBlockAdded(int value, int oldValue, int x, int y, int z)
		{
			base.OnBlockAdded(value, oldValue, x, y, z);
			for (int i = -1; i <= 1; i++)
			{
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						ApplyMagmaNeighborhoodEffect(x + i, y + j, z + k);
					}
				}
			}
		}

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			base.OnNeighborBlockChanged(x, y, z, neighborX, neighborY, neighborZ);
			ApplyMagmaNeighborhoodEffect(neighborX, neighborY, neighborZ);
		}

		protected override bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue)
		{
			if (BlocksManager.Blocks[Terrain.ExtractContents(interactValue)] is WaterBlock)
			{
				base.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 5f, autoDelay: true);
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				Set(x, y, z, 67);
				return true;
			}
			return base.OnFluidInteract(interactValue, x, y, z, fluidValue);
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemFireBlockBehavior = base.Project.FindSubsystem<SubsystemFireBlockBehavior>(throwOnError: true);
			m_subsystemParticles = base.Project.FindSubsystem<SubsystemParticles>(throwOnError: true);
		}

		public void ApplyMagmaNeighborhoodEffect(int x, int y, int z)
		{
			m_subsystemFireBlockBehavior.SetCellOnFire(x, y, z, 1f);
			switch (base.SubsystemTerrain.Terrain.GetCellContents(x, y, z))
			{
			case 61:
			case 62:
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(base.SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
				break;
			case 8:
				base.SubsystemTerrain.ChangeCell(x, y, z, 2);
				m_subsystemParticles.AddParticleSystem(new BurntDebrisParticleSystem(base.SubsystemTerrain, new Vector3((float)x + 0.5f, y + 1, (float)z + 0.5f)));
				break;
			}
		}
	}
}
