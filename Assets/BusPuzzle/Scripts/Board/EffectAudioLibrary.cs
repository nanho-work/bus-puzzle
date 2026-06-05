using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Effect Audio Library", fileName = "EffectAudioLibrary")]
    public sealed class EffectAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip collisionClip = null;
        [SerializeField] private AudioClip boardingClip = null;
        [SerializeField, Range(0f, 1f)] private float collisionVolume = 0.80f;
        [SerializeField, Range(0f, 1f)] private float boardingVolume = 0.55f;

        public AudioClip CollisionClip => collisionClip;
        public AudioClip BoardingClip => boardingClip;
        public float CollisionVolume => collisionVolume;
        public float BoardingVolume => boardingVolume;
    }
}
