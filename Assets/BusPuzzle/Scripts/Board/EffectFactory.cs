using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    internal static class EffectFactory
    {
        private static Material sparkMaterial;
        private static Material dustMaterial;
        private static Material drivingDustMaterial;
        private static Material speedLineMaterial;
        private static readonly Dictionary<PuzzleColor, Material> AbsorbMaterials = new Dictionary<PuzzleColor, Material>();

        public static void PlayCollisionSpark(Vector3 position, Vector3 impactDirection, float cellSize)
        {
            var safeCellSize = Mathf.Max(0.1f, cellSize);
            var direction = NormalizeFlat(impactDirection, Vector3.forward);
            position.y += safeCellSize * 0.34f;

            CreateSparkBits(position, direction, safeCellSize);
            CreateSparkStar(position + direction * (safeCellSize * 0.05f), direction, safeCellSize);
        }

        public static void PlayBoardingAbsorb(Vector3 position, PuzzleColor color, float cellSize)
        {
            var safeCellSize = Mathf.Max(0.1f, cellSize);
            var effectPosition = position + Vector3.up * (safeCellSize * 0.10f);
            var material = GetAbsorbMaterial(color);

            for (var index = 0; index < 12; index++)
            {
                var angle = index / 12f * Mathf.PI * 2f;
                var radius = safeCellSize * (0.07f + (index % 3) * 0.025f);
                var start = effectPosition + new Vector3(Mathf.Cos(angle) * radius, safeCellSize * (0.015f + (index % 2) * 0.018f), Mathf.Sin(angle) * radius);
                var target = effectPosition + Vector3.up * safeCellSize * (0.05f + (index % 4) * 0.015f);
                var scale = Vector3.one * safeCellSize * (0.052f + (index % 3) * 0.010f);
                var duration = 0.20f + (index % 4) * 0.025f;
                CreateMovingSphere("Boarding Absorb Dot", start, target, scale, Vector3.zero, material, duration);
            }
        }

        public static void PlayDepartureTrail(Vector3 rearPosition, Vector3 backwardDirection, PuzzleColor color, float cellSize)
        {
            var safeCellSize = Mathf.Max(0.1f, cellSize);
            var backward = NormalizeFlat(backwardDirection, Vector3.back);
            var right = Vector3.Cross(Vector3.up, backward).normalized;
            var dustPosition = rearPosition + backward * (safeCellSize * 0.16f) + Vector3.up * (safeCellSize * 0.045f);

            CreateDepartureDust(dustPosition, backward, PuzzlePalette.ToColor(color), safeCellSize);

            for (var index = 0; index < 4; index++)
            {
                var lateral = (index - 1.5f) * safeCellSize * 0.13f;
                var depth = safeCellSize * (0.12f + index * 0.08f);
                var length = safeCellSize * (0.24f + index * 0.045f);
                var linePosition = rearPosition + right * lateral + backward * depth + Vector3.up * (safeCellSize * 0.055f);
                CreateSpeedLine(linePosition, backward, length, safeCellSize * 0.018f, 0.24f + index * 0.025f);
            }
        }

        public static void PlayDrivingTrail(Vector3 rearPosition, Vector3 backwardDirection, float cellSize)
        {
            var safeCellSize = Mathf.Max(0.1f, cellSize);
            var backward = NormalizeFlat(backwardDirection, Vector3.back);
            var right = Vector3.Cross(Vector3.up, backward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            var dustPosition = rearPosition + backward * (safeCellSize * 0.13f) + Vector3.up * (safeCellSize * 0.034f);
            for (var index = 0; index < 5; index++)
            {
                var lateral = (index - 2f) * safeCellSize * 0.050f;
                var start = dustPosition + right * lateral + backward * (safeCellSize * 0.026f * index);
                var target = start + backward * safeCellSize * (0.105f + index * 0.020f) + Vector3.up * safeCellSize * 0.018f;
                var scale = Vector3.one * safeCellSize * (0.062f + index * 0.007f);
                var endScale = Vector3.one * safeCellSize * (0.116f + index * 0.010f);
                CreateMovingSphere("Driving Dust Puff", start, target, scale, endScale, GetDrivingDustMaterial(), 0.24f + index * 0.018f);
            }
        }

        private static void CreateSparkBits(Vector3 position, Vector3 direction, float cellSize)
        {
            var right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            for (var index = 0; index < 10; index++)
            {
                var spread = Mathf.Lerp(-0.68f, 0.68f, index / 9f);
                var sparkDirection = (direction + right * spread + Vector3.up * (0.18f + (index % 3) * 0.05f)).normalized;
                var start = position + right * ((index - 4.5f) * cellSize * 0.010f);
                var target = start + sparkDirection * cellSize * (0.12f + (index % 4) * 0.035f);
                var scale = new Vector3(cellSize * 0.030f, cellSize * 0.030f, cellSize * (0.10f + (index % 3) * 0.025f));
                var rotation = Quaternion.LookRotation(sparkDirection, Vector3.up);
                var spark = CreateEffectCube("Collision Spark Bit", start, rotation, scale, GetSparkMaterial());
                spark.AddComponent<EffectMotion>().Configure(start, target, scale, Vector3.zero, 0.13f + (index % 4) * 0.020f, 0f);
            }
        }

        private static void CreateDepartureDust(Vector3 position, Vector3 backwardDirection, Color bodyColor, float cellSize)
        {
            var backward = NormalizeFlat(backwardDirection, Vector3.back);
            var right = Vector3.Cross(Vector3.up, backward).normalized;
            if (right.sqrMagnitude < 0.0001f)
            {
                right = Vector3.right;
            }

            for (var index = 0; index < 8; index++)
            {
                var lateral = (index - 3.5f) * cellSize * 0.050f;
                var depth = (index % 4) * cellSize * 0.035f;
                var start = position + right * lateral + backward * depth;
                var target = start + backward * cellSize * (0.10f + (index % 3) * 0.025f) + Vector3.up * cellSize * 0.025f;
                var scale = Vector3.one * cellSize * (0.070f + (index % 3) * 0.015f);
                var endScale = Vector3.one * cellSize * (0.13f + (index % 4) * 0.018f);
                CreateMovingSphere("Departure Dust Puff", start, target, scale, endScale, GetDustMaterial(bodyColor), 0.22f + (index % 4) * 0.025f);
            }
        }

        private static void CreateSparkStar(Vector3 position, Vector3 impactDirection, float cellSize)
        {
            var star = new GameObject("Collision Star");
            star.transform.position = position;
            star.transform.rotation = Quaternion.LookRotation(Vector3.down, NormalizeFlat(impactDirection, Vector3.forward));
            star.transform.localScale = Vector3.one * (cellSize * 0.16f);

            var meshFilter = star.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateStarMesh();

            var meshRenderer = star.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetSparkMaterial();

            star.AddComponent<EffectMotion>().Configure(
                star.transform.position,
                star.transform.position + Vector3.up * (cellSize * 0.03f),
                star.transform.localScale,
                Vector3.zero,
                0.18f,
                220f);
        }

        private static void CreateSpeedLine(Vector3 position, Vector3 direction, float length, float width, float lifetime)
        {
            var line = CreateEffectCube(
                "Departure Speed Line",
                position,
                Quaternion.LookRotation(NormalizeFlat(direction, Vector3.back), Vector3.up),
                new Vector3(width, width, length),
                GetSpeedLineMaterial());

            line.AddComponent<EffectMotion>().Configure(
                line.transform.position,
                line.transform.position + NormalizeFlat(direction, Vector3.back) * (length * 0.45f),
                line.transform.localScale,
                Vector3.zero,
                lifetime,
                0f);
        }

        private static GameObject CreateEffectCube(string name, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var effect = VisualPrimitiveFactory.Create(PrimitiveType.Cube, name);
            effect.transform.SetPositionAndRotation(position, rotation);
            effect.transform.localScale = scale;
            DisableCollider(effect);

            var renderer = effect.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return effect;
        }

        private static void CreateMovingSphere(
            string name,
            Vector3 start,
            Vector3 target,
            Vector3 scale,
            Vector3 endScale,
            Material material,
            float duration)
        {
            var sphere = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, name);
            sphere.transform.position = start;
            sphere.transform.localScale = scale;
            DisableCollider(sphere);

            var renderer = sphere.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            sphere.AddComponent<EffectMotion>().Configure(start, target, scale, endScale, duration, 0f);
        }

        private static void DisableCollider(GameObject effect)
        {
            var collider = effect.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Object.Destroy(collider);
            }
        }

        private static Mesh CreateStarMesh()
        {
            const int points = 8;
            var vertices = new Vector3[points + 1];
            var triangles = new int[points * 3];
            vertices[0] = Vector3.zero;

            for (var index = 0; index < points; index++)
            {
                var angle = index / (float)points * Mathf.PI * 2f;
                var radius = index % 2 == 0 ? 1f : 0.42f;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = index == points - 1 ? 1 : index + 2;
                triangles[index * 3 + 2] = index + 1;
            }

            var mesh = new Mesh
            {
                name = "Effect Star Mesh",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material GetSparkMaterial()
        {
            if (sparkMaterial == null)
            {
                sparkMaterial = PuzzlePalette.CreateSolidMaterial("Effect Spark", new Color(1f, 0.86f, 0.12f));
            }

            return sparkMaterial;
        }

        private static Material GetAbsorbMaterial(PuzzleColor color)
        {
            if (!AbsorbMaterials.TryGetValue(color, out var material))
            {
                material = PuzzlePalette.CreateSolidMaterial($"Effect Absorb {PuzzlePalette.DisplayName(color)}", Color.Lerp(PuzzlePalette.ToColor(color), Color.white, 0.16f));
                AbsorbMaterials.Add(color, material);
            }

            return material;
        }

        private static Material GetDustMaterial(Color bodyColor)
        {
            if (dustMaterial == null)
            {
                dustMaterial = PuzzlePalette.CreateSolidMaterial("Effect Dust", Color.Lerp(new Color(0.78f, 0.82f, 0.84f), bodyColor, 0.08f));
            }

            return dustMaterial;
        }

        private static Material GetDrivingDustMaterial()
        {
            if (drivingDustMaterial == null)
            {
                drivingDustMaterial = PuzzlePalette.CreateSolidMaterial("Effect Driving Dust", new Color(0.86f, 0.90f, 0.92f));
            }

            return drivingDustMaterial;
        }

        private static Material GetSpeedLineMaterial()
        {
            if (speedLineMaterial == null)
            {
                speedLineMaterial = PuzzlePalette.CreateSolidMaterial("Effect Speed Line", new Color(0.88f, 0.94f, 0.98f));
            }

            return speedLineMaterial;
        }

        private static Vector3 NormalizeFlat(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = fallback;
            }

            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.back;
        }
    }

    internal sealed class EffectMotion : MonoBehaviour
    {
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3 startScale;
        private Vector3 targetScale;
        private float lifetime = 0.2f;
        private float spinDegrees;
        private float elapsed;

        public void Configure(Vector3 start, Vector3 target, Vector3 scale, Vector3 endScale, float duration, float spin)
        {
            startPosition = start;
            targetPosition = target;
            startScale = scale;
            targetScale = endScale;
            lifetime = Mathf.Max(0.01f, duration);
            spinDegrees = spin;
            elapsed = 0f;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / lifetime);
            var eased = 1f - Mathf.Pow(1f - t, 2f);

            transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            transform.localScale = Vector3.Lerp(startScale, targetScale, eased);

            if (Mathf.Abs(spinDegrees) > 0.001f)
            {
                transform.Rotate(Vector3.up, spinDegrees * Time.deltaTime, Space.World);
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
