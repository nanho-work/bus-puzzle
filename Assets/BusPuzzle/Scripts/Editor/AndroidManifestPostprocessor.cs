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
        private const string PortraitOrientation = "portrait";
        private const string VibratePermission = "android.permission.VIBRATE";
        private const string UnityPlayerActivityPrefix = "com.unity3d.player.UnityPlayer";
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
            var activities = document
                .Descendants("activity")
                .Where(IsUnityLauncherActivity)
                .ToList();

            if (activities.Count == 0)
            {
                throw new BuildFailedException("Unity launcher activity was not found in the generated Android manifest.");
            }

            foreach (var activity in activities)
            {
                ForcePortraitActivity(activity);
            }

            EnsurePermission(document, VibratePermission);
            ForcePortraitApplicationMetadata(document);
            document.Save(manifestPath);
            Debug.Log($"Bus Pop Android manifest locked to portrait for {activities.Count} launcher activity entry: {manifestPath}");
        }

        private static bool IsUnityLauncherActivity(XElement activity)
        {
            var activityName = (string)activity.Attribute(AndroidNamespace + "name") ?? string.Empty;
            if (activityName.StartsWith(UnityPlayerActivityPrefix, System.StringComparison.Ordinal))
            {
                return true;
            }

            if (HasMetadata(activity, "unityplayer.UnityActivity"))
            {
                return true;
            }

            return activity
                .Descendants("intent-filter")
                .Any(filter =>
                    filter.Descendants("action").Any(action =>
                        (string)action.Attribute(AndroidNamespace + "name") == "android.intent.action.MAIN") &&
                    filter.Descendants("category").Any(category =>
                        (string)category.Attribute(AndroidNamespace + "name") == "android.intent.category.LAUNCHER"));
        }

        private static void ForcePortraitActivity(XElement activity)
        {
            activity.SetAttributeValue(AndroidNamespace + "screenOrientation", PortraitOrientation);
            activity.SetAttributeValue(AndroidNamespace + "resizeableActivity", "false");
            SetOrCreateMetadata(activity, "WindowManagerPreference:FreeformWindowOrientation", "@string/FreeformWindowOrientation_portrait");
        }

        private static void ForcePortraitApplicationMetadata(XDocument document)
        {
            var application = document.Descendants("application").FirstOrDefault();
            if (application == null)
            {
                return;
            }

            SetOrCreateMetadata(application, "notch.config", PortraitOrientation);
        }

        private static void EnsurePermission(XDocument document, string permissionName)
        {
            var manifest = document.Root;
            if (manifest == null ||
                manifest.Elements("uses-permission")
                    .Any(element => (string)element.Attribute(AndroidNamespace + "name") == permissionName))
            {
                return;
            }

            manifest.AddFirst(new XElement(
                "uses-permission",
                new XAttribute(AndroidNamespace + "name", permissionName)));
        }

        private static bool HasMetadata(XElement parent, string name)
        {
            return parent
                .Elements("meta-data")
                .Any(element => (string)element.Attribute(AndroidNamespace + "name") == name);
        }

        private static void SetOrCreateMetadata(XElement parent, string name, string value)
        {
            var metadata = parent
                .Elements("meta-data")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);

            if (metadata == null)
            {
                metadata = new XElement("meta-data");
                parent.Add(metadata);
            }

            metadata.SetAttributeValue(AndroidNamespace + "name", name);
            metadata.SetAttributeValue(AndroidNamespace + "value", value);
        }
    }
}
#endif
