using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Effect Audio Library", fileName = "EffectAudioLibrary")]
    public sealed class EffectAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip collisionClip = null;
        [SerializeField] private AudioClip boardingClip = null;
        [SerializeField] private AudioClip victoryClip = null;
        [SerializeField] private AudioClip failClip = null;
        [SerializeField] private AudioClip backgroundMusicClip = null;
        [SerializeField, Range(0f, 1f)] private float collisionVolume = 0.80f;
        [SerializeField, Range(0f, 1f)] private float boardingVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float failVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.42f;

        public AudioClip CollisionClip => collisionClip;
        public AudioClip BoardingClip => boardingClip;
        public AudioClip VictoryClip => victoryClip;
        public AudioClip FailClip => failClip;
        public AudioClip BackgroundMusicClip => backgroundMusicClip;
        public float CollisionVolume => collisionVolume;
        public float BoardingVolume => boardingVolume;
        public float VictoryVolume => victoryVolume;
        public float FailVolume => failVolume;
        public float BackgroundMusicVolume => backgroundMusicVolume;
    }
}
