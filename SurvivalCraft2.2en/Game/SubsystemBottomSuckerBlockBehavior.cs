using Engine;

namespace Game
{
	public class SubsystemBottomSuckerBlockBehavior : SubsystemInWaterBlockBehavior
	{
		public override int[] HandledBlocks => new int[0];

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			base.OnNeighborBlockChanged(x, y, z, neighborX, neighborY, neighborZ);
			int face = BottomSuckerBlock.GetFace(Terrain.ExtractData(base.SubsystemTerrain.Terrain.GetCellValue(x, y, z)));
			Point3 point = CellFace.FaceToPoint3(CellFace.OppositeFace(face));
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(x + point.X, y + point.Y, z + point.Z);
			if (!IsSupport(cellValue, face))
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}

		public override void OnCollide(CellFace cellFace, float velocity, ComponentBody componentBody)
		{
			if (Terrain.ExtractContents(base.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)) == 226)
			{
				componentBody.Entity.FindComponent<ComponentCreature>()?.ComponentHealth.Injure(0.01f * MathUtils.Abs(velocity), null, ignoreInvulnerability: false, "Spiked by a sea creature");
			}
		}

		public bool IsSupport(int value, int face)
		{
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(value)];
			if (block.IsCollidable)
			{
				return !block.IsFaceTransparent(base.SubsystemTerrain, CellFace.OppositeFace(face), value);
			}
			return false;
		}
	}
}
