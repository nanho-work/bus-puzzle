using UnityEngine;

namespace BusPuzzle
{
    public sealed class VehiclePrefabLibrary : ScriptableObject
    {
        public const string ResourcePath = "Vehicles/VehiclePrefabLibrary";
        private const string LargeBusResourcePath = "ShopVehicles/Models/shop_large_bus_yellow";

        private static VehiclePrefabLibrary cached;
        private static GameObject cachedLargeBusPrefab;

        [SerializeField] private GameObject smallPrefab;
        [SerializeField] private GameObject mediumPrefab;
        [SerializeField] private GameObject largePrefab;

        public static VehiclePrefabLibrary Load()
        {
#if UNITY_EDITOR
            cached = Resources.Load<VehiclePrefabLibrary>(ResourcePath);
            return cached;
#else
            if (cached == null)
            {
                cached = Resources.Load<VehiclePrefabLibrary>(ResourcePath);
            }

            return cached;
#endif
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
                    prefab = LoadLargeBusPrefab();
                    if (prefab == null)
                    {
                        prefab = largePrefab;
                    }
                    break;
                default:
                    prefab = null;
                    break;
            }

            return prefab != null;
        }

        private static GameObject LoadLargeBusPrefab()
        {
#if UNITY_EDITOR
            return Resources.Load<GameObject>(LargeBusResourcePath);
#else
            if (cachedLargeBusPrefab == null)
            {
                cachedLargeBusPrefab = Resources.Load<GameObject>(LargeBusResourcePath);
            }

            return cachedLargeBusPrefab;
#endif
        }
    }
}
