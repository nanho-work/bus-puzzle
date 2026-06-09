#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BusPuzzle.EditorTools
{
    public static class VehicleShapePreviewSceneBuilder
    {
        private const string PreviewScenePath = "Assets/BusPuzzle/Scenes/VehicleShapePreview.unity";
        private const float CellSize = 0.62f;
        private const float VehicleWidthCells = 0.72f;
        private const float VehicleHeightCells = 0.90f;

        [MenuItem("Bus Puzzle/Preview/Rebuild Vehicle Shape Preview")]
        public static void RebuildVehicleShapePreview()
        {
            EnsureSceneDirectory();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateEnvironment();
            CreateVehicleSample(BusSize.Small, new Vector3(-2.2f, 0f, 0f), "Small");
            CreateVehicleSample(BusSize.Medium, Vector3.zero, "Medium");
            CreateVehicleSample(BusSize.Large, new Vector3(2.4f, 0f, 0f), "Large");
            CreateCamera();
            CreateLight();

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(PreviewScenePath);
            Debug.Log($"Vehicle shape preview scene rebuilt: {PreviewScenePath}");
        }

        private static void CreateVehicleSample(BusSize size, Vector3 position, string label)
        {
            var sampleRoot = new GameObject($"{label} Vehicle Preview");
            sampleRoot.transform.position = position;

            var visualLength = BusSizeUtility.ToVisualLengthCells(size) * CellSize;
            var visualCharacterLength = visualLength / Mathf.Max(1, BusSizeUtility.ToVisualCharacterUnits(size));
            var visualCenterZ = (visualLength - visualCharacterLength) * 0.5f;
            var visualWidth = VehicleWidthCells * CellSize;
            var visualHeight = VehicleHeightCells * CellSize;

            var model = VehicleModelBuilder.Create(
                size,
                PuzzleColor.Blue,
                sampleRoot.transform,
                visualWidth,
                visualHeight,
                visualLength,
                visualCenterZ,
                CellSize);
            model.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);

            CreateLabel($"{label} Label", label, sampleRoot.transform, new Vector3(0f, 0.02f, -1.35f));
            CreateFootprintPad($"{label} Pad", sampleRoot.transform, visualWidth * 1.45f, visualLength * 1.25f, visualCenterZ);
        }

        private static void CreateEnvironment()
        {
            var root = new GameObject("Preview Environment");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Neutral Preview Floor";
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(0f, -0.035f, 0.1f);
            ground.transform.localScale = new Vector3(6.6f, 0.035f, 4.0f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Preview Floor Material", new Color(0.60f, 0.70f, 0.76f));

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Matte Backdrop";
            backdrop.transform.SetParent(root.transform, false);
            backdrop.transform.position = new Vector3(0f, 1.0f, 1.75f);
            backdrop.transform.localScale = new Vector3(6.6f, 2.0f, 0.05f);
            backdrop.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Preview Backdrop Material", new Color(0.50f, 0.62f, 0.70f));
        }

        private static void CreateFootprintPad(string name, Transform parent, float width, float length, float centerZ)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name;
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = new Vector3(0f, -0.010f, centerZ);
            pad.transform.localScale = new Vector3(width, 0.010f, length);
            pad.GetComponent<Renderer>().sharedMaterial = CreateMaterial($"{name} Material", new Color(0.72f, 0.80f, 0.84f));
        }

        private static void CreateLabel(string name, string text, Transform parent, Vector3 localPosition)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.18f;
            label.fontSize = 48;
            label.color = new Color(0.08f, 0.12f, 0.15f);
            label.fontStyle = FontStyle.Bold;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Preview Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 4.8f, -4.2f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 2.65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.54f, 0.66f, 0.74f);
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Preview Key Light", typeof(Light));
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.08f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.64f, 0.70f);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name,
                color = color
            };
            return material;
        }

        private static void EnsureSceneDirectory()
        {
            var directory = Path.GetDirectoryName(PreviewScenePath);
            if (!string.IsNullOrEmpty(directory) && !AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets/BusPuzzle", "Scenes");
            }
        }
    }
}
#endif
