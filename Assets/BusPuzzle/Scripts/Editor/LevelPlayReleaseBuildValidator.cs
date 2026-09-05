#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class LevelPlayReleaseBuildValidator : IPreprocessBuildWithReport
    {
        private const string LevelPlayScriptingDefine = "BUS_PUZZLE_LEVELPLAY";
        private const string AdMobScriptingDefine = "BUS_PUZZLE_ADMOB";
        private const string PackageManifestPath = "Packages/manifest.json";
        private const string LevelPlayMediationSettingsPath = "Assets/LevelPlay/Resources/LevelPlayMediationSettings.asset";
        private const string NetworkManagerSettingsPath = "Assets/LevelPlay/Editor/NetworkManagerSettings.asset";
        private const string LevelPlaySdkDependenciesPath = "Assets/LevelPlay/Editor/IronSourceSDKDependencies.xml";
        private const string UnityAdsAdapterDependenciesPath = "Assets/LevelPlay/Editor/ISUnityAdsAdapterDependencies.xml";
        private const string MainGradleTemplatePath = "Assets/Plugins/Android/mainTemplate.gradle";
        private const string AndroidResolverDependenciesPath = "ProjectSettings/AndroidResolverDependencies.xml";

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || IsDevelopmentBuild(report) || !IsMobileTarget(report.summary.platform))
            {
                return;
            }

            ValidateReleaseSettings(report.summary.platform);
        }

        [MenuItem("Bus Puzzle/Ads/Validate LevelPlay Release Settings")]
        private static void ValidateCurrentSettings()
        {
            ValidateReleaseSettings(EditorUserBuildSettings.activeBuildTarget);
            Debug.Log("Bus Pop LevelPlay release settings passed validation.");
        }

        public static void ValidateCurrentSettingsFromCommandLine()
        {
            ValidateReleaseSettings(BuildTarget.Android);
            ValidateReleaseSettings(BuildTarget.iOS);
            Debug.Log("Bus Pop Android and iOS LevelPlay settings passed validation.");
        }

        private static void ValidateReleaseSettings(BuildTarget target)
        {
            if (!HasCompilerDefine(target, LevelPlayScriptingDefine))
            {
                throw new BuildFailedException($"Add {LevelPlayScriptingDefine} to Scripting Define Symbols before a mobile release build.");
            }

            if (HasCompilerDefine(target, AdMobScriptingDefine))
            {
                throw new BuildFailedException(
                    $"{AdMobScriptingDefine} must stay disabled while LevelPlay is the active ad provider.");
            }

            var settings = Resources.Load<LevelPlaySettings>(LevelPlaySettings.ResourcePath);
            if (settings == null)
            {
                throw new BuildFailedException(
                    "LevelPlaySettings asset is missing. Create Assets/BusPuzzle/Resources/Ads/LevelPlaySettings.asset.");
            }

            var appKey = target == BuildTarget.iOS ? settings.IosAppKey : settings.AndroidAppKey;
            var bannerAdUnitId = target == BuildTarget.iOS
                ? settings.IosBannerAdUnitId
                : settings.AndroidBannerAdUnitId;
            var rewardedAdUnitId = target == BuildTarget.iOS
                ? settings.IosRewardedAdUnitId
                : settings.AndroidRewardedAdUnitId;

            if (!LevelPlaySettings.LooksLikeLevelPlayAppKey(appKey))
            {
                throw new BuildFailedException($"LevelPlay app key is missing or invalid for {target}: {appKey}");
            }

            if (!LevelPlaySettings.LooksLikeLevelPlayAdUnitId(bannerAdUnitId))
            {
                throw new BuildFailedException($"LevelPlay banner ad unit ID is missing or invalid for {target}: {bannerAdUnitId}");
            }

            if (!LevelPlaySettings.LooksLikeLevelPlayAdUnitId(rewardedAdUnitId))
            {
                throw new BuildFailedException($"LevelPlay rewarded ad unit ID is missing or invalid for {target}: {rewardedAdUnitId}");
            }

            if (!settings.ServeContextualAdsUntilConsentFlow)
            {
                throw new BuildFailedException(
                    "Contextual-only LevelPlay privacy mode must remain enabled until a complete consent flow is implemented.");
            }

            ValidatePackageManifest();
            ValidateMediationSettings(settings);
            ValidateNetworkManagerSettings();
            ValidateDependencyFiles();
            if (target == BuildTarget.Android)
            {
                ValidateAndroidGradleDependencies();
            }
        }

        private static void ValidatePackageManifest()
        {
            var manifest = ReadRequiredFile(PackageManifestPath);
            RequireContains(
                manifest,
                "\"com.unity.services.levelplay\": \"9.5.0\"",
                "Packages/manifest.json must pin com.unity.services.levelplay 9.5.0.");
        }

        private static void ValidateMediationSettings(LevelPlaySettings settings)
        {
            var mediationSettings = ReadRequiredFile(LevelPlayMediationSettingsPath);
            RequireContains(
                mediationSettings,
                "EnableIronsourceSDKInitAPI: 0",
                "LevelPlay automatic initialization must be disabled; BusPop initializes it after privacy settings are applied.");
            RequireContains(
                mediationSettings,
                $"AndroidAppKey: {settings.AndroidAppKey}",
                "LevelPlay Android app key is out of sync.");
            RequireContains(
                mediationSettings,
                $"IOSAppKey: {settings.IosAppKey}",
                "LevelPlay iOS app key is out of sync.");
            RequireContains(
                mediationSettings,
                "DeclareAD_IDPermission: 1",
                "LevelPlay must declare the Android advertising ID permission.");
            RequireContains(
                mediationSettings,
                "EnableAdapterDebug: 0",
                "LevelPlay adapter debug logging must be disabled in release settings.");
            RequireContains(
                mediationSettings,
                "EnableIntegrationHelper: 0",
                "LevelPlay Integration Helper must be disabled in release settings.");
        }

        private static void ValidateDependencyFiles()
        {
            GetRequiredAndroidPackageSpec(
                LevelPlaySdkDependenciesPath,
                "com.unity3d.ads-mediation:mediation-sdk:");
            GetRequiredIosPodVersion(LevelPlaySdkDependenciesPath, "IronSourceSDK");

            GetRequiredAndroidPackageSpec(
                UnityAdsAdapterDependenciesPath,
                "com.unity3d.ads-mediation:unityads-adapter:");
            GetRequiredAndroidPackageSpec(
                UnityAdsAdapterDependenciesPath,
                "com.unity3d.ads:unity-ads:");
            GetRequiredIosPodVersion(UnityAdsAdapterDependenciesPath, "IronSourceUnityAdsAdapter");
        }

        private static void ValidateNetworkManagerSettings()
        {
            var networkManagerSettings = ReadRequiredFile(NetworkManagerSettingsPath);
            RequireContains(
                networkManagerSettings,
                "AddNetworksSkadnetworkID: 1",
                "LevelPlay must automatically add installed networks' SKAdNetwork IDs to the iOS Info.plist.");
        }

        private static void ValidateAndroidGradleDependencies()
        {
            var gradleTemplate = ReadRequiredFile(MainGradleTemplatePath);
            var resolverDependencies = ReadRequiredFile(AndroidResolverDependenciesPath);
            var requiredPackageSpecs = new[]
            {
                GetRequiredAndroidPackageSpec(
                    LevelPlaySdkDependenciesPath,
                    "com.unity3d.ads-mediation:mediation-sdk:"),
                GetRequiredAndroidPackageSpec(
                    UnityAdsAdapterDependenciesPath,
                    "com.unity3d.ads-mediation:unityads-adapter:"),
                GetRequiredAndroidPackageSpec(
                    UnityAdsAdapterDependenciesPath,
                    "com.unity3d.ads:unity-ads:")
            };

            foreach (var packageSpec in requiredPackageSpecs)
            {
                RequireContains(
                    gradleTemplate,
                    packageSpec,
                    $"Resolve Android dependencies again; {packageSpec} is missing from mainTemplate.gradle.");
                RequireContains(
                    resolverDependencies,
                    $"<package>{packageSpec}</package>",
                    $"Resolve Android dependencies again; {packageSpec} is missing from AndroidResolverDependencies.xml.");
            }

            if (gradleTemplate.IndexOf("com.google.android.gms:play-services-ads:", StringComparison.Ordinal) >= 0 ||
                gradleTemplate.IndexOf("com.google.android.ump:user-messaging-platform:", StringComparison.Ordinal) >= 0)
            {
                throw new BuildFailedException(
                    "Direct Google Mobile Ads dependencies are still present in the Android release template.");
            }
        }

        private static string GetRequiredAndroidPackageSpec(string path, string packagePrefix)
        {
            var document = LoadRequiredXml(path);
            var nodes = document.SelectNodes("//androidPackage");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var spec = node.Attributes?["spec"]?.Value;
                    if (!string.IsNullOrWhiteSpace(spec) &&
                        spec.StartsWith(packagePrefix, StringComparison.Ordinal) &&
                        spec.Length > packagePrefix.Length)
                    {
                        return spec;
                    }
                }
            }

            throw new BuildFailedException(
                $"Required Android dependency {packagePrefix}<version> is missing from {path}.");
        }

        private static string GetRequiredIosPodVersion(string path, string podName)
        {
            var document = LoadRequiredXml(path);
            var nodes = document.SelectNodes("//iosPod");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var name = node.Attributes?["name"]?.Value;
                    var version = node.Attributes?["version"]?.Value;
                    if (string.Equals(name, podName, StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(version))
                    {
                        return version;
                    }
                }
            }

            throw new BuildFailedException($"Required iOS pod {podName} is missing from {path}.");
        }

        private static XmlDocument LoadRequiredXml(string path)
        {
            ReadRequiredFile(path);

            try
            {
                var document = new XmlDocument();
                document.Load(path);
                return document;
            }
            catch (XmlException exception)
            {
                throw new BuildFailedException($"Invalid XML in {path}: {exception.Message}");
            }
        }

        private static string ReadRequiredFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new BuildFailedException($"Required LevelPlay release file is missing: {path}");
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

        private static bool HasCompilerDefine(BuildTarget target, string symbol)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            if (symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Contains(symbol))
            {
                return true;
            }

            const string responseFilePath = "Assets/csc.rsp";
            if (!File.Exists(responseFilePath))
            {
                return false;
            }

            var responseFile = File.ReadAllText(responseFilePath);
            return responseFile.IndexOf($"-define:{symbol}", StringComparison.Ordinal) >= 0 ||
                responseFile.IndexOf($"-d:{symbol}", StringComparison.Ordinal) >= 0;
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
