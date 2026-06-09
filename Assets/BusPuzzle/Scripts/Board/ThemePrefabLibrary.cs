using UnityEngine;

namespace BusPuzzle
{
    public sealed class ThemePrefabLibrary : ScriptableObject
    {
        public const string ResourcePath = "Theme/ThemePrefabLibrary";

        private static ThemePrefabLibrary cached;

        [SerializeField] private GameObject parkingSurfacePrefab;
        [SerializeField] private GameObject[] buildingPrefabs;
        [SerializeField] private GameObject[] treePrefabs;
        [SerializeField] private GameObject[] signPrefabs;
        [SerializeField] private GameObject[] bushPrefabs;

        public static ThemePrefabLibrary Load()
        {
            if (cached == null)
            {
                cached = Resources.Load<ThemePrefabLibrary>(ResourcePath);
            }

            return cached;
        }

        public bool TryGetParkingSurface(out GameObject prefab)
        {
            prefab = parkingSurfacePrefab;
            return prefab != null;
        }

        public bool TryGetBuilding(int index, out GameObject prefab)
        {
            return TryGet(buildingPrefabs, index, out prefab);
        }

        public bool TryGetTree(int index, out GameObject prefab)
        {
            return TryGet(treePrefabs, index, out prefab);
        }

        public bool TryGetSign(int index, out GameObject prefab)
        {
            return TryGet(signPrefabs, index, out prefab);
        }

        public bool TryGetBush(int index, out GameObject prefab)
        {
            return TryGet(bushPrefabs, index, out prefab);
        }

        private static bool TryGet(GameObject[] prefabs, int index, out GameObject prefab)
        {
            prefab = null;
            if (prefabs == null || prefabs.Length == 0)
            {
                return false;
            }

            var safeIndex = Mathf.Abs(index) % prefabs.Length;
            prefab = prefabs[safeIndex];
            return prefab != null;
        }
    }
}
