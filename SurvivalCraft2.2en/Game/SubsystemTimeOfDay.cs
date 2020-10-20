using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemTimeOfDay : Subsystem
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public bool TimeOfDayEnabled = true;

		public const float DayDuration = 1200f;

		public float TimeOfDay
		{
			get
			{
				if (TimeOfDayEnabled)
				{
					if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Changing)
					{
						return CalculateTimeOfDay(m_subsystemGameInfo.TotalElapsedGameTime);
					}
					if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Day)
					{
						return MathUtils.Remainder(0.5f, 1f);
					}
					if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Night)
					{
						return MathUtils.Remainder(1f, 1f);
					}
					if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunrise)
					{
						return MathUtils.Remainder(0.25f, 1f);
					}
					if (m_subsystemGameInfo.WorldSettings.TimeOfDayMode == TimeOfDayMode.Sunset)
					{
						return MathUtils.Remainder(0.75f, 1f);
					}
					return 0.5f;
				}
				return 0.5f;
			}
		}

		public double Day => CalculateDay(m_subsystemGameInfo.TotalElapsedGameTime);

		public double TimeOfDayOffset
		{
			get;
			set;
		}

		public double CalculateDay(double totalElapsedGameTime)
		{
			return (totalElapsedGameTime + (TimeOfDayOffset + 0.30000001192092896) * 1200.0) / 1200.0;
		}

		public float CalculateTimeOfDay(double totalElapsedGameTime)
		{
			return (float)MathUtils.Remainder(totalElapsedGameTime + (TimeOfDayOffset + 0.30000001192092896) * 1200.0, 1200.0) / 1200f;
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			TimeOfDayOffset = valuesDictionary.GetValue<double>("TimeOfDayOffset");
		}

		public override void Save(ValuesDictionary valuesDictionary)
		{
			valuesDictionary.SetValue("TimeOfDayOffset", TimeOfDayOffset);
		}
	}
}
