using Engine;

namespace Game
{
	public class DrawBlockEnvironmentData
	{
		public SubsystemTerrain SubsystemTerrain;

		public Matrix InWorldMatrix;

		public Matrix? ViewProjectionMatrix;

		public Vector3? BillboardDirection;

		public int Humidity;

		public int Temperature;

		public int Light;

		public DrawBlockEnvironmentData()
		{
			InWorldMatrix = Matrix.Identity;
			Humidity = 15;
			Temperature = 8;
			Light = 15;
		}
	}
}
