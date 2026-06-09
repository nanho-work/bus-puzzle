using UnityEngine;

namespace BusPuzzle
{
    internal static class VisualPrimitiveFactory
    {
        private static Mesh cubeMesh;
        private static Mesh sphereMesh;
        private static Mesh capsuleMesh;
        private static Mesh cylinderMesh;

        public static GameObject Create(PrimitiveType type, string name)
        {
            var gameObject = new GameObject(name);
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetMesh(type);
            gameObject.AddComponent<MeshRenderer>();
            return gameObject;
        }

        private static Mesh GetMesh(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Sphere:
                    return sphereMesh != null ? sphereMesh : sphereMesh = LoadMesh("Sphere.fbx");
                case PrimitiveType.Capsule:
                    return capsuleMesh != null ? capsuleMesh : capsuleMesh = LoadMesh("Capsule.fbx");
                case PrimitiveType.Cylinder:
                    return cylinderMesh != null ? cylinderMesh : cylinderMesh = LoadMesh("Cylinder.fbx");
                default:
                    return cubeMesh != null ? cubeMesh : cubeMesh = LoadMesh("Cube.fbx");
            }
        }

        private static Mesh LoadMesh(string meshName)
        {
            var mesh = Resources.GetBuiltinResource<Mesh>(meshName);
            return mesh != null ? mesh : Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }
    }
}
