using Engine;
using Engine.Audio;
using GameEntitySystem;
using System.Collections.Generic;
using System.Globalization;
using TemplatesDatabase;

namespace Game
{
	public class ComponentVitalStats : Component, IUpdateable
	{
		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemTime m_subsystemTime;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemMetersBlockBehavior m_subsystemMetersBlockBehavior;

		public SubsystemWeather m_subsystemWeather;

		public ComponentPlayer m_componentPlayer;

		public Random m_random = new Random();

		public Sound m_pantingSound;

		public float m_food;

		public float m_stamina;

		public float m_sleep;

		public float m_temperature;

		public float m_wetness;

		public float m_lastFood;

		public float m_lastStamina;

		public float m_lastSleep;

		public float m_lastTemperature;

		public float m_lastWetness;

		public Dictionary<int, float> m_satiation = new Dictionary<int, float>();

		public List<KeyValuePair<int, float>> m_satiationList = new List<KeyValuePair<int, float>>();

		public float m_densityModifierApplied;

		public double? m_lastAttackedTime;

		public float m_sleepBlackoutFactor;

		public float m_sleepBlackoutDuration;

		public float m_environmentTemperature;

		public float m_environmentTemperatureFlux;

		public float m_temperatureBlackoutFactor;

		public float m_temperatureBlackoutDuration;

		public float Food
		{
			get
			{
				return m_food;
			}
			set
			{
				m_food = MathUtils.Saturate(value);
			}
		}

		public float Stamina
		{
			get
			{
				return m_stamina;
			}
			set
			{
				m_stamina = MathUtils.Saturate(value);
			}
		}

		public float Sleep
		{
			get
			{
				return m_sleep;
			}
			set
			{
				m_sleep = MathUtils.Saturate(value);
			}
		}

		public float Temperature
		{
			get
			{
				return m_temperature;
			}
			set
			{
				m_temperature = MathUtils.Clamp(value, 0f, 24f);
			}
		}

		public float Wetness
		{
			get
			{
				return m_wetness;
			}
			set
			{
				m_wetness = MathUtils.Saturate(value);
			}
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public bool Eat(int value)
		{
			int num = Terrain.ExtractContents(value);
			Block obj = BlocksManager.Blocks[num];
			float num2 = obj.GetNutritionalValue(value);
			float sicknessProbability = obj.GetSicknessProbability(value);
			if (num2 > 0f)
			{
				if (m_componentPlayer.ComponentSickness.IsSick && sicknessProbability > 0f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You feel too sick to eat this", Color.White, blinking: true, playNotificationSound: true);
					return false;
				}
				if (Food >= 0.98f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You are full, no more food!", Color.White, blinking: true, playNotificationSound: true);
					return false;
				}
				m_subsystemAudio.PlayRandomSound("Audio/Creatures/HumanEat", 1f, m_random.Float(-0.2f, 0.2f), m_componentPlayer.ComponentBody.Position, 2f, 0f);
				if (m_componentPlayer.ComponentSickness.IsSick)
				{
					num2 *= 0.75f;
				}
				Food += num2;
				m_satiation.TryGetValue(num, out float value2);
				value2 += MathUtils.Max(num2, 0.5f);
				m_satiation[num] = value2;
				if (m_componentPlayer.ComponentSickness.IsSick)
				{
					m_componentPlayer.ComponentSickness.NauseaEffect();
				}
				else if (sicknessProbability >= 0.5f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("It tastes horrible, no more!", Color.White, blinking: true, playNotificationSound: true);
				}
				else if (sicknessProbability > 0f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("It tastes odd, no more", Color.White, blinking: true, playNotificationSound: true);
				}
				else if (value2 > 2.5f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("Eat something else, you will get sick!", Color.White, blinking: true, playNotificationSound: true);
				}
				else if (value2 > 2f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You eat this too often", Color.White, blinking: true, playNotificationSound: true);
				}
				else if (Food > 0.85f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You have eaten well", Color.White, blinking: true, playNotificationSound: true);
				}
				else
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("Good, but you want more", Color.White, blinking: true, playNotificationSound: false);
				}
				if (m_random.Bool(sicknessProbability) || value2 > 3.5f)
				{
					m_componentPlayer.ComponentSickness.StartSickness();
				}
				m_componentPlayer.PlayerStats.FoodItemsEaten++;
				return true;
			}
			return false;
		}

