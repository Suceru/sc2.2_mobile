namespace Game
{
	public class SmallIncendiaryKegBlock : GunpowderKegBlock
	{
		public const int Index = 234;

		public SmallIncendiaryKegBlock()
			: base("Models/SmallGunpowderKeg", isIncendiary: true)
		{
		}
	}
}
