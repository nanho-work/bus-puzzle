using UnityEngine;

namespace BusPuzzle
{
    internal static class EffectAudioPlayer
    {
        private const string LibraryResourcePath = "Audio/EffectAudioLibrary";

        private static EffectAudioLibrary library;
        private static AudioSource source;

        public static void PlayCollision(Vector3 position)
        {
            Play(GetLibrary()?.CollisionClip, GetLibrary()?.CollisionVolume ?? 0f);
        }

        public static void PlayBoarding(Vector3 position)
        {
            Play(GetLibrary()?.BoardingClip, GetLibrary()?.BoardingVolume ?? 0f);
        }

        private static EffectAudioLibrary GetLibrary()
        {
            if (library == null)
            {
                library = Resources.Load<EffectAudioLibrary>(LibraryResourcePath);
            }

            return library;
        }

        private static void Play(AudioClip clip, float volume)
        {
            if (clip == null || volume <= 0.001f)
            {
                return;
            }

            EnsureSource();
            source.PlayOneShot(clip, volume);
        }

        private static void EnsureSource()
        {
            if (source != null)
            {
                return;
            }

            var audioObject = new GameObject("Effect Audio Player");
            Object.DontDestroyOnLoad(audioObject);

            source = audioObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }
    }
}
