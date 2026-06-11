#if UNITY_EDITOR && UNITY_ANDROID
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class AndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string UnityPlayerActivity = "com.unity3d.player.UnityPlayerActivity";
        private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            ForcePortraitOrientation(manifestPath);
        }

        private static void ForcePortraitOrientation(string manifestPath)
        {
            if (!File.Exists(manifestPath))
            {
                throw new BuildFailedException($"Android manifest was not generated: {manifestPath}");
            }

            var document = XDocument.Load(manifestPath);
            var activity = document
                .Descendants("activity")
                .FirstOrDefault(element =>
                    (string)element.Attribute(AndroidNamespace + "name") == UnityPlayerActivity);

            if (activity == null)
            {
                throw new BuildFailedException("UnityPlayerActivity was not found in the generated Android manifest.");
            }

            activity.SetAttributeValue(AndroidNamespace + "screenOrientation", "portrait");
            document.Save(manifestPath);
            Debug.Log($"Bus Pop Android manifest locked to portrait: {manifestPath}");
        }
    }
}
#endif
