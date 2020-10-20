using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Game
{
	public class Terrain : IDisposable
	{
		public class ChunksStorage
		{
			public const int Shift = 8;

			public const int Capacity = 65536;

			public const int CapacityMinusOne = 65535;

			public TerrainChunk[] m_array = new TerrainChunk[65536];

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public TerrainChunk Get(int x, int y)
			{
				int num = (x + (y << 8)) & 0xFFFF;
				TerrainChunk terrainChunk;
				while (true)
				{
					terrainChunk = m_array[num];
					if (terrainChunk == null)
					{
						return null;
					}
					if (terrainChunk.Coords.X == x && terrainChunk.Coords.Y == y)
					{
						break;
					}
					num = ((num + 1) & 0xFFFF);
				}
				return terrainChunk;
			}

			public void Add(int x, int y, TerrainChunk chunk)
			{
				int num = (x + (y << 8)) & 0xFFFF;
				while (m_array[num] != null)
				{
					num = ((num + 1) & 0xFFFF);
				}
				m_array[num] = chunk;
			}

			public void Remove(int x, int y)
			{
				int num = (x + (y << 8)) & 0xFFFF;
				while (true)
				{
					TerrainChunk terrainChunk = m_array[num];
					if (terrainChunk == null)
					{
						return;
					}
					if (terrainChunk.Coords.X == x && terrainChunk.Coords.Y == y)
					{
						break;
					}
					num = ((num + 1) & 0xFFFF);
				}
				m_array[num] = null;
			}
		}

		public const int ContentsMask = 1023;

		public const int LightMask = 15360;

		public const int LightShift = 10;

		public const int DataMask = -16384;

		public const int DataShift = 14;

		public const int TopHeightMask = 255;

		public const int TopHeightShift = 0;

		public const int TemperatureMask = 3840;

		public const int TemperatureShift = 8;

		public const int HumidityMask = 61440;

		public const int HumidityShift = 12;

		public const int BottomHeightMask = 16711680;

		public const int BottomHeightShift = 16;

		public const int SunlightHeightMask = -16777216;

		public const int SunlightHeightShift = 24;

		public ChunksStorage m_allChunks;

		public HashSet<TerrainChunk> m_allocatedChunks;

		public TerrainChunk[] m_allocatedChunksArray;

		public int SeasonTemperature;

		public int SeasonHumidity;

		public TerrainChunk[] AllocatedChunks
		{
			get
			{
				if (m_allocatedChunksArray == null)
				{
					m_allocatedChunksArray = m_allocatedChunks.ToArray();
				}
				return m_allocatedChunksArray;
			}
		}

		public Terrain()
		{
			m_allChunks = new ChunksStorage();
			m_allocatedChunks = new HashSet<TerrainChunk>();
		}

		public void Dispose()
		{
			foreach (TerrainChunk allocatedChunk in m_allocatedChunks)
			{
				allocatedChunk.Dispose();
			}
		}

		public TerrainChunk GetNextChunk(int chunkX, int chunkZ)
		{
			TerrainChunk terrainChunk = GetChunkAtCoords(chunkX, chunkZ);
			if (terrainChunk != null)
			{
				return terrainChunk;
			}
			TerrainChunk[] allocatedChunks = AllocatedChunks;
			for (int i = 0; i < allocatedChunks.Length; i++)
			{
				if (ComparePoints(allocatedChunks[i].Coords, new Point2(chunkX, chunkZ)) >= 0 && (terrainChunk == null || ComparePoints(allocatedChunks[i].Coords, terrainChunk.Coords) < 0))
				{
					terrainChunk = allocatedChunks[i];
				}
			}
			if (terrainChunk == null)
			{
				for (int j = 0; j < allocatedChunks.Length; j++)
				{
					if (terrainChunk == null || ComparePoints(allocatedChunks[j].Coords, terrainChunk.Coords) < 0)
					{
						terrainChunk = allocatedChunks[j];
					}
				}
			}
			return terrainChunk;
		}

		public TerrainChunk GetChunkAtCoords(int chunkX, int chunkZ)
		{
			return m_allChunks.Get(chunkX, chunkZ);
		}

		public TerrainChunk GetChunkAtCell(int x, int z)
		{
			return GetChunkAtCoords(x >> 4, z >> 4);
		}

		public TerrainChunk AllocateChunk(int chunkX, int chunkZ)
		{
			if (GetChunkAtCoords(chunkX, chunkZ) != null)
			{
				throw new InvalidOperationException("Chunk already allocated.");
			}
			TerrainChunk terrainChunk = new TerrainChunk(this, chunkX, chunkZ);
			m_allocatedChunks.Add(terrainChunk);
			m_allChunks.Add(chunkX, chunkZ, terrainChunk);
			m_allocatedChunksArray = null;
			return terrainChunk;
		}

		public void FreeChunk(TerrainChunk chunk)
		{
			if (!m_allocatedChunks.Remove(chunk))
			{
				throw new InvalidOperationException("Chunk not allocated.");
			}
			m_allChunks.Remove(chunk.Coords.X, chunk.Coords.Y);
			m_allocatedChunksArray = null;
		}

		public static int ComparePoints(Point2 c1, Point2 c2)
		{
			if (c1.Y == c2.Y)
			{
				return c1.X - c2.X;
			}
			return c1.Y - c2.Y;
		}

		public static Point2 ToChunk(Vector2 p)
		{
			return ToChunk(ToCell(p.X), ToCell(p.Y));
		}

		public static Point2 ToChunk(int x, int z)
		{
			return new Point2(x >> 4, z >> 4);
		}

		public static int ToCell(float x)
		{
			return (int)MathUtils.Floor(x);
		}

		public static Point2 ToCell(float x, float y)
		{
			return new Point2((int)MathUtils.Floor(x), (int)MathUtils.Floor(y));
		}

		public static Point2 ToCell(Vector2 p)
		{
			return new Point2((int)MathUtils.Floor(p.X), (int)MathUtils.Floor(p.Y));
		}

		public static Point3 ToCell(float x, float y, float z)
		{
			return new Point3((int)MathUtils.Floor(x), (int)MathUtils.Floor(y), (int)MathUtils.Floor(z));
		}

		public static Point3 ToCell(Vector3 p)
		{
			return new Point3((int)MathUtils.Floor(p.X), (int)MathUtils.Floor(p.Y), (int)MathUtils.Floor(p.Z));
		}

		public bool IsCellValid(int x, int y, int z)
		{
			if (y >= 0)
			{
				return y < 256;
			}
			return false;
		}

		public int GetCellValue(int x, int y, int z)
		{
			if (!IsCellValid(x, y, z))
			{
				return 0;
			}
			return GetCellValueFast(x, y, z);
		}

		public int GetCellContents(int x, int y, int z)
		{
			if (!IsCellValid(x, y, z))
			{
				return 0;
			}
			return GetCellContentsFast(x, y, z);
		}

		public int GetCellLight(int x, int y, int z)
		{
			if (!IsCellValid(x, y, z))
			{
				return 0;
			}
			return GetCellLightFast(x, y, z);
		}

		public int GetCellValueFast(int x, int y, int z)
		{
			return GetChunkAtCell(x, z)?.GetCellValueFast(x & 0xF, y, z & 0xF) ?? 0;
		}

		public int GetCellValueFastChunkExists(int x, int y, int z)
		{
			return GetChunkAtCell(x, z).GetCellValueFast(x & 0xF, y, z & 0xF);
		}

		public int GetCellContentsFast(int x, int y, int z)
		{
			return ExtractContents(GetCellValueFast(x, y, z));
		}

		public int GetCellLightFast(int x, int y, int z)
		{
			return ExtractLight(GetCellValueFast(x, y, z));
		}

		public void SetCellValueFast(int x, int y, int z, int value)
		{
			GetChunkAtCell(x, z)?.SetCellValueFast(x & 0xF, y, z & 0xF, value);
		}

		public int CalculateTopmostCellHeight(int x, int z)
		{
			return GetChunkAtCell(x, z)?.CalculateTopmostCellHeight(x & 0xF, z & 0xF) ?? 0;
		}

		public int GetShaftValue(int x, int z)
		{
			return GetChunkAtCell(x, z)?.GetShaftValueFast(x & 0xF, z & 0xF) ?? 0;
		}

		public void SetShaftValue(int x, int z, int value)
		{
			GetChunkAtCell(x, z)?.SetShaftValueFast(x & 0xF, z & 0xF, value);
		}

		public int GetTemperature(int x, int z)
		{
			return ExtractTemperature(GetShaftValue(x, z));
		}

		public void SetTemperature(int x, int z, int temperature)
		{
			SetShaftValue(x, z, ReplaceTemperature(GetShaftValue(x, z), temperature));
		}

		public int GetHumidity(int x, int z)
		{
			return ExtractHumidity(GetShaftValue(x, z));
		}

		public void SetHumidity(int x, int z, int humidity)
		{
			SetShaftValue(x, z, ReplaceHumidity(GetShaftValue(x, z), humidity));
		}

		public int GetTopHeight(int x, int z)
		{
			return ExtractTopHeight(GetShaftValue(x, z));
		}

		public void SetTopHeight(int x, int z, int topHeight)
		{
			SetShaftValue(x, z, ReplaceTopHeight(GetShaftValue(x, z), topHeight));
		}

		public int GetBottomHeight(int x, int z)
		{
			return ExtractBottomHeight(GetShaftValue(x, z));
		}

		public void SetBottomHeight(int x, int z, int bottomHeight)
		{
			SetShaftValue(x, z, ReplaceBottomHeight(GetShaftValue(x, z), bottomHeight));
		}

		public int GetSunlightHeight(int x, int z)
		{
			return ExtractSunlightHeight(GetShaftValue(x, z));
		}

		public void SetSunlightHeight(int x, int z, int sunlightHeight)
		{
			SetShaftValue(x, z, ReplaceSunlightHeight(GetShaftValue(x, z), sunlightHeight));
		}

		public static int MakeBlockValue(int contents)
		{
			return contents & 0x3FF;
		}

		public static int MakeBlockValue(int contents, int light, int data)
		{
			return (contents & 0x3FF) | ((light << 10) & 0x3C00) | ((data << 14) & -16384);
		}

		public static int ExtractContents(int value)
		{
			return value & 0x3FF;
		}

		public static int ExtractLight(int value)
		{
			return (value & 0x3C00) >> 10;
		}

		public static int ExtractData(int value)
		{
			return (value & -16384) >> 14;
		}

		public static int ExtractTopHeight(int value)
		{
			return value & 0xFF;
		}

		public static int ExtractBottomHeight(int value)
		{
			return (value & 0xFF0000) >> 16;
		}

		public static int ExtractSunlightHeight(int value)
		{
			return (value & -16777216) >> 24;
		}

		public static int ExtractHumidity(int value)
		{
			return (value & 0xF000) >> 12;
		}

		public static int ExtractTemperature(int value)
		{
			return (value & 0xF00) >> 8;
		}

		public static int ReplaceContents(int value, int contents)
		{
			return value ^ ((value ^ contents) & 0x3FF);
		}

		public static int ReplaceLight(int value, int light)
		{
			return value ^ ((value ^ (light << 10)) & 0x3C00);
		}

		public static int ReplaceData(int value, int data)
		{
			return value ^ ((value ^ (data << 14)) & -16384);
		}

		public static int ReplaceTopHeight(int value, int topHeight)
		{
			return value ^ ((value ^ topHeight) & 0xFF);
		}

		public static int ReplaceBottomHeight(int value, int bottomHeight)
		{
			return value ^ ((value ^ (bottomHeight << 16)) & 0xFF0000);
		}

		public static int ReplaceSunlightHeight(int value, int sunlightHeight)
		{
			return value ^ ((value ^ (sunlightHeight << 24)) & -16777216);
		}

		public static int ReplaceHumidity(int value, int humidity)
		{
			return value ^ ((value ^ (humidity << 12)) & 0xF000);
		}

		public static int ReplaceTemperature(int value, int temperature)
		{
			return value ^ ((value ^ (temperature << 8)) & 0xF00);
		}

		public int GetSeasonalTemperature(int x, int z)
		{
			return GetTemperature(x, z) + SeasonTemperature;
		}

		public int GetSeasonalTemperature(int shaftValue)
		{
			return ExtractTemperature(shaftValue) + SeasonTemperature;
		}

		public int GetSeasonalHumidity(int x, int z)
		{
			return GetHumidity(x, z) + SeasonHumidity;
		}

		public int GetSeasonalHumidity(int shaftValue)
		{
			return ExtractHumidity(shaftValue) + SeasonHumidity;
		}
	}
}
