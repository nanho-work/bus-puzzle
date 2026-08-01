#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BusPuzzle
{
    public static class IosReleaseProjectBuilder
    {
        private const string OutputRoot = "Build";

        [MenuItem("Bus Puzzle/Release/Build iOS Xcode Project")]
        private static void BuildReleaseXcodeProjectFromMenu()
        {
            BuildReleaseXcodeProject();
        }

        public static void BuildReleaseXcodeProject()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.iOS,
                BuildTarget.iOS);

            var outputPath = GetOutputPath();
            if (Directory.Exists(outputPath) &&
                Directory.GetFileSystemEntries(outputPath).Length > 0)
            {
                throw new BuildFailedException(
                    $"iOS release output already exists and is not empty: {outputPath}. " +
                    "Use a new build number or archive the previous output before rebuilding.");
            }

            Directory.CreateDirectory(outputPath);
            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"iOS release Xcode export failed: {summary.result}, " +
                    $"errors {summary.totalErrors}.");
            }

            UnityEngine.Debug.Log(
                $"iOS release Xcode project built: {outputPath} " +
                $"({summary.totalSize} bytes).");
        }

        private static string GetOutputPath()
        {
            return Path.Combine(
                OutputRoot,
                $"IOS-{PlayerSettings.bundleVersion}-{PlayerSettings.iOS.buildNumber}");
        }

        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var enabledScenes = new string[scenes.Length];
            var count = 0;
            for (var index = 0; index < scenes.Length; index++)
            {
                if (!scenes[index].enabled)
                {
                    continue;
                }

                enabledScenes[count++] = scenes[index].path;
            }

            if (count == 0)
            {
                throw new BuildFailedException(
                    "No enabled scenes are configured in Build Settings.");
            }

            Array.Resize(ref enabledScenes, count);
            return enabledScenes;
        }
    }
}
#endif
