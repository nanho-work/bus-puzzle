using UnityEngine;

namespace BusPuzzle
{
    internal static class PassengerModelBuilder
    {
        private const float PassengerVisualScale = 1.26f;

        public static PassengerModel Create(PuzzleColor color, Transform parent)
        {
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
