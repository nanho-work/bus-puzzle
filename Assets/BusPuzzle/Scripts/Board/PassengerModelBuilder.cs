using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerModelBuilder
    {
        private const float PassengerVisualScale = 1.26f;
        private const string PassengerModelResourcePath = "PassengerModels/PassengerUnit";

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
                new Vector3(0f, 0f, -0.155f * PassengerVisualScale),
                new Vector3(0f, 0f, -0.052f * PassengerVisualScale),
                new Vector3(0f, 0f, 0.052f * PassengerVisualScale),
                new Vector3(0f, 0f, 0.155f * PassengerVisualScale)
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
            Debug.LogWarning(
                $"Passenger model prefab at Resources/{PassengerModelResourcePath} is missing a valid PassengerModelRig. Falling back to generated passengers.");
            DestroyModelInstance(instance);
            model = null;
            return false;
        }

        private static GameObject GetPassengerModelPrefab()
        {
            if (!passengerModelPrefabLoaded)
            {
                passengerModelPrefab = Resources.Load<GameObject>(PassengerModelResourcePath);
                passengerModelPrefabLoaded = true;
            }

            return passengerModelPrefab;
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
                0.145f * PassengerVisualScale,
                0.095f * PassengerVisualScale);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(personRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.155f * PassengerVisualScale, 0f);
            body.transform.localScale = new Vector3(0.092f, 0.105f, 0.092f) * PassengerVisualScale;
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
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

            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg Mesh";
            leg.transform.SetParent(legRoot, false);
            leg.transform.localPosition = new Vector3(0f, -0.035f * PassengerVisualScale, 0f);
            leg.transform.localScale = new Vector3(0.024f, 0.070f, 0.026f) * PassengerVisualScale;
            leg.GetComponent<Renderer>().sharedMaterial = material;
            return legRoot;
        }
    }
}
