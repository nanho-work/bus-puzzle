using UnityEngine;

namespace BusPuzzle
{
    internal static class BackgroundMusicPlayer
    {
        private const string LibraryResourcePath = "Audio/EffectAudioLibrary";

        private enum MusicMode
        {
            Default,
            DailyChallengeEvent
        }

        private static EffectAudioLibrary library;
        private static AudioSource source;
        private static MusicMode currentMode = MusicMode.Default;

        public static void ApplyPreferences()
        {
            if (!UserPreferences.MainSoundEnabled)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }

                return;
            }

            PlayCurrentMode();
        }

        public static void Play()
        {
            PlayDefault();
        }

        public static void PlayDefault()
        {
            Play(MusicMode.Default);
        }

        public static void PlayDailyChallengeEvent()
        {
            Play(MusicMode.DailyChallengeEvent);
        }

        private static void Play(MusicMode mode)
        {
            currentMode = mode;
            if (!UserPreferences.MainSoundEnabled)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }

                return;
            }

            PlayCurrentMode();
        }

        private static void PlayCurrentMode()
        {
            var audioLibrary = GetLibrary();
            var clip = GetClip(audioLibrary, currentMode);
            if (clip == null)
            {
                return;
            }

            EnsureSource();
            var clipChanged = source.clip != clip;
            if (clipChanged)
            {
                source.Stop();
                source.clip = clip;
            }

            source.volume = GetVolume(audioLibrary, currentMode);
            source.loop = true;

            if (clipChanged || !source.isPlaying)
            {
                source.Play();
            }
        }

        private static AudioClip GetClip(EffectAudioLibrary audioLibrary, MusicMode mode)
        {
            if (audioLibrary == null)
            {
                return null;
            }

            if (mode == MusicMode.DailyChallengeEvent && audioLibrary.DailyChallengeMusicClip != null)
            {
                return audioLibrary.DailyChallengeMusicClip;
            }

            return audioLibrary.BackgroundMusicClip;
        }

        private static float GetVolume(EffectAudioLibrary audioLibrary, MusicMode mode)
        {
            if (audioLibrary == null)
            {
                return 0f;
            }

            return mode == MusicMode.DailyChallengeEvent && audioLibrary.DailyChallengeMusicClip != null
                ? audioLibrary.DailyChallengeMusicVolume
                : audioLibrary.BackgroundMusicVolume;
        }

        private static EffectAudioLibrary GetLibrary()
        {
            if (library == null)
            {
                library = Resources.Load<EffectAudioLibrary>(LibraryResourcePath);
            }

            return library;
        }

        private static void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            var audioObject = new GameObject("Background Music Player");
            Object.DontDestroyOnLoad(audioObject);

            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
        }
    }
}
