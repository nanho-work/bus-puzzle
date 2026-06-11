#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BusPuzzle
{
    public sealed class ReleasePlayerSettingsBuildValidator : IPreprocessBuildWithReport
    {
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string AndroidResolverPath = "ProjectSettings/AndroidResolverDependencies.xml";
        private const string GameUiControllerPath = "Assets/BusPuzzle/Scripts/UI/GameUiController.cs";
        private const string AppIconPath = "Assets/BusPuzzle/Resources/UI/Boosters/Bus_Pop(en)_icon.png";
        private const string AppIconGuid = "e9c693834f7d74611a1844930e77f5c5";
        private const string ProductName = "Bus Pop";
        private const string CompanyName = "Koofy Lab";
        private const string BundleIdentifier = "com.koofylab.buspop";
        private const string PrivacyPolicyUrl = "https://www.koofy.co.kr/bus-pop/privacy";

        public int callbackOrder => -10;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || IsDevelopmentBuild(report) || !IsMobileTarget(report.summary.platform))
            {
                return;
            }

            ValidateReleaseSettings(report.summary.platform);
        }

        [MenuItem("Bus Puzzle/Release/Validate Player Settings")]
        private static void ValidateCurrentSettings()
        {
            ValidateReleaseSettings(EditorUserBuildSettings.activeBuildTarget);
            UnityEngine.Debug.Log("Bus Pop release player settings passed validation.");
        }

        private static void ValidateReleaseSettings(BuildTarget target)
        {
            var settings = ReadRequiredFile(ProjectSettingsPath);
            RequireContains(settings, $"productName: {ProductName}", "Player Settings productName must be Bus Pop.");
            RequireContains(settings, $"companyName: {CompanyName}", "Player Settings companyName must be Koofy Lab.");
            RequireContains(settings, $"Android: {BundleIdentifier}", "Android bundle identifier must be com.koofylab.buspop.");
            RequireContains(settings, $"iPhone: {BundleIdentifier}", "iOS bundle identifier must be com.koofylab.buspop.");
            RequireContains(settings, "bundleVersion: 1.0.0", "Release version should start at 1.0.0.");
            RequireContains(settings, AppIconGuid, "App icon is not assigned in Player Settings.");
            RequireContains(settings, "defaultScreenOrientation: 2", "Default orientation must be Portrait. Do not ship reverse portrait builds.");
            RequireContains(settings, "allowedAutorotateToPortrait: 1", "Portrait orientation must be allowed.");
            RequireContains(settings, "allowedAutorotateToPortraitUpsideDown: 0", "Portrait upside down must be disabled.");
            RequireContains(settings, "allowedAutorotateToLandscapeRight: 0", "Landscape right must be disabled.");
            RequireContains(settings, "allowedAutorotateToLandscapeLeft: 0", "Landscape left must be disabled.");

            if (!File.Exists(AppIconPath))
            {
                throw new BuildFailedException($"App icon file is missing: {AppIconPath}");
            }

            if (target == BuildTarget.Android)
            {
                if (ReadYamlField(settings, "AndroidTargetArchitectures") != "3")
                {
                    throw new BuildFailedException("Android release must include ARM64.");
                }

                if (ReadYamlNestedField(settings, "scriptingBackend", "Android") != "1")
                {
                    throw new BuildFailedException("Android release must use IL2CPP.");
                }

                var versionCodeText = ReadYamlField(settings, "AndroidBundleVersionCode");
                if (!int.TryParse(versionCodeText, out var versionCode) || versionCode < 2)
                {
                    throw new BuildFailedException("Android versionCode must be 2 or higher because versionCode 1 was already uploaded to Google Play.");
                }

                var resolverSettings = ReadRequiredFile(AndroidResolverPath);
                RequireContains(resolverSettings, "arm64-v8a", "Android resolver ABI list must include arm64-v8a.");
                RequireContains(resolverSettings, BundleIdentifier, "Android resolver bundle id is out of sync.");
            }

            if (target == BuildTarget.iOS)
            {
                if (ReadYamlField(settings, "appleDeveloperTeamID").Length == 0)
                {
                    throw new BuildFailedException("iOS Apple Developer Team ID is missing. Set it in Player Settings before release builds.");
                }

                var automaticSigning = ReadYamlField(settings, "appleEnableAutomaticSigning");
                var manualProfile = ReadYamlField(settings, "iOSManualSigningProvisioningProfileID");
                if (automaticSigning != "1" && manualProfile.Length == 0)
                {
                    throw new BuildFailedException("iOS signing is not configured. Enable automatic signing or set a provisioning profile before release builds.");
                }
            }

            var uiController = ReadRequiredFile(GameUiControllerPath);
            RequireContains(uiController, PrivacyPolicyUrl, "In-app privacy policy URL must point to /bus-pop/privacy.");
        }

        private static string ReadRequiredFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new BuildFailedException($"Required release settings file is missing: {path}");
            }

            return File.ReadAllText(path);
        }

        private static void RequireContains(string contents, string expected, string message)
        {
            if (contents.IndexOf(expected, StringComparison.Ordinal) < 0)
            {
                throw new BuildFailedException(message);
            }
        }

        private static string ReadYamlField(string contents, string fieldName)
        {
            using var reader = new StringReader(contents);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith(fieldName + ":", StringComparison.Ordinal))
                {
                    continue;
                }

                return trimmed.Substring(fieldName.Length + 1).Trim();
            }

            return string.Empty;
        }

        private static string ReadYamlNestedField(string contents, string parentFieldName, string childFieldName)
        {
            using var reader = new StringReader(contents);
            string line;
            var inParent = false;
            var parentIndent = -1;
            while ((line = reader.ReadLine()) != null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var indent = line.Length - line.TrimStart().Length;
                if (!inParent)
                {
                    if (trimmed == parentFieldName + ":")
                    {
                        inParent = true;
                        parentIndent = indent;
                    }

                    continue;
                }

                if (indent <= parentIndent)
                {
                    break;
                }

                if (trimmed.StartsWith(childFieldName + ":", StringComparison.Ordinal))
                {
                    return trimmed.Substring(childFieldName.Length + 1).Trim();
                }
            }

            return string.Empty;
        }

        private static bool IsDevelopmentBuild(BuildReport report)
        {
            return report != null && report.summary.options.ToString().Contains("Development");
        }

        private static bool IsMobileTarget(BuildTarget target)
        {
            return target == BuildTarget.Android || target == BuildTarget.iOS;
        }
    }
}
#endif
