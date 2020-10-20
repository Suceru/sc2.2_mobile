using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class ComponentBlockEntity : Component
	{
		public Point3 Coordinates
		{
			get;
			set;
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			Coordinates = valuesDictionary.GetValue<Point3>("Coordinates");
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("Coordinates", Coordinates);
		}
	}
}
