using Engine;

namespace Game
{
	public class BlockDebrisParticleSystem : ParticleSystem<BlockDebrisParticleSystem.Particle>
	{
		public class Particle : Game.Particle
		{
			public Vector3 Velocity;

			public float TimeToLive;
		}

		public Random m_random = new Random();

		public SubsystemTerrain m_subsystemTerrain;

		public BlockDebrisParticleSystem(SubsystemTerrain terrain, Vector3 position, float strength, float scale, Color color, int textureSlot)
			: base((int)(50f * strength))
		{
			m_subsystemTerrain = terrain;
			base.Texture = terrain.Project.FindSubsystem<SubsystemBlocksTexture>(throwOnError: true).BlocksTexture;
			int num = Terrain.ToCell(position.X);
			int num2 = Terrain.ToCell(position.Y);
			int num3 = Terrain.ToCell(position.Z);
			int x = 0;
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num + 1, num2, num3));
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num - 1, num2, num3));
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 + 1, num3));
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2 - 1, num3));
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 + 1));
			x = MathUtils.Max(x, terrain.Terrain.GetCellLight(num, num2, num3 - 1));
			base.TextureSlotsCount = 32;
			float num4 = LightingManager.LightIntensityByLightValue[x];
			color *= num4;
			color.A = 255;
			float num5 = MathUtils.Sqrt(strength);
			for (int i = 0; i < base.Particles.Length; i++)
			{
				Particle obj = base.Particles[i];
				obj.IsActive = true;
				Vector3 vector = new Vector3(m_random.Float(-1f, 1f), m_random.Float(-1f, 1f), m_random.Float(-1f, 1f));
				obj.Position = position + strength * 0.45f * vector;
				obj.Color = Color.MultiplyColorOnly(color, m_random.Float(0.7f, 1f));
				obj.Size = num5 * scale * new Vector2(m_random.Float(0.05f, 0.06f));
				obj.TimeToLive = num5 * m_random.Float(1f, 3f);
				obj.Velocity = num5 * 2f * (vector + new Vector3(m_random.Float(-0.2f, 0.2f), 0.6f, m_random.Float(-0.2f, 0.2f)));
				obj.TextureSlot = textureSlot % 16 * 2 + m_random.Int(0, 1) + 32 * (textureSlot / 16 * 2 + m_random.Int(0, 1));
			}
		}

		public override bool Simulate(float dt)
		{
			dt = MathUtils.Clamp(dt, 0f, 0.1f);
			float num = MathUtils.Pow(0.1f, dt);
			bool flag = false;
			for (int i = 0; i < base.Particles.Length; i++)
			{
				Particle particle = base.Particles[i];
				if (!particle.IsActive)
				{
					continue;
				}
				flag = true;
				particle.TimeToLive -= dt;
				if (particle.TimeToLive > 0f)
				{
					Vector3 position = particle.Position;
					Vector3 vector = position + particle.Velocity * dt;
					TerrainRaycastResult? terrainRaycastResult = m_subsystemTerrain.Raycast(position, vector, useInteractionBoxes: false, skipAirBlocks: true, (int value, float distance) => BlocksManager.Blocks[Terrain.ExtractContents(value)].IsCollidable);
					if (terrainRaycastResult.HasValue)
					{
						Plane plane = terrainRaycastResult.Value.CellFace.CalculatePlane();
						vector = position;
						if (plane.Normal.X != 0f)
						{
							particle.Velocity *= new Vector3(-0.25f, 0.25f, 0.25f);
						}
						if (plane.Normal.Y != 0f)
						{
							particle.Velocity *= new Vector3(0.25f, -0.25f, 0.25f);
						}
						if (plane.Normal.Z != 0f)
						{
							particle.Velocity *= new Vector3(0.25f, 0.25f, -0.25f);
						}
					}
					particle.Position = vector;
					particle.Velocity.Y += -9.81f * dt;
					particle.Velocity *= num;
					particle.Color *= MathUtils.Saturate(particle.TimeToLive);
				}
				else
				{
					particle.IsActive = false;
				}
			}
			return !flag;
		}
	}
}
