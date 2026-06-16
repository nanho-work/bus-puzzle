#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BusPuzzle
{
    public static class AndroidReleaseAabBuilder
    {
        private const string StorePassEnv = "BUSPOP_ANDROID_STORE_PASS";
        private const string KeyPassEnv = "BUSPOP_ANDROID_KEY_PASS";
        private const string OutputDirectory = "Build/Android";
        private const string RootUploadOutputPath = "buspop.aab";
        private const string KeystorePath = "Build/Signing/buspop-upload-key.jks";
        private const string KeyAlias = "buspop-upload";

        [MenuItem("Bus Puzzle/Release/Build Android AAB")]
        private static void BuildReleaseAabFromMenu()
        {
            BuildReleaseAab();
        }

        public static void BuildReleaseAab()
        {
            var storePass = ReadSigningCredential(StorePassEnv, PlayerSettings.Android.keystorePass, "Android keystore password");
            var keyPass = ReadSigningCredential(KeyPassEnv, PlayerSettings.Android.keyaliasPass, "Android key alias password");
            if (!File.Exists(KeystorePath))
            {
                throw new FileNotFoundException("Android upload keystore is missing.", KeystorePath);
            }

            Directory.CreateDirectory(OutputDirectory);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = true;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            PlayerSettings.Android.keystorePass = storePass;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = keyPass;

            var outputPath = GetOutputPath();
            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Android release AAB build failed: {summary.result}, errors {summary.totalErrors}");
            }

            File.Copy(outputPath, RootUploadOutputPath, true);
            UnityEngine.Debug.Log($"Android release AAB built: {outputPath} ({summary.totalSize} bytes)");
            UnityEngine.Debug.Log($"Android release AAB copied: {RootUploadOutputPath}");
        }

        private static string GetOutputPath()
        {
            return Path.Combine(
                OutputDirectory,
                $"BusPop-{PlayerSettings.bundleVersion}-{PlayerSettings.Android.bundleVersionCode}.aab");
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
                throw new BuildFailedException("No enabled scenes are configured in Build Settings.");
            }

            Array.Resize(ref enabledScenes, count);
            return enabledScenes;
        }

        private static string ReadSigningCredential(string environmentVariableName, string playerSettingsValue, string label)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            if (!string.IsNullOrEmpty(playerSettingsValue))
            {
                return playerSettingsValue;
            }

            throw new BuildFailedException(
                $"Missing {label}. Set {environmentVariableName} or enter the password in Unity Player Settings.");
        }
    }
}
#endif
