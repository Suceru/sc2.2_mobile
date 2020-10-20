using System;
using System.Collections.Generic;

namespace Game
{
	public class WorldInfo
	{
		public string DirectoryName = string.Empty;

		public long Size;

		public DateTime LastSaveTime;

		public string SerializationVersion = string.Empty;

		public WorldSettings WorldSettings = new WorldSettings();

		public List<PlayerInfo> PlayerInfos = new List<PlayerInfo>();
	}
}
