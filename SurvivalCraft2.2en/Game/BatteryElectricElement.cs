using Engine;

namespace Game
{
	public class BatteryElectricElement : ElectricElement
	{
		public BatteryElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public override float GetOutputVoltage(int face)
		{
			Point3 point = base.CellFaces[0].Point;
			return (float)BatteryBlock.GetVoltageLevel(Terrain.ExtractData(base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(point.X, point.Y, point.Z))) / 15f;
		}

		public override void OnNeighborBlockChanged(CellFace cellFace, int neighborX, int neighborY, int neighborZ)
		{
			int cellValue = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y - 1, cellFace.Z);
			Block block = BlocksManager.Blocks[Terrain.ExtractContents(cellValue)];
			if (!block.IsCollidable || block.IsTransparent)
			{
				base.SubsystemElectricity.SubsystemTerrain.DestroyCell(0, cellFace.X, cellFace.Y, cellFace.Z, 0, noDrop: false, noParticleSystem: false);
			}
		}
	}
}
