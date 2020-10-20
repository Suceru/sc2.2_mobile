namespace Game
{
	public class MediumIncendiaryKegBlock : GunpowderKegBlock
	{
		public const int Index = 235;

		public MediumIncendiaryKegBlock()
			: base("Models/MediumGunpowderKeg", isIncendiary: true)
		{
		}
	}
}
