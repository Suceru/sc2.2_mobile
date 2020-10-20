namespace Game
{
	public class SubsystemSnowBlockBehavior : SubsystemBlockBehavior
	{
		public override int[] HandledBlocks => new int[1]
		{
			61
		};

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			if (!CanSupportSnow(base.SubsystemTerrain.Terrain.GetCellValue(x, y - 1, z)))
			{
				base.SubsystemTerrain.DestroyCell(0, x, y, z, 0, noDrop: false, noParticleSystem: false);
			}
		}

		public static bool CanSupportSnow(int value)
		{
			int num = Terrain.ExtractContents(value);
			Block block = BlocksManager.Blocks[num];
			if (block.IsTransparent)
			{
				return block is LeavesBlock;
			}
			return true;
		}
	}
}
