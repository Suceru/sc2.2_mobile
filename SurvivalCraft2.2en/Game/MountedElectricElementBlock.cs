namespace Game
{
	public abstract class MountedElectricElementBlock : Block, IElectricElementBlock
	{
		public abstract int GetFace(int value);

		public abstract ElectricElement CreateElectricElement(SubsystemElectricity subsystemElectricity, int value, int x, int y, int z);

		public abstract ElectricConnectorType? GetConnectorType(SubsystemTerrain terrain, int value, int face, int connectorFace, int x, int y, int z);

		public virtual int GetConnectionMask(int value)
		{
			return 2147483647;
		}
	}
}
