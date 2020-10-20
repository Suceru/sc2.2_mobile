using System;

namespace Game
{
	[Flags]
	public enum CreatureCategory
	{
		LandPredator = 0x1,
		LandOther = 0x2,
		WaterPredator = 0x4,
		WaterOther = 0x8,
		Bird = 0x10
	}
}