		public void MakeSleepy(float sleepValue)
		{
			Sleep = MathUtils.Min(Sleep, sleepValue);
		}

		public void Update(float dt)
		{
			if (m_componentPlayer.ComponentHealth.Health > 0f)
			{
				UpdateFood();
				UpdateStamina();
				UpdateSleep();
				UpdateTemperature();
				UpdateWetness();
			}
			else
			{
				m_pantingSound.Stop();
			}
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_subsystemMetersBlockBehavior = base.Project.FindSubsystem<SubsystemMetersBlockBehavior>(throwOnError: true);
			m_subsystemWeather = base.Project.FindSubsystem<SubsystemWeather>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			m_pantingSound = m_subsystemAudio.CreateSound("Audio/HumanPanting");
			m_pantingSound.IsLooped = true;
			Food = valuesDictionary.GetValue<float>("Food");
			Stamina = valuesDictionary.GetValue<float>("Stamina");
			Sleep = valuesDictionary.GetValue<float>("Sleep");
			Temperature = valuesDictionary.GetValue<float>("Temperature");
			Wetness = valuesDictionary.GetValue<float>("Wetness");
			m_lastFood = Food;
			m_lastStamina = Stamina;
			m_lastSleep = Sleep;
			m_lastTemperature = Temperature;
			m_lastWetness = Wetness;
			m_environmentTemperature = Temperature;
			foreach (KeyValuePair<string, object> item in valuesDictionary.GetValue<ValuesDictionary>("Satiation"))
			{
				m_satiation[int.Parse(item.Key, CultureInfo.InvariantCulture)] = (float)item.Value;
			}
			m_componentPlayer.ComponentHealth.Attacked += delegate
			{
				m_lastAttackedTime = m_subsystemTime.GameTime;
			};
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("Food", Food);
			valuesDictionary.SetValue("Stamina", Stamina);
			valuesDictionary.SetValue("Sleep", Sleep);
			valuesDictionary.SetValue("Temperature", Temperature);
			valuesDictionary.SetValue("Wetness", Wetness);
			ValuesDictionary valuesDictionary2 = new ValuesDictionary();
			valuesDictionary.SetValue("Satiation", valuesDictionary2);
			foreach (KeyValuePair<int, float> item in m_satiation)
			{
				if (item.Value > 0f)
				{
					valuesDictionary2.SetValue(item.Key.ToString(CultureInfo.InvariantCulture), item.Value);
				}
			}
		}

		public override void OnEntityRemoved()
		{
			m_pantingSound.Stop();
		}

