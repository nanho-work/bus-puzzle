using UnityEngine;

namespace BusPuzzle
{
    public sealed class VehiclePrefabLibrary : ScriptableObject
    {
        public const string ResourcePath = "Vehicles/VehiclePrefabLibrary";

        private static VehiclePrefabLibrary cached;

        [SerializeField] private GameObject smallPrefab;
        [SerializeField] private GameObject mediumPrefab;
        [SerializeField] private GameObject largePrefab;

        public static VehiclePrefabLibrary Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<VehiclePrefabLibrary>(ResourcePath);
            }

            return cached;
        }

        public bool TryGetPrefab(BusSize size, out GameObject prefab)
        {
            switch (size)
            {
                case BusSize.Small:
                    prefab = smallPrefab;
                    break;
                case BusSize.Medium:
                    prefab = mediumPrefab;
                    break;
                case BusSize.Large:
                    prefab = largePrefab;
                    break;
                default:
                    prefab = null;
                    break;
            }

            return prefab != null;
        }
    }
}
