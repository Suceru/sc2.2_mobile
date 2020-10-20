using Engine;
using Engine.Graphics;
using System;

namespace Game
{
	public class TerrainChunkGeometry : IDisposable
	{
		public class Buffer : IDisposable
		{
			public VertexBuffer VertexBuffer;

			public IndexBuffer IndexBuffer;

			public int[] SubsetIndexBufferStarts = new int[7];

			public int[] SubsetIndexBufferEnds = new int[7];

			public void Dispose()
			{
				Utilities.Dispose(ref VertexBuffer);
				Utilities.Dispose(ref IndexBuffer);
			}
		}

		public const int SubsetsCount = 7;

		public TerrainChunkSliceGeometry[] Slices = new TerrainChunkSliceGeometry[16];

		public DynamicArray<Buffer> Buffers = new DynamicArray<Buffer>();

		public TerrainChunkGeometry()
		{
			for (int i = 0; i < Slices.Length; i++)
			{
				Slices[i] = new TerrainChunkSliceGeometry();
			}
		}

		public void Dispose()
		{
			foreach (Buffer buffer in Buffers)
			{
				buffer.Dispose();
			}
		}

		public void InvalidateSliceContentsHashes()
		{
			for (int i = 0; i < Slices.Length; i++)
			{
				Slices[i].ContentsHash = 0;
			}
		}

		public void CopySliceContentsHashes(TerrainChunk chunk)
		{
			for (int i = 0; i < Slices.Length; i++)
			{
				Slices[i].ContentsHash = chunk.SliceContentsHashes[i];
			}
		}
	}
}
