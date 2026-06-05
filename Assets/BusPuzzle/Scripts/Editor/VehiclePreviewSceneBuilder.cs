#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusPuzzle.EditorTools
{
    public static class VehiclePreviewSceneBuilder
    {
        private const string PreviewScenePath = "Assets/BusPuzzle/Scenes/VehiclePreview.unity";
        private const float SlotSpacingX = 2.15f;
        private const float SlotSpacingZ = 2.20f;
        private const float TargetFootprint = 1.30f;

        private static readonly string[] CandidatePaths =
        {
            "Assets/van/van.FBX",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Bus_1.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Bus_2.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Car_1.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Car_2.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Car_3.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Car_4.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Taxi.prefab",
            "Assets/Simple Vehicle Pack/Prefabs/Mobile/Police_car.prefab",
            "Assets/Cobra Games Studio/Low Poly Bus Pack/Prefabs/kozak_i_van.prefab"
        };

        [MenuItem("Bus Puzzle/Vehicle Preview/Rebuild Vehicle Preview Scene")]
        public static void RebuildVehiclePreviewScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Vehicle Preview Gallery");
            var loadedPaths = new List<string>();

            CreateLighting();
            CreateCamera();
            CreateFloor(root.transform);

            for (var index = 0; index < CandidatePaths.Length; index++)
            {
                var path = CandidatePaths[index];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                var row = index / 3;
                var column = index % 3;
                var slotPosition = new Vector3((column - 1) * SlotSpacingX, 0f, -row * SlotSpacingZ);
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"{Path.GetFileNameWithoutExtension(path)} Preview";
                instance.transform.SetParent(root.transform, true);
                instance.transform.SetPositionAndRotation(slotPosition, Quaternion.Euler(0f, 145f, 0f));
                NormalizeToSlot(instance, slotPosition);
                CreateLabel(root.transform, slotPosition + new Vector3(0f, 0.02f, -0.92f), GetDisplayLabel(path));
                loadedPaths.Add(path);
            }

            var pathList = new GameObject("Candidate Asset Paths");
            pathList.transform.SetParent(root.transform, false);
            var note = pathList.AddComponent<VehiclePreviewPathList>();
            note.Paths = loadedPaths.ToArray();

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Vehicle preview scene rebuilt: {PreviewScenePath}");
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.08f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.82f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 6.1f, -5.8f);
            cameraObject.transform.rotation = Quaternion.Euler(56f, 0f, 0f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.68f, 0.78f);
        }

        private static void CreateFloor(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Preview Floor";
            floor.transform.SetParent(parent, false);
            floor.transform.position = new Vector3(0f, -0.03f, -3.0f);
            floor.transform.localScale = new Vector3(7.2f, 0.04f, 7.3f);
            floor.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Vehicle Preview Floor", new Color(0.78f, 0.86f, 0.90f));
        }

        private static void NormalizeToSlot(GameObject instance, Vector3 slotPosition)
        {
            var bounds = CalculateBounds(instance);
            var footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            if (footprint > 0.001f)
            {
                instance.transform.localScale *= TargetFootprint / footprint;
            }

            bounds = CalculateBounds(instance);
            var offset = slotPosition - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            instance.transform.position += offset;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void CreateLabel(Transform parent, Vector3 position, string text)
        {
            var label = new GameObject($"{text} Label");
            label.transform.SetParent(parent, false);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);

            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 42;
            textMesh.characterSize = 0.050f;
            textMesh.color = new Color(0.08f, 0.10f, 0.13f);
        }

        private static string GetDisplayLabel(string path)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folderName = Path.GetFileName(Path.GetDirectoryName(path));
            return $"{fileName}\n{folderName}";
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            return material;
        }
    }

    public sealed class VehiclePreviewPathList : MonoBehaviour
    {
        [TextArea(4, 18)] public string[] Paths;
    }
}
#endif
