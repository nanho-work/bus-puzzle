using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    internal static class PassengerModelBuilder
    {
        private const float PassengerVisualScale = 4.08f;
        private const float PassengerSetSpacingScale = 4.08f;
        private const string PassengerModelResourcePath = "PassengerModels/PassengerUnit";
        private const float NpcTargetHeight = 0.405f;
        private const float NpcTargetWidth = 0.115f;
        private const float NpcTargetDepth = 0.105f;

        private static GameObject passengerModelPrefab;
        private static bool passengerModelPrefabLoaded;
        private static bool passengerModelPrefabInvalid;

        public static PassengerModel Create(PuzzleColor color, Transform parent)
        {
            if (TryCreateAssetModel(color, parent, out var assetModel))
            {
                return assetModel;
            }

            var bodyMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Unit");
            var headMaterial = bodyMaterial;
            var legMaterial = PuzzlePalette.CreateSolidMaterial("Passenger Legs", PuzzlePalette.Darken(PuzzlePalette.ToColor(color), 0.18f));
            var offsets = new[]
            {
                new Vector3(0f, 0f, -0.155f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, -0.052f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, 0.052f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, 0.155f * PassengerSetSpacingScale)
            };

            var personRoots = new Transform[offsets.Length];
            var leftLegs = new Transform[offsets.Length];
            var rightLegs = new Transform[offsets.Length];

            for (var index = 0; index < offsets.Length; index++)
            {
                personRoots[index] = CreatePerson(index, parent, offsets[index], bodyMaterial, headMaterial, legMaterial, leftLegs, rightLegs);
            }

            return new PassengerModel(personRoots, offsets, leftLegs, rightLegs);
        }

        private static bool TryCreateAssetModel(PuzzleColor color, Transform parent, out PassengerModel model)
        {
            model = null;
            if (passengerModelPrefabInvalid)
            {
                return false;
            }

            var prefab = GetPassengerModelPrefab();
            if (prefab == null)
            {
                return false;
            }

            if (prefab.GetComponentInChildren<PassengerModelRig>(true) == null)
            {
                if (TryCreateNpcModelFromPrefab(prefab, color, parent, out model))
                {
                    return true;
                }

                passengerModelPrefabInvalid = true;
                Debug.LogWarning("Passenger prefab is missing a valid PassengerModelRig or NPC renderer setup. Falling back to generated passengers.");
                model = null;
                return false;
            }

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = "Passenger Unit Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var rig = instance.GetComponentInChildren<PassengerModelRig>(true);
            if (rig != null && rig.TryCreateModel(color, out model))
            {
                return true;
            }

            passengerModelPrefabInvalid = true;
            Debug.LogWarning("Passenger prefab is missing a valid PassengerModelRig or NPC renderer setup. Falling back to generated passengers.");
            DestroyModelInstance(instance);
            model = null;
            return false;
        }

        private static GameObject GetPassengerModelPrefab()
        {
            if (!passengerModelPrefabLoaded)
            {
                var prefabLibrary = PassengerPrefabLibrary.Load();
                if (prefabLibrary != null && prefabLibrary.TryGetPassengerPrefab(out var prefab))
                {
                    passengerModelPrefab = prefab;
                }
                else
                {
                    passengerModelPrefab = Resources.Load<GameObject>(PassengerModelResourcePath);
                }

                passengerModelPrefabLoaded = true;
            }

            return passengerModelPrefab;
        }

        private static bool TryCreateNpcModelFromPrefab(GameObject prefab, PuzzleColor color, Transform parent, out PassengerModel model)
        {
            model = null;
            if (prefab == null)
            {
                return false;
            }

            var offsets = new[]
            {
                new Vector3(0f, 0f, -0.155f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, -0.052f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, 0.052f * PassengerSetSpacingScale),
                new Vector3(0f, 0f, 0.155f * PassengerSetSpacingScale)
            };

            var personRoots = new Transform[offsets.Length];
            var leftLegs = new Transform[offsets.Length];
            var rightLegs = new Transform[offsets.Length];
            var hatMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Hat");

            for (var index = 0; index < offsets.Length; index++)
            {
                var personRoot = new GameObject($"Person {index + 1}").transform;
                personRoot.SetParent(parent, false);
                personRoot.localPosition = offsets[index];

                GroundShadowBuilder.CreatePassengerShadow(
                    personRoot,
                    new Vector3(0f, 0.006f * PassengerVisualScale, 0.002f * PassengerVisualScale),
                    0.090f * PassengerVisualScale,
                    0.056f * PassengerVisualScale);

                var instance = Object.Instantiate(prefab, personRoot, false);
                instance.name = "NPC Passenger";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                DisableRuntimeComponents(instance);
                ConfigureNpcRenderers(instance, hatMaterial);
                NormalizeNpcBounds(personRoot, instance.transform);
                leftLegs[index] = FindNpcLeg(instance.transform, true);
                rightLegs[index] = FindNpcLeg(instance.transform, false);
                personRoots[index] = personRoot;
            }

            model = new PassengerModel(
                personRoots,
                offsets,
                leftLegs,
                rightLegs,
                true);
            return true;
        }

        private static Transform FindNpcLeg(Transform root, bool left)
        {
            var sideToken = left ? ".L" : ".R";
            return FindNamedChild(root, $"thigh{sideToken}") ??
                FindNamedChild(root, $"shin{sideToken}") ??
                FindNamedChild(root, $"foot{sideToken}");
        }

        private static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < children.Length; index++)
            {
                if (children[index] != null && string.Equals(children[index].name, childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return children[index];
                }
            }

            return null;
        }

        private static void DisableRuntimeComponents(GameObject instance)
        {
            var animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = false;
                animator.applyRootMotion = false;
            }

            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (var index = 0; index < colliders.Length; index++)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(colliders[index]);
                }
                else
                {
                    Object.DestroyImmediate(colliders[index]);
                }
            }
        }

        private static void ConfigureNpcRenderers(GameObject instance, Material hatMaterial)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                if (hatMaterial != null && IsHatRenderer(renderer))
                {
                    renderer.sharedMaterial = hatMaterial;
                }
            }
        }

        private static bool IsHatRenderer(Renderer renderer)
        {
            var current = renderer != null ? renderer.transform : null;
            while (current != null)
            {
                if (current.name.IndexOf("Hat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void NormalizeNpcBounds(Transform personRoot, Transform instance)
        {
            if (!TryCalculateLocalRendererBounds(personRoot, out var bounds))
            {
                return;
            }

            var targetHeight = NpcTargetHeight * PassengerVisualScale;
            var targetWidth = NpcTargetWidth * PassengerVisualScale;
            var targetDepth = NpcTargetDepth * PassengerVisualScale;
            var widthScale = targetWidth / Mathf.Max(0.001f, bounds.size.x);
            var heightScale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            var depthScale = targetDepth / Mathf.Max(0.001f, bounds.size.z);
            var scale = Mathf.Min(widthScale, heightScale, depthScale);
            instance.localScale *= scale;

            if (!TryCalculateLocalRendererBounds(personRoot, out bounds))
            {
                return;
            }

            instance.localPosition += new Vector3(
                -bounds.center.x,
                -bounds.min.y,
                -bounds.center.z);
        }

        private static bool TryCalculateLocalRendererBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;

            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer == null || renderer.name.IndexOf("Shadow", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var worldBounds = renderer.bounds;
                EncapsulateLocalPoint(root, worldBounds.min, ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, worldBounds.max, ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z), ref bounds, ref hasBounds);
                EncapsulateLocalPoint(root, new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z), ref bounds, ref hasBounds);
            }

            return hasBounds;
        }

        private static void EncapsulateLocalPoint(Transform root, Vector3 worldPoint, ref Bounds bounds, ref bool hasBounds)
        {
            var localPoint = root.InverseTransformPoint(worldPoint);
            if (!hasBounds)
            {
                bounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(localPoint);
        }

        private static void DestroyModelInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(instance);
            }
            else
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static Transform CreatePerson(
            int index,
            Transform parent,
            Vector3 rootPosition,
            Material bodyMaterial,
            Material headMaterial,
            Material legMaterial,
            Transform[] leftLegs,
            Transform[] rightLegs)
        {
            var personRoot = new GameObject($"Person {index + 1}").transform;
            personRoot.SetParent(parent, false);
            personRoot.localPosition = rootPosition;

            GroundShadowBuilder.CreatePassengerShadow(
                personRoot,
                new Vector3(0f, 0.006f * PassengerVisualScale, 0.002f * PassengerVisualScale),
                0.092f * PassengerVisualScale,
                0.058f * PassengerVisualScale);

            var body = VisualPrimitiveFactory.Create(PrimitiveType.Capsule, "Body");
            body.transform.SetParent(personRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.155f * PassengerVisualScale, 0f);
            body.transform.localScale = new Vector3(0.092f, 0.105f, 0.092f) * PassengerVisualScale;
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var head = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, "Head");
            head.transform.SetParent(personRoot, false);
            head.transform.localPosition = new Vector3(0f, 0.318f * PassengerVisualScale, 0.012f * PassengerVisualScale);
            head.transform.localScale = new Vector3(0.096f, 0.096f, 0.096f) * PassengerVisualScale;
            head.GetComponent<Renderer>().sharedMaterial = headMaterial;

            leftLegs[index] = CreateLeg(personRoot, "Left Leg", new Vector3(-0.026f, 0.055f, 0.016f) * PassengerVisualScale, legMaterial);
            rightLegs[index] = CreateLeg(personRoot, "Right Leg", new Vector3(0.026f, 0.055f, 0.016f) * PassengerVisualScale, legMaterial);
            return personRoot;
        }

        private static Transform CreateLeg(Transform parent, string name, Vector3 localPosition, Material material)
        {
            var legRoot = new GameObject(name).transform;
            legRoot.SetParent(parent, false);
            legRoot.localPosition = localPosition;

            var leg = VisualPrimitiveFactory.Create(PrimitiveType.Cube, "Leg Mesh");
            leg.transform.SetParent(legRoot, false);
            leg.transform.localPosition = new Vector3(0f, -0.035f * PassengerVisualScale, 0f);
            leg.transform.localScale = new Vector3(0.024f, 0.070f, 0.026f) * PassengerVisualScale;
            leg.GetComponent<Renderer>().sharedMaterial = material;
            return legRoot;
        }
    }
}
