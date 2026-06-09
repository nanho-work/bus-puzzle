using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    internal static class ThemePrefabUtility
    {
        public static GameObject InstantiateUniform(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 groundCenter,
            Vector2 maxFootprint,
            float maxHeight,
            Quaternion rotation)
        {
            var instance = InstantiatePrepared(prefab, name, parent, rotation);
            if (instance == null)
            {
                return null;
            }

            if (!TryGetBounds(instance, out var bounds))
            {
                instance.transform.localPosition = groundCenter;
                return instance;
            }

            var scale = CalculateUniformScale(bounds, maxFootprint, maxHeight);
            instance.transform.localScale = Vector3.one * scale;
            AlignBoundsToGround(instance, groundCenter);
            return instance;
        }

        public static GameObject InstantiateFloor(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 center,
            Vector2 footprint,
            float maxHeight,
            Quaternion rotation)
        {
            var instance = InstantiatePrepared(prefab, name, parent, rotation);
            if (instance == null)
            {
                return null;
            }

            if (!TryGetBounds(instance, out var bounds))
            {
                instance.transform.localPosition = center;
                return instance;
            }

            var scaleX = footprint.x / Mathf.Max(bounds.size.x, 0.001f);
            var scaleZ = footprint.y / Mathf.Max(bounds.size.z, 0.001f);
            var scaleY = maxHeight > 0f ? Mathf.Min(1f, maxHeight / Mathf.Max(bounds.size.y, 0.001f)) : 1f;
            instance.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            AlignBoundsTopTo(instance, center);
            return instance;
        }

        private static GameObject InstantiatePrepared(GameObject prefab, string name, Transform parent, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = rotation;
            instance.transform.localScale = Vector3.one;
            DisableSceneInteraction(instance);
            return instance;
        }

        private static float CalculateUniformScale(Bounds bounds, Vector2 maxFootprint, float maxHeight)
        {
            var xScale = maxFootprint.x / Mathf.Max(bounds.size.x, 0.001f);
            var zScale = maxFootprint.y / Mathf.Max(bounds.size.z, 0.001f);
            var scale = Mathf.Min(xScale, zScale);
            if (maxHeight > 0f)
            {
                scale = Mathf.Min(scale, maxHeight / Mathf.Max(bounds.size.y, 0.001f));
            }

            return Mathf.Max(scale, 0.001f);
        }

        private static void AlignBoundsToGround(GameObject instance, Vector3 groundCenter)
        {
            if (!TryGetBounds(instance, out var bounds))
            {
                return;
            }

            var offset = groundCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            instance.transform.position += offset;
        }

        private static void AlignBoundsTopTo(GameObject instance, Vector3 center)
        {
            if (!TryGetBounds(instance, out var bounds))
            {
                return;
            }

            var offset = center - new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            instance.transform.position += offset;
        }

        private static bool TryGetBounds(GameObject instance, out Bounds bounds)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            bounds = default;
            var hasBounds = false;
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }

        private static void DisableSceneInteraction(GameObject instance)
        {
            var colliders = instance.GetComponentsInChildren<Collider>();
            for (var index = 0; index < colliders.Length; index++)
            {
                Object.Destroy(colliders[index]);
            }

            var renderers = instance.GetComponentsInChildren<Renderer>();
            for (var index = 0; index < renderers.Length; index++)
            {
                renderers[index].shadowCastingMode = ShadowCastingMode.Off;
                renderers[index].receiveShadows = false;
            }
        }
    }
}
