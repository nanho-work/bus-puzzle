using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class PassengerModelRig : MonoBehaviour
    {
        [SerializeField] private Transform[] personRoots = Array.Empty<Transform>();
        [SerializeField] private Transform[] leftLegs = Array.Empty<Transform>();
        [SerializeField] private Transform[] rightLegs = Array.Empty<Transform>();
        [SerializeField] private Renderer[] colorRenderers = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] legRenderers = Array.Empty<Renderer>();
        [SerializeField] private bool colorAllRenderersWhenEmpty = true;
        [SerializeField] private bool addGeneratedGroundShadows = true;

        internal bool TryCreateModel(PuzzleColor color, out PassengerModel model)
        {
            model = null;

            var resolvedPersonRoots = ResolvePersonRoots();
            if (resolvedPersonRoots.Length == 0)
            {
                return false;
            }

            ApplyPalette(color);
            if (addGeneratedGroundShadows)
            {
                AddGroundShadows(resolvedPersonRoots);
            }

            var defaultPositions = new Vector3[resolvedPersonRoots.Length];
            for (var index = 0; index < resolvedPersonRoots.Length; index++)
            {
                defaultPositions[index] = resolvedPersonRoots[index].localPosition;
            }

            model = new PassengerModel(
                resolvedPersonRoots,
                defaultPositions,
                NormalizeArray(leftLegs, resolvedPersonRoots.Length),
                NormalizeArray(rightLegs, resolvedPersonRoots.Length));
            return true;
        }

        private Transform[] ResolvePersonRoots()
        {
            if (HasAnyAssigned(personRoots))
            {
                return Compact(personRoots);
            }

            var candidates = new List<Transform>();
            var children = GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < children.Length; index++)
            {
                var child = children[index];
                if (child == null || child == transform || !child.name.StartsWith("Person", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                candidates.Add(child);
            }

            candidates.Sort((first, second) => string.CompareOrdinal(first.name, second.name));
            return candidates.ToArray();
        }

        private void ApplyPalette(PuzzleColor color)
        {
            var bodyMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Unit Asset");
            var legMaterial = PuzzlePalette.CreateSolidMaterial(
                "Passenger Asset Legs",
                PuzzlePalette.Darken(PuzzlePalette.ToColor(color), 0.18f));

            if (HasAnyAssigned(colorRenderers))
            {
                ApplyMaterial(colorRenderers, bodyMaterial);
            }
            else if (colorAllRenderersWhenEmpty)
            {
                ApplyMaterial(GetDefaultColorRenderers(), bodyMaterial);
            }

            if (HasAnyAssigned(legRenderers))
            {
                ApplyMaterial(legRenderers, legMaterial);
            }
        }

        private static void ApplyMaterial(Renderer[] renderers, Material material)
        {
            if (renderers == null || material == null)
            {
                return;
            }

            for (var index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].sharedMaterial = material;
                }
            }
        }

        private static void AddGroundShadows(Transform[] roots)
        {
            for (var index = 0; index < roots.Length; index++)
            {
                if (roots[index] == null)
                {
                    continue;
                }

                if (HasGeneratedShadow(roots[index]))
                {
                    continue;
                }

                GroundShadowBuilder.CreatePassengerShadow(
                    roots[index],
                    new Vector3(0f, 0.006f * PassengerUnitLayout.VisualScale, 0.002f * PassengerUnitLayout.VisualScale),
                    0.092f * PassengerUnitLayout.VisualScale,
                    0.058f * PassengerUnitLayout.VisualScale);
            }
        }

        private Renderer[] GetDefaultColorRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            var result = new List<Renderer>();
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer != null && !IsShadowRenderer(renderer))
                {
                    result.Add(renderer);
                }
            }

            return result.ToArray();
        }

        private static bool IsShadowRenderer(Renderer renderer)
        {
            var current = renderer.transform;
            while (current != null)
            {
                if (current.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool HasGeneratedShadow(Transform root)
        {
            for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                var child = root.GetChild(childIndex);
                if (child != null && child.name.IndexOf("Passenger Ground Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform[] NormalizeArray(Transform[] source, int length)
        {
            var result = new Transform[Mathf.Max(0, length)];
            if (source == null)
            {
                return result;
            }

            for (var index = 0; index < result.Length && index < source.Length; index++)
            {
                result[index] = source[index];
            }

            return result;
        }

        private static bool HasAnyAssigned<T>(T[] values) where T : UnityEngine.Object
        {
            if (values == null)
            {
                return false;
            }

            for (var index = 0; index < values.Length; index++)
            {
                if (values[index] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform[] Compact(Transform[] source)
        {
            var result = new List<Transform>();
            if (source == null)
            {
                return result.ToArray();
            }

            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] != null)
                {
                    result.Add(source[index]);
                }
            }

            return result.ToArray();
        }
    }
}
