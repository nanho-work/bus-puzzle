using UnityEngine;

namespace BusPuzzle
{
    public sealed class PassengerPrefabLibrary : ScriptableObject
    {
        public const string ResourcePath = "Passengers/PassengerPrefabLibrary";

        private static PassengerPrefabLibrary cached;

        [SerializeField] private GameObject passengerPrefab;

        public static PassengerPrefabLibrary Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<PassengerPrefabLibrary>(ResourcePath);
            }

            return cached;
        }

        public bool TryGetPassengerPrefab(out GameObject prefab)
        {
            prefab = passengerPrefab;
            return prefab != null;
        }
    }
}
