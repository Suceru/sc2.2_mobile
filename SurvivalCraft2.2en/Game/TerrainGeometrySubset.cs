using Engine;

namespace Game
{
	public class TerrainGeometrySubset
	{
		public DynamicArray<TerrainVertex> Vertices = new DynamicArray<TerrainVertex>();

		public DynamicArray<ushort> Indices = new DynamicArray<ushort>();

		public TerrainGeometrySubset()
		{
		}

		public TerrainGeometrySubset(DynamicArray<TerrainVertex> vertices, DynamicArray<ushort> indices)
		{
			Vertices = vertices;
			Indices = indices;
		}
	}
}
