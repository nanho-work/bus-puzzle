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
        private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";
        private const string AndroidResolverPath = "ProjectSettings/AndroidResolverDependencies.xml";
        private const string GameUiControllerPath = "Assets/BusPuzzle/Scripts/UI/GameUiController.cs";
        private const string RemoteConfigServicePath = "Assets/BusPuzzle/Scripts/Core/RemoteConfigService.cs";
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

        public static void ValidateCurrentSettingsFromCommandLine()
        {
            ValidateReleaseSettings(BuildTarget.Android);
            ValidateReleaseSettings(BuildTarget.iOS);
            UnityEngine.Debug.Log(
                "Bus Pop Android and iOS release player settings passed validation.");
        }

        private static void ValidateReleaseSettings(BuildTarget target)
        {
            var settings = ReadRequiredFile(ProjectSettingsPath);
            RequireContains(settings, $"productName: {ProductName}", "Player Settings productName must be Bus Pop.");
            RequireContains(settings, $"companyName: {CompanyName}", "Player Settings companyName must be Koofy Lab.");
            RequireContains(settings, $"Android: {BundleIdentifier}", "Android bundle identifier must be com.koofylab.buspop.");
            RequireContains(settings, $"iPhone: {BundleIdentifier}", "iOS bundle identifier must be com.koofylab.buspop.");
            RequireContains(settings, "bundleVersion: 1.1.0", "Release version should be 1.1.0 for this update.");
            RequireContains(settings, AppIconGuid, "App icon is not assigned in Player Settings.");
            RequireContains(settings, "defaultScreenOrientation: 5", "Default orientation must be Auto Rotation so Android can preserve the current portrait direction at launch.");
            RequireContains(settings, "allowedAutorotateToPortrait: 1", "Portrait orientation must be allowed.");
            RequireContains(settings, "allowedAutorotateToPortraitUpsideDown: 1", "Portrait upside down must be allowed so Android does not force a 180 degree flip at launch.");
            RequireContains(settings, "allowedAutorotateToLandscapeRight: 0", "Landscape right must be disabled.");
            RequireContains(settings, "allowedAutorotateToLandscapeLeft: 0", "Landscape left must be disabled.");
            RequireContains(settings, "androidResizeableActivity: 0", "Android activity must not be resizeable for portrait launch stability.");

            if (!File.Exists(AppIconPath))
            {
                throw new BuildFailedException($"App icon file is missing: {AppIconPath}");
            }

            if (target == BuildTarget.Android)
            {
                RequireContains(settings, "AndroidSplashScreenScale: 0", "Android native splash image scale should stay neutral because launch art is shown inside Unity.");
                RequireContains(settings, "androidSplashScreen: {fileID: 0}", "Android native splash image must stay empty. Show the Bus Pop launch image inside Unity after orientation settles.");

                if (ReadYamlField(settings, "AndroidTargetArchitectures") != "3")
                {
                    throw new BuildFailedException("Android release must include ARM64.");
                }

                if (ReadYamlNestedField(settings, "scriptingBackend", "Android") != "1")
                {
                    throw new BuildFailedException("Android release must use IL2CPP.");
                }

                var versionCodeText = ReadYamlField(settings, "AndroidBundleVersionCode");
                if (!int.TryParse(versionCodeText, out var versionCode) || versionCode < 18)
                {
                    throw new BuildFailedException(
                        "Android versionCode must be 18 or higher for the 1.1.0 release baseline.");
                }

                var remoteConfigService = ReadRequiredFile(RemoteConfigServicePath);
                RequireContains(remoteConfigService, $"CurrentAndroidVersionCode = {versionCode}", "RemoteConfigService CurrentAndroidVersionCode must match Android versionCode.");

                var resolverSettings = ReadRequiredFile(AndroidResolverPath);
                RequireContains(resolverSettings, "arm64-v8a", "Android resolver ABI list must include arm64-v8a.");
                RequireContains(resolverSettings, BundleIdentifier, "Android resolver bundle id is out of sync.");

                var qualitySettings = ReadRequiredFile(QualitySettingsPath);
                if (ReadYamlNestedField(qualitySettings, "m_PerPlatformDefaultQuality", "Android") != "1")
                {
                    throw new BuildFailedException("Android release must use Low quality by default for stable mobile frame pacing.");
                }
            }

            if (target == BuildTarget.iOS)
            {
                var iosBuildNumberText = ReadYamlNestedField(settings, "buildNumber", "iPhone");
                if (!int.TryParse(iosBuildNumberText, out var iosBuildNumber) || iosBuildNumber < 18)
                {
                    throw new BuildFailedException(
                        "iOS build number must be 18 or higher for the 1.1.0 release baseline.");
                }

                var remoteConfigService = ReadRequiredFile(RemoteConfigServicePath);
                RequireContains(remoteConfigService, $"CurrentIosBuildNumber = {iosBuildNumber}", "RemoteConfigService CurrentIosBuildNumber must match iOS build number.");

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
