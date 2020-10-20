namespace Game
{
	public class LargeIncendiaryKegBlock : GunpowderKegBlock
	{
		public const int Index = 236;

		public LargeIncendiaryKegBlock()
			: base("Models/LargeGunpowderKeg", isIncendiary: true)
		{
		}
	}
}
