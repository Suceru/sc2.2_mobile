using Engine;

namespace Game
{
	public abstract class MountedElectricElement : ElectricElement
	{
		protected MountedElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public override void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ)
		{
			Point3 point = CellFace.FaceToPoint3(cellFace.Face);
			int x = cellFace.X - point.X;
			int y = cellFace.Y - point.Y;
			int z = cellFace.Z - point.Z;
			if (base.SubsystemElectricity.SubsystemTerrain.Terrain.IsCellValid(x, y, z))
			{
				int cellValue = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(x, y, z);
				Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
				if ((!block.IsCollidable || block.IsFaceTransparent(base.SubsystemElectricity.SubsystemTerrain, cellFace.Face, cellValue)) && (cellFace.Face != 4 || !(block is FenceBlock)))
				{
					base.SubsystemElectricity.SubsystemTerrain.DestroyCell(0, cellFace.X, cellFace.Y, cellFace.Z, 0, noDrop: false, noParticleSystem: false);
				}
			}
		}
	}
}
