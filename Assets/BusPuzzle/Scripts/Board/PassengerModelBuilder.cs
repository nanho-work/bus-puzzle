using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BusPuzzle
{
    internal static class PassengerModelBuilder
    {
        private const float NekoArmRestAngleDegrees = 58f;
        private const string PassengerModelResourcePath = "PassengerModels/PassengerUnit";
        private const float NpcTargetHeight = 0.405f;
        private const float NpcTargetWidth = 0.115f;
        private const float NpcTargetDepth = 0.105f;
        private const float BodyOnlyScaleMultiplier = 1.2f;

        private static GameObject passengerModelPrefab;
        private static bool passengerModelPrefabLoaded;
        private static bool passengerModelPrefabInvalid;
        private static readonly Dictionary<string, Material> NekoFurMaterials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> NekoFurTextures = new Dictionary<string, Texture2D>();

        public static PassengerModel Create(PuzzleColor color, Transform parent)
        {
            if (ShouldUseAssetPassengerPrefabs() && TryCreateAssetModel(color, parent, out var assetModel))
            {
                return assetModel;
            }

            var bodyMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Unit");
            var headMaterial = bodyMaterial;
            var legMaterial = PuzzlePalette.CreateSolidMaterial("Passenger Legs", PuzzlePalette.Darken(PuzzlePalette.ToColor(color), 0.18f));
            var offsets = PassengerUnitLayout.CreateDefaultPersonLocalPositions();

            var personRoots = new Transform[offsets.Length];
            var leftLegs = new Transform[offsets.Length];
            var rightLegs = new Transform[offsets.Length];
            var leftFeet = new Transform[offsets.Length];
            var rightFeet = new Transform[offsets.Length];
            var leftArms = new Transform[offsets.Length];
            var rightArms = new Transform[offsets.Length];

            for (var index = 0; index < offsets.Length; index++)
            {
                personRoots[index] = CreatePerson(
                    index,
                    parent,
                    offsets[index],
                    bodyMaterial,
                    headMaterial,
                    legMaterial,
                    leftLegs,
                    rightLegs,
                    leftFeet,
                    rightFeet,
                    leftArms,
                    rightArms);
            }

            return new PassengerModel(
                personRoots,
                offsets,
                leftLegs,
                rightLegs,
                true,
                null,
                null,
                leftFeet,
                rightFeet,
                leftArms,
                rightArms);
        }

        private static bool ShouldUseAssetPassengerPrefabs()
        {
            return PassengerUnitLayout.UseAssetPassengerPrefabs;
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

            var offsets = PassengerUnitLayout.CreateDefaultPersonLocalPositions();

            var personRoots = new Transform[offsets.Length];
            var leftLegs = new Transform[offsets.Length];
            var rightLegs = new Transform[offsets.Length];
            var leftLowerLegs = new Transform[offsets.Length];
            var rightLowerLegs = new Transform[offsets.Length];
            var leftFeet = new Transform[offsets.Length];
            var rightFeet = new Transform[offsets.Length];
            var leftArms = new Transform[offsets.Length];
            var rightArms = new Transform[offsets.Length];
            var bodyMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Asset Body");
            var legMaterial = PuzzlePalette.CreateSolidMaterial(
                "Passenger Asset Legs",
                PuzzlePalette.Darken(PuzzlePalette.ToColor(color), 0.18f));
            var hatMaterial = PuzzlePalette.CreateMaterial(color, "Passenger Hat");
            var useNekoWalkRig = false;

            for (var index = 0; index < offsets.Length; index++)
            {
                var personRoot = new GameObject($"Person {index + 1}").transform;
                personRoot.SetParent(parent, false);
                personRoot.localPosition = offsets[index];

                GroundShadowBuilder.CreatePassengerShadow(
                    personRoot,
                    new Vector3(0f, 0.006f * PassengerUnitLayout.VisualScale, 0.002f * PassengerUnitLayout.VisualScale),
                    0.090f * PassengerUnitLayout.VisualScale,
                    0.056f * PassengerUnitLayout.VisualScale);

                var instance = Object.Instantiate(prefab, personRoot, false);
                instance.name = "NPC Passenger";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = GetAssetPassengerPrefabRotation(prefab);
                instance.transform.localScale = Vector3.one;

                DisableRuntimeComponents(instance);
                ConfigureNpcRenderers(instance, color, bodyMaterial, legMaterial, hatMaterial);
                NormalizeNpcBounds(personRoot, instance.transform);
                ApplyNpcBodyOnlyScale(instance.transform);
                leftLegs[index] = FindNpcUpperLeg(instance.transform, true);
                rightLegs[index] = FindNpcUpperLeg(instance.transform, false);
                leftLowerLegs[index] = FindNpcLowerLeg(instance.transform, true);
                rightLowerLegs[index] = FindNpcLowerLeg(instance.transform, false);
                leftFeet[index] = FindNpcFoot(instance.transform, true);
                rightFeet[index] = FindNpcFoot(instance.transform, false);
                leftArms[index] = FindNpcUpperArm(instance.transform, true);
                rightArms[index] = FindNpcUpperArm(instance.transform, false);
                if (HasNekoArmRig(instance.transform))
                {
                    ApplyNekoArmRestPose(leftArms[index], rightArms[index]);
                }

                useNekoWalkRig = useNekoWalkRig || HasNekoLegRig(instance.transform);
                personRoots[index] = personRoot;
            }

            model = new PassengerModel(
                personRoots,
                offsets,
                leftLegs,
                rightLegs,
                !useNekoWalkRig,
                leftLowerLegs,
                rightLowerLegs,
                leftFeet,
                rightFeet,
                leftArms,
                rightArms);
            return true;
        }

        private static Transform FindNpcUpperLeg(Transform root, bool left)
        {
            if (left)
            {
                return FindNamedChild(root, "thigh.L") ??
                    FindNamedChild(root, "RigLLeg1") ??
                    FindNamedChild(root, "shin.L") ??
                    FindNamedChild(root, "foot.L");
            }

            return FindNamedChild(root, "thigh.R") ??
                FindNamedChild(root, "RigRLeg1") ??
                FindNamedChild(root, "shin.R") ??
                FindNamedChild(root, "foot.R");
        }

        private static Transform FindNpcLowerLeg(Transform root, bool left)
        {
            if (left)
            {
                return FindNamedChild(root, "shin.L") ??
                    FindNamedChild(root, "RigLLeg2");
            }

            return FindNamedChild(root, "shin.R") ??
                FindNamedChild(root, "RigRLeg2");
        }

        private static Transform FindNpcFoot(Transform root, bool left)
        {
            if (left)
            {
                return FindNamedChild(root, "foot.L") ??
                    FindNamedChild(root, "RigLLegAnkle") ??
                    FindNamedChild(root, "RigLLegFoot1");
            }

            return FindNamedChild(root, "foot.R") ??
                FindNamedChild(root, "RigRLegAnkle") ??
                FindNamedChild(root, "RigRLegFoot1");
        }

        private static Transform FindNpcUpperArm(Transform root, bool left)
        {
            if (left)
            {
                return FindNamedChild(root, "upper_arm.L") ??
                    FindNamedChild(root, "arm.L") ??
                    FindNamedChild(root, "RigLArm1");
            }

            return FindNamedChild(root, "upper_arm.R") ??
                FindNamedChild(root, "arm.R") ??
                FindNamedChild(root, "RigRArm1");
        }

        private static bool HasNekoLegRig(Transform root)
        {
            return FindNamedChild(root, "RigLLeg1") != null &&
                FindNamedChild(root, "RigRLeg1") != null;
        }

        private static bool HasNekoArmRig(Transform root)
        {
            return FindNamedChild(root, "RigLArm1") != null &&
                FindNamedChild(root, "RigRArm1") != null;
        }

        private static void ApplyNekoArmRestPose(Transform leftArm, Transform rightArm)
        {
            if (leftArm != null)
            {
                leftArm.localRotation *= Quaternion.Euler(0f, NekoArmRestAngleDegrees, 0f);
            }

            if (rightArm != null)
            {
                rightArm.localRotation *= Quaternion.Euler(0f, -NekoArmRestAngleDegrees, 0f);
            }
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
            var animators = instance.GetComponentsInChildren<Animator>(true);
            for (var index = 0; index < animators.Length; index++)
            {
                if (animators[index] == null)
                {
                    continue;
                }

                animators[index].enabled = false;
                animators[index].applyRootMotion = false;
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

        private static Quaternion GetAssetPassengerPrefabRotation(GameObject prefab)
        {
            return prefab != null && prefab.name == "PassengerUnit"
                ? Quaternion.Euler(-90f, 180f, 0f)
                : Quaternion.identity;
        }

        private static void ConfigureNpcRenderers(
            GameObject instance,
            PuzzleColor color,
            Material bodyMaterial,
            Material legMaterial,
            Material hatMaterial)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                if (TryApplyNekoFurRenderer(renderer, color))
                {
                    continue;
                }

                if (hatMaterial != null && IsHatRenderer(renderer))
                {
                    renderer.sharedMaterial = hatMaterial;
                    continue;
                }

                if (legMaterial != null && IsPassengerLegRenderer(renderer))
                {
                    renderer.sharedMaterial = legMaterial;
                    continue;
                }

                if (bodyMaterial != null && IsPassengerColorRenderer(renderer))
                {
                    renderer.sharedMaterial = bodyMaterial;
                }
            }
        }

        private static bool TryApplyNekoFurRenderer(Renderer renderer, PuzzleColor color)
        {
            if (renderer == null)
            {
                return false;
            }

            var materials = renderer.sharedMaterials;
            var changed = false;
            for (var index = 0; index < materials.Length; index++)
            {
                if (!IsNekoFurMaterial(materials[index]))
                {
                    continue;
                }

                materials[index] = GetNekoFurMaterial(materials[index], color);
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }

            return changed;
        }

        private static bool IsNekoFurMaterial(Material material)
        {
            if (material == null)
            {
                return false;
            }

            return material.name.IndexOf("Neko Cat", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                material.name.IndexOf("Face", System.StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static Material GetNekoFurMaterial(Material source, PuzzleColor color)
        {
            if (source == null)
            {
                return null;
            }

            var key = $"{source.GetInstanceID()}:{(int)color}";
            if (NekoFurMaterials.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var material = new Material(source)
            {
                name = $"{PuzzlePalette.DisplayName(color)} Neko Fur"
            };

            var sourceTexture = GetMainTexture(source);
            if (sourceTexture != null && sourceTexture.isReadable)
            {
                var texture = GetNekoFurTexture(sourceTexture, color);
                if (texture != null)
                {
                    SetMainTexture(material, texture);
                    SetMaterialColor(material, Color.white);
                }
                else
                {
                    SetMaterialColor(material, PuzzlePalette.ToColor(color));
                }
            }
            else
            {
                SetMaterialColor(material, PuzzlePalette.ToColor(color));
            }

            NekoFurMaterials.Add(key, material);
            return material;
        }

        private static Texture2D GetMainTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") is Texture2D baseMap)
            {
                return baseMap;
            }

            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") is Texture2D mainTex)
            {
                return mainTex;
            }

            return null;
        }

        private static void SetMainTexture(Material material, Texture texture)
        {
            if (material == null || texture == null)
            {
                return;
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }
        }

        private static Texture2D GetNekoFurTexture(Texture2D source, PuzzleColor color)
        {
            if (source == null)
            {
                return null;
            }

            var key = $"{source.GetInstanceID()}:{(int)color}";
            if (NekoFurTextures.TryGetValue(key, out var cached))
            {
                return cached;
            }

            Texture2D texture;
            try
            {
                var pixels = source.GetPixels32();
                for (var index = 0; index < pixels.Length; index++)
                {
                    var original = (Color)pixels[index];
                    if (ShouldRecolorNekoFur(original))
                    {
                        pixels[index] = RecolorNekoFurPixel(original, color);
                    }
                }

                texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true)
                {
                    name = $"{PuzzlePalette.DisplayName(color)} Neko Fur Texture",
                    filterMode = source.filterMode,
                    wrapMode = source.wrapMode,
                    anisoLevel = source.anisoLevel
                };
                texture.SetPixels32(pixels);
                texture.Apply(true, false);
            }
            catch (UnityException)
            {
                return null;
            }

            NekoFurTextures.Add(key, texture);
            return texture;
        }

        private static bool ShouldRecolorNekoFur(Color color)
        {
            if (color.a <= 0.05f)
            {
                return false;
            }

            Color.RGBToHSV(color, out _, out var saturation, out var value);
            var maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            var minChannel = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            var channelSpread = maxChannel - minChannel;

            return saturation <= 0.28f &&
                channelSpread <= 0.22f &&
                value >= 0.16f &&
                value <= 0.80f;
        }

        private static Color32 RecolorNekoFurPixel(Color original, PuzzleColor color)
        {
            Color.RGBToHSV(original, out _, out _, out var value);
            var target = PuzzlePalette.ToColor(color);
            var shade = Mathf.InverseLerp(0.16f, 0.80f, value);
            var shadeFactor = Mathf.Lerp(0.52f, 1.16f, shade);
            var recolored = new Color(
                Mathf.Clamp01(target.r * shadeFactor),
                Mathf.Clamp01(target.g * shadeFactor),
                Mathf.Clamp01(target.b * shadeFactor),
                original.a);

            return recolored;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
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

        private static bool IsPassengerLegRenderer(Renderer renderer)
        {
            return HasRendererToken(renderer, "Leg") ||
                HasRendererToken(renderer, "Foot") ||
                HasRendererToken(renderer, "PassengerLeg");
        }

        private static bool IsPassengerColorRenderer(Renderer renderer)
        {
            return HasRendererToken(renderer, "Body") ||
                HasRendererToken(renderer, "Head") ||
                HasRendererToken(renderer, "Arm") ||
                HasRendererToken(renderer, "Hand") ||
                HasRendererToken(renderer, "Shade");
        }

        private static bool HasRendererToken(Renderer renderer, string token)
        {
            if (renderer == null || string.IsNullOrEmpty(token))
            {
                return false;
            }

            var current = renderer.transform;
            while (current != null)
            {
                if (current.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                current = current.parent;
            }

            var materials = renderer.sharedMaterials;
            for (var index = 0; index < materials.Length; index++)
            {
                if (materials[index] != null &&
                    materials[index].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ApplyNpcBodyOnlyScale(Transform root)
        {
            if (root == null || Mathf.Approximately(BodyOnlyScaleMultiplier, 1f))
            {
                return;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < children.Length; index++)
            {
                var child = children[index];
                if (child == null ||
                    child == root ||
                    !IsNpcBodyScaleTarget(child) ||
                    HasNpcBodyScaleAncestor(child, root))
                {
                    continue;
                }

                child.localScale *= BodyOnlyScaleMultiplier;
            }
        }

        private static bool HasNpcBodyScaleAncestor(Transform transform, Transform stopAt)
        {
            var current = transform != null ? transform.parent : null;
            while (current != null && current != stopAt)
            {
                if (IsNpcBodyScaleTarget(current))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsNpcBodyScaleTarget(Transform transform)
        {
            if (transform == null)
            {
                return false;
            }

            return HasNameToken(transform, "Body") ||
                HasNameToken(transform, "Arm") ||
                HasNameToken(transform, "Hand") ||
                HasNameToken(transform, "Leg") ||
                HasNameToken(transform, "Foot") ||
                HasNameToken(transform, "thigh") ||
                HasNameToken(transform, "shin");
        }

        private static bool HasNameToken(Transform transform, string token)
        {
            return transform != null &&
                !string.IsNullOrEmpty(token) &&
                transform.name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void NormalizeNpcBounds(Transform personRoot, Transform instance)
        {
            if (!TryCalculateLocalRendererBounds(personRoot, out var bounds))
            {
                return;
            }

            var targetHeight = NpcTargetHeight * PassengerUnitLayout.VisualScale;
            var targetWidth = NpcTargetWidth * PassengerUnitLayout.VisualScale;
            var targetDepth = NpcTargetDepth * PassengerUnitLayout.VisualScale;
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
            Transform[] rightLegs,
            Transform[] leftFeet,
            Transform[] rightFeet,
            Transform[] leftArms,
            Transform[] rightArms)
        {
            var personRoot = new GameObject($"Person {index + 1}").transform;
            personRoot.SetParent(parent, false);
            personRoot.localPosition = rootPosition;

            GroundShadowBuilder.CreatePassengerShadow(
                personRoot,
                new Vector3(0f, 0.006f * PassengerUnitLayout.VisualScale, 0.002f * PassengerUnitLayout.VisualScale),
                0.092f * PassengerUnitLayout.VisualScale,
                0.058f * PassengerUnitLayout.VisualScale);

            var body = VisualPrimitiveFactory.Create(PrimitiveType.Capsule, "Body");
            body.transform.SetParent(personRoot, false);
            body.transform.localPosition = new Vector3(0f, 0.176f * PassengerUnitLayout.VisualScale, 0f);
            body.transform.localScale = new Vector3(0.0592f, 0.074f, 0.0592f) *
                PassengerUnitLayout.VisualScale *
                BodyOnlyScaleMultiplier;
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            var head = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, "Head");
            head.transform.SetParent(personRoot, false);
            head.transform.localPosition = new Vector3(0f, 0.296f * PassengerUnitLayout.VisualScale, 0.010f * PassengerUnitLayout.VisualScale);
            head.transform.localScale = new Vector3(0.0624f, 0.0624f, 0.0624f) * PassengerUnitLayout.VisualScale;
            head.GetComponent<Renderer>().sharedMaterial = headMaterial;

            leftLegs[index] = CreateLeg(
                personRoot,
                "Left Leg",
                new Vector3(-0.022f, 0.086f, 0.014f) * PassengerUnitLayout.VisualScale,
                legMaterial,
                out var leftFoot);
            leftFeet[index] = leftFoot;
            rightLegs[index] = CreateLeg(
                personRoot,
                "Right Leg",
                new Vector3(0.022f, 0.086f, 0.014f) * PassengerUnitLayout.VisualScale,
                legMaterial,
                out var rightFoot);
            rightFeet[index] = rightFoot;
            leftArms[index] = CreateArm(
                personRoot,
                "Left Arm",
                new Vector3(-0.054f, 0.206f, 0.010f) * PassengerUnitLayout.VisualScale,
                -1f,
                bodyMaterial,
                headMaterial);
            rightArms[index] = CreateArm(
                personRoot,
                "Right Arm",
                new Vector3(0.054f, 0.206f, 0.010f) * PassengerUnitLayout.VisualScale,
                1f,
                bodyMaterial,
                headMaterial);
            return personRoot;
        }

        private static Transform CreateLeg(Transform parent, string name, Vector3 localPosition, Material material, out Transform footRoot)
        {
            var legRoot = new GameObject(name).transform;
            legRoot.SetParent(parent, false);
            legRoot.localPosition = localPosition;

            var leg = VisualPrimitiveFactory.Create(PrimitiveType.Cube, "Leg Mesh");
            leg.transform.SetParent(legRoot, false);
            leg.transform.localPosition = new Vector3(0f, -0.054f * PassengerUnitLayout.VisualScale, 0f);
            leg.transform.localScale = new Vector3(0.020f, 0.108f, 0.022f) *
                PassengerUnitLayout.VisualScale *
                BodyOnlyScaleMultiplier;
            leg.GetComponent<Renderer>().sharedMaterial = material;

            footRoot = new GameObject($"{name} Foot").transform;
            footRoot.SetParent(legRoot, false);
            footRoot.localPosition = new Vector3(0f, -0.111f, 0.024f) * PassengerUnitLayout.VisualScale;

            var foot = VisualPrimitiveFactory.Create(PrimitiveType.Cube, "Foot Mesh");
            foot.transform.SetParent(footRoot, false);
            foot.transform.localPosition = Vector3.zero;
            foot.transform.localScale = new Vector3(0.028f, 0.012f, 0.052f) *
                PassengerUnitLayout.VisualScale *
                BodyOnlyScaleMultiplier;
            foot.GetComponent<Renderer>().sharedMaterial = material;
            return legRoot;
        }

        private static Transform CreateArm(
            Transform parent,
            string name,
            Vector3 localPosition,
            float side,
            Material armMaterial,
            Material handMaterial)
        {
            var armRoot = new GameObject(name).transform;
            armRoot.SetParent(parent, false);
            armRoot.localPosition = localPosition;

            var arm = VisualPrimitiveFactory.Create(PrimitiveType.Cube, "Arm Mesh");
            arm.transform.SetParent(armRoot, false);
            arm.transform.localPosition = new Vector3(side * 0.008f, -0.056f, 0.018f) * PassengerUnitLayout.VisualScale;
            arm.transform.localScale = new Vector3(0.016f, 0.104f, 0.018f) *
                PassengerUnitLayout.VisualScale *
                BodyOnlyScaleMultiplier;
            arm.GetComponent<Renderer>().sharedMaterial = armMaterial;

            var hand = VisualPrimitiveFactory.Create(PrimitiveType.Sphere, "Hand");
            hand.transform.SetParent(armRoot, false);
            hand.transform.localPosition = new Vector3(side * 0.010f, -0.116f, 0.026f) * PassengerUnitLayout.VisualScale;
            hand.transform.localScale = Vector3.one *
                (0.020f * PassengerUnitLayout.VisualScale * BodyOnlyScaleMultiplier);
            hand.GetComponent<Renderer>().sharedMaterial = handMaterial;
            return armRoot;
        }
    }
}
