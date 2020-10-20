using Engine;

namespace Game
{
	public struct TerrainRaycastResult
	{
		public Ray3 Ray;

		public int Value;

		public CellFace CellFace;

		public int CollisionBoxIndex;

		public float Distance;

		public Vector3 HitPoint(float offsetFromSurface = 0f)
		{
			return Ray.Position + Ray.Direction * Distance + CellFace.FaceToVector3(CellFace.Face) * offsetFromSurface;
		}
	}
}
