using Engine;

namespace Game
{
	public abstract class RotateableElectricElement : MountedElectricElement
	{
		public int Rotation
		{
			get
			{
				CellFace cellFace = base.CellFaces[0];
				return RotateableMountedElectricElementBlock.GetRotation(Terrain.ExtractData(base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z)));
			}
			set
			{
				CellFace cellFace = base.CellFaces[0];
				int cellValue = base.SubsystemElectricity.SubsystemTerrain.Terrain.GetCellValue(cellFace.X, cellFace.Y, cellFace.Z);
				int value2 = Terrain.ReplaceData(cellValue, RotateableMountedElectricElementBlock.SetRotation(Terrain.ExtractData(cellValue), value % 4));
				base.SubsystemElectricity.SubsystemTerrain.ChangeCell(cellFace.X, cellFace.Y, cellFace.Z, value2);
				base.SubsystemElectricity.SubsystemAudio.PlaySound("Audio/Click", 1f, 0f, new Vector3(cellFace.X, cellFace.Y, cellFace.Z), 2f, autoDelay: true);
			}
		}

		protected RotateableElectricElement(SubsystemElectricity subsystemElectricity, CellFace cellFace)
			: base(subsystemElectricity, cellFace)
		{
		}

		public override bool OnInteract(TerrainRaycastResult raycastResult, ComponentMiner componentMiner)
		{
			int num = ++Rotation;
			return true;
		}
	}
}
