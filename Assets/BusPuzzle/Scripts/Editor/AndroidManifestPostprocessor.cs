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
        private const string StartupScreenOrientation = "userPortrait";
        private const string PortraitConfiguration = "portrait";
        private const string AppCategoryGame = "game";
        private const string RestrictedResizableProperty = "android.window.PROPERTY_COMPAT_ALLOW_RESTRICTED_RESIZABILITY";
        private const string VibratePermission = "android.permission.VIBRATE";
        private const string UnityPlayerActivityPrefix = "com.unity3d.player.UnityPlayer";
        private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public int callbackOrder => 1000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            ForcePortraitOrientation(manifestPath);
            ForceQuietStartupTheme(path);
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

        private static void ForceQuietStartupTheme(string projectPath)
        {
            var stylePaths = new[]
            {
                Path.Combine(projectPath, "src/main/res/values/styles.xml"),
                Path.Combine(projectPath, "src/main/res/values-v31/styles.xml")
            };

            foreach (var stylePath in stylePaths.Where(File.Exists))
            {
                var document = XDocument.Load(stylePath);
                var styles = document
                    .Descendants("style")
                    .Where(IsStartupTheme)
                    .ToList();

                foreach (var style in styles)
                {
                    SetOrCreateStyleItem(style, "android:windowBackground", "@android:color/black");
                    SetOrCreateStyleItem(style, "android:windowDisablePreview", "true");
                }

                document.Save(stylePath);
                Debug.Log($"Bus Pop Android startup theme quieted for {styles.Count} style entry: {stylePath}");
            }
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
            activity.SetAttributeValue(AndroidNamespace + "screenOrientation", StartupScreenOrientation);
            activity.SetAttributeValue(AndroidNamespace + "resizeableActivity", "false");
            SetOrCreateMetadata(activity, "WindowManagerPreference:FreeformWindowOrientation", "@string/FreeformWindowOrientation_portrait");
            SetOrCreateProperty(activity, RestrictedResizableProperty, "true");
        }

        private static void ForcePortraitApplicationMetadata(XDocument document)
        {
            var application = document.Descendants("application").FirstOrDefault();
            if (application == null)
            {
                return;
            }

            application.SetAttributeValue(AndroidNamespace + "appCategory", AppCategoryGame);
            SetOrCreateProperty(application, RestrictedResizableProperty, "true");
            SetOrCreateMetadata(application, "notch.config", PortraitConfiguration);
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

        private static bool IsStartupTheme(XElement style)
        {
            var name = (string)style.Attribute("name") ?? string.Empty;
            return name == "BaseUnityTheme" || name == "UnityThemeSelector";
        }

        private static void SetOrCreateStyleItem(XElement style, string name, string value)
        {
            var item = style
                .Elements("item")
                .FirstOrDefault(element => (string)element.Attribute("name") == name);

            if (item == null)
            {
                item = new XElement("item", new XAttribute("name", name));
                style.Add(item);
            }

            item.Value = value;
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

        private static void SetOrCreateProperty(XElement parent, string name, string value)
        {
            var property = parent
                .Elements("property")
                .FirstOrDefault(element => (string)element.Attribute(AndroidNamespace + "name") == name);

            if (property == null)
            {
                property = new XElement("property");
                parent.Add(property);
            }

            property.SetAttributeValue(AndroidNamespace + "name", name);
            property.SetAttributeValue(AndroidNamespace + "value", value);
        }
    }
}
#endif
