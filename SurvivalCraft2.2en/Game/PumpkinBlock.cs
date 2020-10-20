using System.Collections.Generic;

namespace Game
{
	public class PumpkinBlock : BasePumpkinBlock
	{
		public const int Index = 131;

		public PumpkinBlock()
			: base(isRotten: false)
		{
		}

		public override void GetDropValues(SubsystemTerrain subsystemTerrain, int oldValue, int newValue, int toolLevel, List<BlockDropValue> dropValues, out bool showDebris)
		{
			base.GetDropValues(subsystemTerrain, oldValue, newValue, toolLevel, dropValues, out showDebris);
			int data = Terrain.ExtractData(oldValue);
			if (BasePumpkinBlock.GetSize(data) == 7 && !BasePumpkinBlock.GetIsDead(data) && Random.Bool(0.5f))
			{
				dropValues.Add(new BlockDropValue
				{
					Value = 248,
					Count = 1
				});
			}
		}
	}
}
