using Engine;

namespace Game
{
	public class SubsystemWaterBlockBehavior : SubsystemFluidBlockBehavior, IUpdateable
	{
		public Random m_random = new Random();

		public float m_soundVolume;

		public override int[] HandledBlocks => new int[1]
		{
			18
		};

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public SubsystemWaterBlockBehavior()
			: base(BlocksManager.FluidBlocks[18], generateSources: true)
		{
		}

		public void Update(float dt)
		{
			if (base.SubsystemTime.PeriodicGameTimeEvent(0.25, 0.0))
			{
				SpreadFluid();
			}
			if (base.SubsystemTime.PeriodicGameTimeEvent(1.0, 0.25))
			{
				float num = 3.40282347E+38f;
				foreach (Vector3 listenerPosition in base.SubsystemAudio.ListenerPositions)
				{
					float? num2 = CalculateDistanceToFluid(listenerPosition, 8, flowingFluidOnly: true);
					if (num2.HasValue && num2.Value < num)
					{
						num = num2.Value;
					}
				}
				m_soundVolume = 0.5f * base.SubsystemAudio.CalculateVolume(num, 2f, 3.5f);
			}
			base.SubsystemAmbientSounds.WaterSoundVolume = MathUtils.Max(base.SubsystemAmbientSounds.WaterSoundVolume, m_soundVolume);
		}

		protected override bool OnFluidInteract(int interactValue, int x, int y, int z, int fluidValue)
		{
			if (BlocksManager.Blocks[Terrain.ExtractContents(interactValue)] is MagmaBlock)
			{
				base.SubsystemAudio.PlayRandomSound("Audio/Sizzles", 1f, m_random.Float(-0.1f, 0.1f), new Vector3(x, y, z), 5f, autoDelay: true);
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
				Set(x, y, z, 3);
				return true;
			}
			return base.OnFluidInteract(interactValue, x, y, z, fluidValue);
		}

		public override void OnItemHarvested(int x, int y, int z, int blockValue, ref BlockDropValue dropValue, ref int newBlockValue)
		{
			if (y > 80 && SubsystemWeather.IsPlaceFrozen(base.SubsystemTerrain.Terrain.GetSeasonalTemperature(x, z), y))
			{
				dropValue.Value = Terrain.MakeBlockValue(62);
			}
			else
			{
				base.OnItemHarvested(x, y, z, blockValue, ref dropValue, ref newBlockValue);
			}
		}
	}
}
