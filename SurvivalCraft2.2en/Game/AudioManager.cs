using Engine;
using Engine.Audio;
using System;

namespace Game
{
	public static class AudioManager
	{
		public static float MinAudibleVolume => 0.05f * SettingsManager.SoundsVolume;

		public static void PlaySound(string name, float volume, float pitch, float pan)
		{
			if (SettingsManager.SoundsVolume > 0f)
			{
				float num = volume * SettingsManager.SoundsVolume;
				if (num > MinAudibleVolume)
				{
					try
					{
						new Sound(ContentManager.Get<SoundBuffer>(name), num, ToEnginePitch(pitch), pan, isLooped: false, disposeOnStop: true).Play();
					}
					catch (Exception)
					{
					}
				}
			}
		}

		public static float ToEnginePitch(float pitch)
		{
			return MathUtils.Pow(2f, pitch);
		}
	}
}
