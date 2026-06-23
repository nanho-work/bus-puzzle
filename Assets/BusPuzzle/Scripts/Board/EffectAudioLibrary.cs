using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Effect Audio Library", fileName = "EffectAudioLibrary")]
    public sealed class EffectAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip collisionClip = null;
        [SerializeField] private AudioClip boardingClip = null;
        [SerializeField] private AudioClip vehicleLaunchClip = null;
        [SerializeField] private AudioClip busFullClip = null;
        [SerializeField] private AudioClip victoryClip = null;
        [SerializeField] private AudioClip failClip = null;
        [SerializeField] private AudioClip backgroundMusicClip = null;
        [SerializeField] private AudioClip dailyChallengeMusicClip = null;
        [SerializeField, Range(0f, 1f)] private float collisionVolume = 0.80f;
        [SerializeField, Range(0f, 1f)] private float boardingVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float vehicleLaunchVolume = 0.70f;
        [SerializeField, Range(0f, 1f)] private float busFullVolume = 0.78f;
        [SerializeField, Range(0f, 1f)] private float victoryVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float failVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float dailyChallengeMusicVolume = 0.42f;

        public AudioClip CollisionClip => collisionClip;
        public AudioClip BoardingClip => boardingClip;
        public AudioClip VehicleLaunchClip => vehicleLaunchClip;
        public AudioClip BusFullClip => busFullClip;
        public AudioClip VictoryClip => victoryClip;
        public AudioClip FailClip => failClip;
        public AudioClip BackgroundMusicClip => backgroundMusicClip;
        public AudioClip DailyChallengeMusicClip => dailyChallengeMusicClip;
        public float CollisionVolume => collisionVolume;
        public float BoardingVolume => boardingVolume;
        public float VehicleLaunchVolume => vehicleLaunchVolume;
        public float BusFullVolume => busFullVolume;
        public float VictoryVolume => victoryVolume;
        public float FailVolume => failVolume;
        public float BackgroundMusicVolume => backgroundMusicVolume;
        public float DailyChallengeMusicVolume => dailyChallengeMusicVolume;
    }
}
