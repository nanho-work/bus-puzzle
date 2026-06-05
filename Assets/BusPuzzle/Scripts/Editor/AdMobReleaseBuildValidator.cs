using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class AdMobReleaseBuildValidator : IPreprocessBuildWithReport
    {
        private const string AdMobScriptingDefine = "BUS_PUZZLE_ADMOB";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null || IsDevelopmentBuild(report) || !IsMobileTarget(report.summary.platform))
            {
                return;
            }

            var settings = Resources.Load<AdMobSettings>(AdMobSettings.ResourcePath);
            if (settings == null)
            {
                throw new BuildFailedException("AdMobSettings asset is missing. Create Assets/BusPuzzle/Resources/Ads/AdMobSettings.asset before release builds.");
            }

            if (!settings.UseProductionAdsInRelease)
            {
                throw new BuildFailedException("AdMobSettings.UseProductionAdsInRelease must be enabled for release builds.");
            }

            var appId = GetPlatformAppId(settings, report.summary.platform);
            var stationRewardedId = GetPlatformProductionRewardedAdUnitId(settings, report.summary.platform, RewardedAdPlacement.StationSlotUnlock);
            var vipRewardedId = GetPlatformProductionRewardedAdUnitId(settings, report.summary.platform, RewardedAdPlacement.VipBusTeleport);
            var shuffleRewardedId = GetPlatformProductionRewardedAdUnitId(settings, report.summary.platform, RewardedAdPlacement.BusColorShuffle);

            if (!AdMobSettings.LooksLikeAppId(appId))
            {
                throw new BuildFailedException($"AdMob app ID is missing or invalid for {report.summary.platform}: {appId}");
            }

            if (AdMobSettings.IsGoogleTestAdUnitId(appId))
            {
                throw new BuildFailedException("Release build is still configured with Google's test AdMob app ID.");
            }

            ValidateProductionRewardedAdUnitId(report.summary.platform, "station slot unlock", stationRewardedId);
            ValidateProductionRewardedAdUnitId(report.summary.platform, "VIP bus teleport", vipRewardedId);
            ValidateProductionRewardedAdUnitId(report.summary.platform, "bus color shuffle", shuffleRewardedId);

            if (!HasAdMobCompilerDefine(report.summary.platform))
            {
                throw new BuildFailedException($"Add {AdMobScriptingDefine} to Scripting Define Symbols after installing the Google Mobile Ads Unity SDK.");
            }
        }

        [MenuItem("Bus Puzzle/Ads/Validate AdMob Release Settings")]
        private static void ValidateCurrentSettings()
        {
            var settings = Resources.Load<AdMobSettings>(AdMobSettings.ResourcePath);
            if (settings == null)
            {
                Debug.LogError("AdMobSettings asset is missing.");
                return;
            }

            Debug.Log(
                "AdMob settings loaded. " +
                $"Android app: {settings.AndroidAppId}, Android station rewarded: {settings.AndroidRewardedProductionAdUnitId}, " +
                $"Android VIP rewarded: {settings.AndroidVipRewardedProductionAdUnitId}, " +
                $"Android shuffle rewarded: {settings.AndroidShuffleRewardedProductionAdUnitId}, " +
                $"iOS app: {settings.IosAppId}, iOS station rewarded: {settings.IosRewardedProductionAdUnitId}, " +
                $"iOS VIP rewarded: {settings.IosVipRewardedProductionAdUnitId}, " +
                $"iOS shuffle rewarded: {settings.IosShuffleRewardedProductionAdUnitId}");
        }

        private static bool IsDevelopmentBuild(BuildReport report)
        {
            return (report.summary.options & BuildOptions.Development) != 0;
        }

        private static bool IsMobileTarget(BuildTarget target)
        {
            return target == BuildTarget.Android || target == BuildTarget.iOS;
        }

        private static string GetPlatformAppId(AdMobSettings settings, BuildTarget target)
        {
            return target == BuildTarget.iOS ? settings.IosAppId : settings.AndroidAppId;
        }

        private static string GetPlatformProductionRewardedAdUnitId(
            AdMobSettings settings,
            BuildTarget target,
            RewardedAdPlacement placement)
        {
            if (placement == RewardedAdPlacement.VipBusTeleport)
            {
                return target == BuildTarget.iOS
                    ? settings.IosVipRewardedProductionAdUnitId
                    : settings.AndroidVipRewardedProductionAdUnitId;
            }

            if (placement == RewardedAdPlacement.BusColorShuffle)
            {
                return target == BuildTarget.iOS
                    ? settings.IosShuffleRewardedProductionAdUnitId
                    : settings.AndroidShuffleRewardedProductionAdUnitId;
            }

            return target == BuildTarget.iOS
                ? settings.IosRewardedProductionAdUnitId
                : settings.AndroidRewardedProductionAdUnitId;
        }

        private static void ValidateProductionRewardedAdUnitId(BuildTarget target, string label, string adUnitId)
        {
            if (!AdMobSettings.LooksLikeAdUnitId(adUnitId))
            {
                throw new BuildFailedException($"Rewarded ad unit ID is missing or invalid for {target} {label}: {adUnitId}");
            }

            if (AdMobSettings.IsGoogleTestAdUnitId(adUnitId))
            {
                throw new BuildFailedException($"Release build is still configured with Google's rewarded test ad unit ID for {label}.");
            }
        }

        private static bool HasAdMobScriptingDefine(BuildTarget target)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            return symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Contains(AdMobScriptingDefine);
        }

        private static bool HasAdMobCompilerDefine(BuildTarget target)
        {
            return HasAdMobScriptingDefine(target) || HasCompilerResponseDefine("Assets/csc.rsp");
        }

        private static bool HasCompilerResponseDefine(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var contents = File.ReadAllText(path);
            return contents.Contains($"-define:{AdMobScriptingDefine}", StringComparison.Ordinal) ||
                contents.Contains($"-d:{AdMobScriptingDefine}", StringComparison.Ordinal);
        }
    }
}
