using UnityEngine;

namespace BusPuzzle
{
    internal static class BackgroundMusicPlayer
    {
        private const string LibraryResourcePath = "Audio/EffectAudioLibrary";

        private static EffectAudioLibrary library;
        private static AudioSource source;

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

            Play();
        }

        public static void Play()
        {
            var audioLibrary = GetLibrary();
            var clip = audioLibrary != null ? audioLibrary.BackgroundMusicClip : null;
            if (clip == null)
            {
                return;
            }

            EnsureSource();
            if (source.clip != clip)
            {
                source.clip = clip;
            }

            source.volume = audioLibrary.BackgroundMusicVolume;
            source.loop = true;

            if (!source.isPlaying)
            {
                source.Play();
            }
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
