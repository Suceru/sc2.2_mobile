using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using TemplatesDatabase;

namespace Game
{
	public class ComponentLevel : Component, IUpdateable
	{
		public struct Factor
		{
			public string Description;

			public float Value;
		}

		public Random m_random = new Random();

		public List<Factor> m_factors = new List<Factor>();

		public float? m_lastLevelTextValue;

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemAudio m_subsystemAudio;

		public SubsystemTime m_subsystemTime;

		public ComponentPlayer m_componentPlayer;

		public const float FemaleStrengthFactor = 0.8f;

		public const float FemaleResilienceFactor = 0.8f;

		public const float FemaleSpeedFactor = 1.03f;

		public const float FemaleHungerFactor = 0.7f;

		public float StrengthFactor
		{
			get;
			set;
		}

		public float ResilienceFactor
		{
			get;
			set;
		}

		public float SpeedFactor
		{
			get;
			set;
		}

		public float HungerFactor
		{
			get;
			set;
		}

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public void AddExperience(int count, bool playSound)
		{
			if (playSound)
			{
				m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 0.2f, m_random.Float(-0.1f, 0.4f), 0f, 0f);
			}
			for (int i = 0; i < count; i++)
			{
				float num = 0.012f / MathUtils.Pow(1.08f, MathUtils.Floor(m_componentPlayer.PlayerData.Level - 1f));
				if (MathUtils.Floor(m_componentPlayer.PlayerData.Level + num) > MathUtils.Floor(m_componentPlayer.PlayerData.Level))
				{
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.0, delegate
					{
						m_componentPlayer.ComponentGui.DisplaySmallMessage("You've gained a level!", Color.White, blinking: true, playNotificationSound: false);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.0, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.2f, 0f, 0f);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.15000000596046448, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.03333333f, 0f, 0f);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.30000001192092896, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 142f / (339f * (float)Math.PI), 0f, 0f);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.45000001788139343, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 23f / 60f, 0f, 0f);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.75, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, -0.03333333f, 0f, 0f);
					});
					Time.QueueTimeDelayedExecution(Time.FrameStartTime + 0.5 + 0.90000003576278687, delegate
					{
						m_subsystemAudio.PlaySound("Audio/ExperienceCollected", 1f, 23f / 60f, 0f, 0f);
					});
				}
				m_componentPlayer.PlayerData.Level += num;
			}
		}

		public float CalculateStrengthFactor(ICollection<Factor> factors)
		{
			float num = (m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.8f : 1f;
			float num2 = 1f * num;
			Factor item;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num,
					Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
				};
				factors.Add(item);
			}
			float level = m_componentPlayer.PlayerData.Level;
			float num3 = 1f + 0.05f * MathUtils.Floor(MathUtils.Clamp(level, 1f, 21f) - 1f);
			float num4 = num2 * num3;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num3,
					Description = "Level " + MathUtils.Floor(level).ToString()
				};
				factors.Add(item);
			}
			float stamina = m_componentPlayer.ComponentVitalStats.Stamina;
			float num5 = MathUtils.Lerp(0.5f, 1f, MathUtils.Saturate(4f * stamina)) * MathUtils.Lerp(0.9f, 1f, MathUtils.Saturate(stamina));
			float num6 = num4 * num5;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num5,
					Description = $"{stamina * 100f:0}% Stamina"
				};
				factors.Add(item);
			}
			float num7 = m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f;
			float num8 = num6 * num7;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num7,
					Description = (m_componentPlayer.ComponentSickness.IsSick ? "Sickness" : "No Sickness")
				};
				factors.Add(item);
			}
			float num9 = (!m_componentPlayer.ComponentSickness.IsPuking) ? 1 : 0;
			float num10 = num8 * num9;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num9,
					Description = (m_componentPlayer.ComponentSickness.IsPuking ? "Vomiting" : "Not Vomiting")
				};
				factors.Add(item);
			}
			float num11 = m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f;
			float num12 = num10 * num11;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num11,
					Description = (m_componentPlayer.ComponentFlu.HasFlu ? "Flu" : "No Flu")
				};
				factors.Add(item);
			}
			float num13 = (!m_componentPlayer.ComponentFlu.IsCoughing) ? 1 : 0;
			float num14 = num12 * num13;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num13,
					Description = (m_componentPlayer.ComponentFlu.IsCoughing ? "Coughing" : "Not Coughing")
				};
				factors.Add(item);
			}
			float num15 = (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless) ? 1.25f : 1f;
			float result = num14 * num15;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num15,
					Description = m_subsystemGameInfo.WorldSettings.GameMode.ToString() + " Mode"
				};
				factors.Add(item);
			}
			return result;
		}

		public float CalculateResilienceFactor(ICollection<Factor> factors)
		{
			float num = (m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.8f : 1f;
			float num2 = 1f * num;
			Factor item;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num,
					Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
				};
				factors.Add(item);
			}
			float level = m_componentPlayer.PlayerData.Level;
			float num3 = 1f + 0.05f * MathUtils.Floor(MathUtils.Clamp(level, 1f, 21f) - 1f);
			float num4 = num2 * num3;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num3,
					Description = "Level " + MathUtils.Floor(level).ToString()
				};
				factors.Add(item);
			}
			float num5 = m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f;
			float num6 = num4 * num5;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num5,
					Description = (m_componentPlayer.ComponentSickness.IsSick ? "Sickness" : "No Sickness")
				};
				factors.Add(item);
			}
			float num7 = m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f;
			float num8 = num6 * num7;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num7,
					Description = (m_componentPlayer.ComponentFlu.HasFlu ? "Flu" : "No Flu")
				};
				factors.Add(item);
			}
			float num9 = 1f;
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
			{
				num9 = 1.5f;
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				num9 = 1f / 0f;
			}
			float result = num8 * num9;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num9,
					Description = m_subsystemGameInfo.WorldSettings.GameMode.ToString() + " Mode"
				};
				factors.Add(item);
			}
			return result;
		}

		public float CalculateSpeedFactor(ICollection<Factor> factors)
		{
			float num = 1f;
			float num2 = (m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 1.03f : 1f;
			num *= num2;
			Factor item;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num2,
					Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
				};
				factors.Add(item);
			}
			float level = m_componentPlayer.PlayerData.Level;
			float num3 = 1f + 0.02f * MathUtils.Floor(MathUtils.Clamp(level, 1f, 21f) - 1f);
			num *= num3;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num3,
					Description = "Level " + MathUtils.Floor(level).ToString()
				};
				factors.Add(item);
			}
			float clothingFactor = 1f;
			foreach (int clothe in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Head))
			{
				AddClothingFactor(clothe, ref clothingFactor, factors);
			}
			foreach (int clothe2 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Torso))
			{
				AddClothingFactor(clothe2, ref clothingFactor, factors);
			}
			foreach (int clothe3 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Legs))
			{
				AddClothingFactor(clothe3, ref clothingFactor, factors);
			}
			foreach (int clothe4 in m_componentPlayer.ComponentClothing.GetClothes(ClothingSlot.Feet))
			{
				AddClothingFactor(clothe4, ref clothingFactor, factors);
			}
			num *= clothingFactor;
			float stamina = m_componentPlayer.ComponentVitalStats.Stamina;
			float num4 = MathUtils.Lerp(0.5f, 1f, MathUtils.Saturate(4f * stamina)) * MathUtils.Lerp(0.9f, 1f, MathUtils.Saturate(stamina));
			num *= num4;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num4,
					Description = $"{stamina * 100f:0}% Stamina"
				};
				factors.Add(item);
			}
			float num5 = m_componentPlayer.ComponentSickness.IsSick ? 0.75f : 1f;
			num *= num5;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num5,
					Description = (m_componentPlayer.ComponentSickness.IsSick ? "Sickness" : "No Sickness")
				};
				factors.Add(item);
			}
			float num6 = (!m_componentPlayer.ComponentSickness.IsPuking) ? 1 : 0;
			num *= num6;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num6,
					Description = (m_componentPlayer.ComponentSickness.IsPuking ? "Vomiting" : "Not Vomiting")
				};
				factors.Add(item);
			}
			float num7 = m_componentPlayer.ComponentFlu.HasFlu ? 0.75f : 1f;
			num *= num7;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num7,
					Description = (m_componentPlayer.ComponentFlu.HasFlu ? "Flu" : "No Flu")
				};
				factors.Add(item);
			}
			float num8 = (!m_componentPlayer.ComponentFlu.IsCoughing) ? 1 : 0;
			num *= num8;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num8,
					Description = (m_componentPlayer.ComponentFlu.IsCoughing ? "Coughing" : "Not Coughing")
				};
				factors.Add(item);
			}
			return num;
		}

		public float CalculateHungerFactor(ICollection<Factor> factors)
		{
			float num = (m_componentPlayer.PlayerData.PlayerClass == PlayerClass.Female) ? 0.7f : 1f;
			float num2 = 1f * num;
			Factor item;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num,
					Description = m_componentPlayer.PlayerData.PlayerClass.ToString()
				};
				factors.Add(item);
			}
			float level = m_componentPlayer.PlayerData.Level;
			float num3 = 1f - 0.01f * MathUtils.Floor(MathUtils.Clamp(level, 1f, 21f) - 1f);
			float num4 = num2 * num3;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num3,
					Description = "Level " + MathUtils.Floor(level).ToString()
				};
				factors.Add(item);
			}
			float num5 = 1f;
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Harmless)
			{
				num5 = 0.66f;
			}
			if (m_subsystemGameInfo.WorldSettings.GameMode == GameMode.Creative)
			{
				num5 = 0f;
			}
			float result = num4 * num5;
			if (factors != null)
			{
				item = new Factor
				{
					Value = num5,
					Description = m_subsystemGameInfo.WorldSettings.GameMode.ToString() + " Mode"
				};
				factors.Add(item);
			}
			return result;
		}

		public void Update(float dt)
		{
			if (m_subsystemTime.PeriodicGameTimeEvent(180.0, 179.0))
			{
				AddExperience(1, playSound: false);
			}
			StrengthFactor = CalculateStrengthFactor(null);
			SpeedFactor = CalculateSpeedFactor(null);
			HungerFactor = CalculateHungerFactor(null);
			ResilienceFactor = CalculateResilienceFactor(null);
			if (!m_lastLevelTextValue.HasValue || m_lastLevelTextValue.Value != MathUtils.Floor(m_componentPlayer.PlayerData.Level))
			{
				m_componentPlayer.ComponentGui.LevelLabelWidget.Text = "Level " + MathUtils.Floor(m_componentPlayer.PlayerData.Level).ToString();
				m_lastLevelTextValue = MathUtils.Floor(m_componentPlayer.PlayerData.Level);
			}
			m_componentPlayer.PlayerStats.HighestLevel = MathUtils.Max(m_componentPlayer.PlayerStats.HighestLevel, m_componentPlayer.PlayerData.Level);
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemAudio = base.Project.FindSubsystem<SubsystemAudio>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			StrengthFactor = 1f;
			SpeedFactor = 1f;
			HungerFactor = 1f;
			ResilienceFactor = 1f;
		}

		public static void AddClothingFactor(int clothingValue, ref float clothingFactor, ICollection<Factor> factors)
		{
			ClothingData clothingData = ClothingBlock.GetClothingData(Terrain.ExtractData(clothingValue));
			if (clothingData.MovementSpeedFactor != 1f)
			{
				clothingFactor *= clothingData.MovementSpeedFactor;
				factors?.Add(new Factor
				{
					Value = clothingData.MovementSpeedFactor,
					Description = clothingData.DisplayName
				});
			}
		}
	}
}