		public void UpdateFood()
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			float num = m_componentPlayer.ComponentLocomotion.LastWalkOrder.HasValue ? m_componentPlayer.ComponentLocomotion.LastWalkOrder.Value.Length() : 0f;
			float lastJumpOrder = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
			float num2 = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
			bool flag = m_componentPlayer.ComponentBody.ImmersionDepth > num2;
			bool flag2 = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
			bool flag3 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 13.0) && !m_componentPlayer.ComponentSickness.IsSick;
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				float hungerFactor = m_componentPlayer.ComponentLevel.HungerFactor;
				Food -= hungerFactor * gameTimeDelta / 2880f;
				if (flag2 | flag)
				{
					Food -= hungerFactor * gameTimeDelta * num / 1440f;
				}
				else
				{
					Food -= hungerFactor * gameTimeDelta * num / 2880f;
				}
				Food -= hungerFactor * lastJumpOrder / 1200f;
				if (m_componentPlayer.ComponentMiner.DigCellFace.HasValue)
				{
					Food -= hungerFactor * gameTimeDelta / 2880f;
				}
				if (!m_componentPlayer.ComponentSleep.IsSleeping)
				{
					if (Food <= 0f)
					{
						if (m_subsystemTime.PeriodicGameTimeEvent(50.0, 0.0))
						{
							m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, "Starved to death");
							m_componentPlayer.ComponentGui.DisplaySmallMessage("You are starving, find food!", Color.White, blinking: true, playNotificationSound: false);
							m_componentPlayer.ComponentGui.FoodBarWidget.Flash(10);
						}
					}
					else if (Food < 0.1f && ((m_lastFood >= 0.1f) | flag3))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are close to starvation", Color.White, blinking: true, playNotificationSound: true);
					}
					else if (Food < 0.25f && ((m_lastFood >= 0.25f) | flag3))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("Time to eat something", Color.White, blinking: true, playNotificationSound: true);
					}
					else if (Food < 0.5f && ((m_lastFood >= 0.5f) | flag3))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are slightly hungry", Color.White, blinking: true, playNotificationSound: false);
					}
				}
			}
			else
			{
				Food = 0.9f;
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(1.0, -0.01))
			{
				m_satiationList.Clear();
				m_satiationList.AddRange(m_satiation);
				m_satiation.Clear();
				foreach (KeyValuePair<int, float> satiation in m_satiationList)
				{
					float num3 = MathUtils.Max(satiation.Value - 0.000416666677f, 0f);
					if (num3 > 0f)
					{
						m_satiation.Add(satiation.Key, num3);
					}
				}
			}
			m_lastFood = Food;
			m_componentPlayer.ComponentGui.FoodBarWidget.Value = Food;
		}

		public void UpdateStamina()
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			float num = m_componentPlayer.ComponentLocomotion.LastWalkOrder.HasValue ? m_componentPlayer.ComponentLocomotion.LastWalkOrder.Value.Length() : 0f;
			float lastJumpOrder = m_componentPlayer.ComponentLocomotion.LastJumpOrder;
			float num2 = m_componentPlayer.ComponentCreatureModel.EyePosition.Y - m_componentPlayer.ComponentBody.Position.Y;
			bool flag = m_componentPlayer.ComponentBody.ImmersionDepth > num2;
			bool flag2 = m_componentPlayer.ComponentBody.ImmersionFactor > 0.33f && !m_componentPlayer.ComponentBody.StandingOnValue.HasValue;
			_ = m_componentPlayer.ComponentSickness.IsPuking;
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.GameMode != GameMode.Harmless && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				float num3 = 1f / MathUtils.Max(m_componentPlayer.ComponentLevel.SpeedFactor, 0.75f);
				if (m_componentPlayer.ComponentSickness.IsSick || m_componentPlayer.ComponentFlu.HasFlu)
				{
					num3 *= 5f;
				}
				Stamina += gameTimeDelta * 0.07f;
				Stamina -= 0.025f * lastJumpOrder * num3;
				if (flag2 | flag)
				{
					Stamina -= gameTimeDelta * (0.07f + 0.006f * num3 + 0.008f * num);
				}
				else
				{
					Stamina -= gameTimeDelta * (0.07f + 0.006f * num3) * num;
				}
				if (!flag2 && !flag && Stamina < 0.33f && m_lastStamina >= 0.33f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You are panting, slow down", Color.White, blinking: true, playNotificationSound: false);
				}
				if ((flag2 | flag) && Stamina < 0.4f && m_lastStamina >= 0.4f)
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("You are panting, get out of the water", Color.White, blinking: true, playNotificationSound: true);
				}
				if (Stamina < 0.1f)
				{
					if (flag2 | flag)
					{
						if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
						{
							m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, "Drowned");
							m_componentPlayer.ComponentGui.DisplaySmallMessage("You are drowning!", Color.White, blinking: true, playNotificationSound: false);
						}
						if (m_random.Float(0f, 1f) < 1f * gameTimeDelta)
						{
							m_componentPlayer.ComponentLocomotion.JumpOrder = 1f;
						}
					}
					else if (m_subsystemTime.PeriodicGameTimeEvent(5.0, 0.0))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("Rest a while!", Color.White, blinking: true, playNotificationSound: true);
					}
				}
				m_lastStamina = Stamina;
				float num4 = MathUtils.Saturate(2f * (0.5f - Stamina));
				if (!flag && num4 > 0f)
				{
					float num5 = (m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.2f : 0f;
					m_pantingSound.Volume = 1f * SettingsManager.SoundsVolume * MathUtils.Saturate(1f * num4) * MathUtils.Lerp(0.8f, 1f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 100.0, 1000.0)));
					m_pantingSound.Pitch = AudioManager.ToEnginePitch(num5 + MathUtils.Lerp(-0.15f, 0.05f, num4) * MathUtils.Lerp(0.8f, 1.2f, SimplexNoise.Noise((float)MathUtils.Remainder(3.0 * Time.RealTime + 200.0, 1000.0))));
					m_pantingSound.Play();
				}
				else
				{
					m_pantingSound.Stop();
				}
				float num6 = MathUtils.Saturate(3f * (0.33f - Stamina));
				if (num6 > 0f && SimplexNoise.Noise((float)MathUtils.Remainder(Time.RealTime, 1000.0)) < num6)
				{
					ApplyDensityModifier(0.6f);
				}
				else
				{
					ApplyDensityModifier(0f);
				}
			}
			else
			{
				Stamina = 1f;
				ApplyDensityModifier(0f);
			}
		}

		public void UpdateSleep()
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			bool flag = m_componentPlayer.ComponentBody.ImmersionFactor > 0.05f;
			bool flag2 = m_subsystemTime.PeriodicGameTimeEvent(240.0, 9.0);
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				if (m_componentPlayer.ComponentSleep.SleepFactor == 1f)
				{
					Sleep += 0.05f * gameTimeDelta;
				}
				else if (!flag && (!m_lastAttackedTime.HasValue || m_subsystemTime.GameTime - m_lastAttackedTime > 10.0))
				{
					Sleep -= gameTimeDelta / 1800f;
					if (Sleep < 0.075f && ((m_lastSleep >= 0.075f) | flag2))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You will faint, go to sleep!", Color.White, blinking: true, playNotificationSound: true);
						m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
					}
					else if (Sleep < 0.2f && ((m_lastSleep >= 0.2f) | flag2))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are falling over, sleep", Color.White, blinking: true, playNotificationSound: true);
						m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
					}
					else if (Sleep < 0.33f && ((m_lastSleep >= 0.33f) | flag2))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are very tired, sleep", Color.White, blinking: true, playNotificationSound: false);
					}
					else if (Sleep < 0.5f && ((m_lastSleep >= 0.5f) | flag2))
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are tired, take a nap", Color.White, blinking: true, playNotificationSound: false);
					}
					if (Sleep < 0.075f)
					{
						float num = MathUtils.Lerp(0.05f, 0.2f, (0.075f - Sleep) / 0.075f);
						float x = (Sleep < 0.0375f) ? m_random.Float(3f, 6f) : m_random.Float(2f, 4f);
						if (m_random.Float(0f, 1f) < num * gameTimeDelta)
						{
							m_sleepBlackoutDuration = MathUtils.Max(m_sleepBlackoutDuration, x);
							m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
						}
					}
					if (Sleep <= 0f && !m_componentPlayer.ComponentSleep.IsSleeping)
					{
						m_componentPlayer.ComponentSleep.Sleep(allowManualWakeup: false);
						m_componentPlayer.ComponentGui.DisplaySmallMessage("Can't go no more", Color.White, blinking: true, playNotificationSound: true);
						m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
					}
				}
			}
			else
			{
				Sleep = 0.9f;
			}
			m_lastSleep = Sleep;
			m_sleepBlackoutDuration -= gameTimeDelta;
			float num2 = MathUtils.Saturate(0.5f * m_sleepBlackoutDuration);
			m_sleepBlackoutFactor = MathUtils.Saturate(m_sleepBlackoutFactor + 2f * gameTimeDelta * (num2 - m_sleepBlackoutFactor));
			if (!m_componentPlayer.ComponentSleep.IsSleeping)
			{
				m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_sleepBlackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
				if ((double)m_sleepBlackoutFactor > 0.01)
				{
					m_componentPlayer.ComponentScreenOverlays.FloatingMessage = "Aaa...";
					m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_sleepBlackoutFactor - 0.9f));
				}
			}
		}

		public void UpdateTemperature()
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			bool flag = m_subsystemTime.PeriodicGameTimeEvent(300.0, 17.0);
			float num = m_componentPlayer.ComponentClothing.Insulation * MathUtils.Lerp(1f, 0.05f, MathUtils.Saturate(4f * Wetness));
			string arg;
			switch (m_componentPlayer.ComponentClothing.LeastInsulatedSlot)
			{
			case ClothingSlot.Head:
				arg = "head is";
				break;
			case ClothingSlot.Torso:
				arg = "chest is";
				break;
			case ClothingSlot.Legs:
				arg = "legs are";
				break;
			default:
				arg = "feet are";
				break;
			}
			if (m_subsystemTime.PeriodicGameTimeEvent(2.0, 2.0 * (double)GetHashCode() % 1000.0 / 1000.0))
			{
				int x = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.X);
				int y = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Y + 0.1f);
				int z = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Z);
				m_subsystemMetersBlockBehavior.CalculateTemperature(x, y, z, 12f, num, out m_environmentTemperature, out m_environmentTemperatureFlux);
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode != 0 && m_subsystemGameInfo.WorldSettings.AreAdventureSurvivalMechanicsEnabled)
			{
				float num2 = m_environmentTemperature - Temperature;
				float num3 = 0.01f + 0.005f * m_environmentTemperatureFlux;
				Temperature += MathUtils.Saturate(num3 * gameTimeDelta) * num2;
			}
			else
			{
				Temperature = 12f;
			}
			if (Temperature <= 0f)
			{
				m_componentPlayer.ComponentHealth.Injure(1f, null, ignoreInvulnerability: false, "Froze to death");
			}
			else if (Temperature < 3f)
			{
				if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
				{
					m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, "Hypothermia");
					string text = (Wetness > 0f) ? $"Your {arg} freezing, dry your clothes!" : ((!(num < 1f)) ? $"Your {arg} freezing, seek shelter!" : $"Your {arg} freezing, get clothed!");
					m_componentPlayer.ComponentGui.DisplaySmallMessage(text, Color.White, blinking: true, playNotificationSound: false);
					m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
				}
			}
			else if (Temperature < 6f && ((m_lastTemperature >= 6f) | flag))
			{
				string text2 = (Wetness > 0f) ? $"Your {arg} getting cold, dry your clothes" : ((!(num < 1f)) ? $"Your {arg} getting cold, seek shelter" : $"Your {arg} getting cold, get clothed");
				m_componentPlayer.ComponentGui.DisplaySmallMessage(text2, Color.White, blinking: true, playNotificationSound: true);
				m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
			}
			else if (Temperature < 8f && ((m_lastTemperature >= 8f) | flag))
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage("You feel a bit chilly", Color.White, blinking: true, playNotificationSound: false);
				m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
			}
			if (Temperature >= 24f)
			{
				if (m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
				{
					m_componentPlayer.ComponentGui.DisplaySmallMessage("It's too hot, run away!", Color.White, blinking: true, playNotificationSound: false);
					m_componentPlayer.ComponentHealth.Injure(0.05f, null, ignoreInvulnerability: false, "Overheated");
					m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
				}
				if (m_subsystemTime.PeriodicGameTimeEvent(8.0, 0.0))
				{
					m_temperatureBlackoutDuration = MathUtils.Max(m_temperatureBlackoutDuration, 6f);
					m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
				}
			}
			else if (Temperature > 20f && m_subsystemTime.PeriodicGameTimeEvent(10.0, 0.0))
			{
				m_componentPlayer.ComponentGui.DisplaySmallMessage("You feel hot", Color.White, blinking: true, playNotificationSound: false);
				m_temperatureBlackoutDuration = MathUtils.Max(m_temperatureBlackoutDuration, 3f);
				m_componentPlayer.ComponentGui.TemperatureBarWidget.Flash(10);
				m_componentPlayer.ComponentCreatureSounds.PlayMoanSound();
			}
			m_lastTemperature = Temperature;
			m_componentPlayer.ComponentScreenOverlays.IceFactor = MathUtils.Saturate(1f - Temperature / 6f);
			m_temperatureBlackoutDuration -= gameTimeDelta;
			float num4 = MathUtils.Saturate(0.5f * m_temperatureBlackoutDuration);
			m_temperatureBlackoutFactor = MathUtils.Saturate(m_temperatureBlackoutFactor + 2f * gameTimeDelta * (num4 - m_temperatureBlackoutFactor));
			m_componentPlayer.ComponentScreenOverlays.BlackoutFactor = MathUtils.Max(m_temperatureBlackoutFactor, m_componentPlayer.ComponentScreenOverlays.BlackoutFactor);
			if ((double)m_temperatureBlackoutFactor > 0.01)
			{
				m_componentPlayer.ComponentScreenOverlays.FloatingMessage = "Ugh...";
				m_componentPlayer.ComponentScreenOverlays.FloatingMessageFactor = MathUtils.Saturate(10f * (m_temperatureBlackoutFactor - 0.9f));
			}
			if (m_environmentTemperature > 22f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature6");
			}
			else if (m_environmentTemperature > 18f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature5");
			}
			else if (m_environmentTemperature > 14f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature4");
			}
			else if (m_environmentTemperature > 10f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature3");
			}
			else if (m_environmentTemperature > 6f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature2");
			}
			else if (m_environmentTemperature > 2f)
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature1");
			}
			else
			{
				m_componentPlayer.ComponentGui.TemperatureBarWidget.BarSubtexture = ContentManager.Get<Subtexture>("Textures/Atlas/Temperature0");
			}
		}

		public void UpdateWetness()
		{
			float gameTimeDelta = m_subsystemTime.GameTimeDelta;
			if (m_componentPlayer.ComponentBody.ImmersionFactor > 0.2f && m_componentPlayer.ComponentBody.ImmersionFluidBlock is WaterBlock)
			{
				float num = 2f * m_componentPlayer.ComponentBody.ImmersionFactor;
				Wetness += MathUtils.Saturate(3f * gameTimeDelta) * (num - Wetness);
			}
			int x = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.X);
			int num2 = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Y + 0.1f);
			int z = Terrain.ToCell(m_componentPlayer.ComponentBody.Position.Z);
			PrecipitationShaftInfo precipitationShaftInfo = m_subsystemWeather.GetPrecipitationShaftInfo(x, z);
			if (num2 >= precipitationShaftInfo.YLimit && precipitationShaftInfo.Type == PrecipitationType.Rain)
			{
				Wetness += 0.05f * precipitationShaftInfo.Intensity * gameTimeDelta;
			}
			float num3 = 180f;
			if (m_environmentTemperature > 8f)
			{
				num3 = 120f;
			}
			if (m_environmentTemperature > 16f)
			{
				num3 = 60f;
			}
			if (m_environmentTemperature > 24f)
			{
				num3 = 30f;
			}
			Wetness -= gameTimeDelta / num3;
			if (Wetness > 0.8f && m_lastWetness <= 0.8f)
			{
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, delegate
				{
					if (Wetness > 0.8f)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are completely wet", Color.White, blinking: true, playNotificationSound: true);
					}
				});
			}
			else if (Wetness > 0.2f && m_lastWetness <= 0.2f)
			{
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, delegate
				{
					if (Wetness > 0.2f && Wetness <= 0.8f && Wetness > m_lastWetness)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are getting wet", Color.White, blinking: true, playNotificationSound: true);
					}
				});
			}
			else if (Wetness <= 0f && m_lastWetness > 0f)
			{
				Time.QueueTimeDelayedExecution(Time.FrameStartTime + 2.0, delegate
				{
					if (Wetness <= 0f)
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You are no longer wet", Color.White, blinking: true, playNotificationSound: true);
					}
				});
			}
			m_lastWetness = Wetness;
		}

		public void ApplyDensityModifier(float modifier)
		{
			float num = modifier - m_densityModifierApplied;
			if (num != 0f)
			{
				m_densityModifierApplied = modifier;
				m_componentPlayer.ComponentBody.Density += num;
			}
		}
	}
}
