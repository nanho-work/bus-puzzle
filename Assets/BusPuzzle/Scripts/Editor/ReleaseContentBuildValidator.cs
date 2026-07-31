#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class ReleaseContentBuildValidator : IPreprocessBuildWithReport
    {
        private const string GeneratedLevelSequenceResourcePath = "Levels/Generated/GeneratedLevelSequence";
        private const string StageGenerationConfigResourcePath = "Levels/StageGenerationConfig";
        private const string HeartShapeTemplateResourcePath = "VehicleShapeTemplates/Heart";
        private const string HeartShapeTemplateAssetPath =
            "Assets/BusPuzzle/Resources/VehicleShapeTemplates/Heart.asset";
        private const string GeneratedLevelDirectory = "Assets/BusPuzzle/Resources/Levels/Generated";
        private const string GeneratedLevelSequenceAssetPath = GeneratedLevelDirectory + "/GeneratedLevelSequence.asset";
        private const string ActiveLevelSequenceAssetPath = "Assets/BusPuzzle/Resources/Levels/LevelSequence.asset";
        private const string ShapePreviewDirectory = "Assets/BusPuzzle/ShapePreview";
        private const string ShapePreviewSignaturePrefix = "previewOnly=shape;";
        private const string ManualShapeSignatureToken = "manualShape=";
        private const string ReleaseLevelManifestPath =
            "Assets/BusPuzzle/Release/ReleaseLevelManifest.txt";
        public const int RequiredReleaseStageCount = 200;

        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null)
            {
                return;
            }

            ValidateReleaseContentOrThrow();
        }

        public static void ValidateReleaseContentOrThrow()
        {
            ValidateRequiredHeartShapeTemplateOrThrow();

            var generatedSequence = Resources.Load<LevelSequence>(GeneratedLevelSequenceResourcePath);
            if (generatedSequence == null)
            {
                throw new BuildFailedException(
                    "Verified generated level sequence is missing. " +
                    "Run Bus Puzzle/Levels/Restore Release Sequences From Existing Generated Levels before release builds.");
            }

            var stageGenerationConfig = Resources.Load<StageGenerationConfig>(StageGenerationConfigResourcePath);
            if (stageGenerationConfig == null)
            {
                throw new BuildFailedException(
                    "StageGenerationConfig is missing. Release content cannot be validated against defaults.");
            }

            if (stageGenerationConfig.GeneratedStageCount != RequiredReleaseStageCount)
            {
                throw new BuildFailedException(
                    $"This release requires exactly {RequiredReleaseStageCount} prebuilt stages, but " +
                    $"StageGenerationConfig declares {stageGenerationConfig.GeneratedStageCount}.");
            }

            ValidateSequenceOrThrow(
                generatedSequence,
                GeneratedLevelSequenceAssetPath,
                "generated release sequence",
                stageGenerationConfig,
                true);

            var activeSequence = AssetDatabase.LoadAssetAtPath<LevelSequence>(ActiveLevelSequenceAssetPath);
            ValidateSequenceOrThrow(
                activeSequence,
                ActiveLevelSequenceAssetPath,
                "active release sequence",
                stageGenerationConfig,
                false);

            if (activeSequence.Count != generatedSequence.Count)
            {
                throw new BuildFailedException(
                    $"Active release sequence contains {activeSequence.Count} stages while the generated release sequence contains " +
                    $"{generatedSequence.Count}. Restore both release sequences from the existing generated levels.");
            }

            for (var index = 0; index < generatedSequence.Count; index++)
            {
                if (generatedSequence.GetLevel(index) != activeSequence.GetLevel(index))
                {
                    throw new BuildFailedException(
                        $"Active and generated release sequences differ at stage {index + 1:000}. " +
                        "Restore both release sequences from the existing generated levels.");
                }
            }

            ValidateReleaseLevelManifestOrThrow();
            // Shape quality is a generated-content contract, not a manual preview task.
            // Exercise both budget extremes and prove the preserved bad fixture remains
            // rejected before any player build can leave the editor.
            EditorTools.VehicleShapeTemplateAudit.ValidateAutomaticPositiveHeartStageFromCommandLine();
            EditorTools.VehicleShapeTemplateAudit.ValidateBadHeartBaselineIsRejectedFromCommandLine();
            EditorTools.RuntimeStageRegressionValidator.ValidateRuntimeStageContinuity();
            Debug.Log(
                $"Release content validation passed: {RequiredReleaseStageCount} locked pre-shape levels, " +
                "both release sequences match, and all content hashes are unchanged.");
        }

        private static void ValidateRequiredHeartShapeTemplateOrThrow()
        {
            var template = Resources.Load<VehicleShapeTemplate>(HeartShapeTemplateResourcePath);
            if (template == null || !template.IsUsable)
            {
                throw new BuildFailedException(
                    $"Required Heart quality template is missing or unusable: {HeartShapeTemplateAssetPath}.");
            }

            if (!template.Constraints.EnableSilhouetteGate)
            {
                throw new BuildFailedException(
                    $"Required Heart quality template has its silhouette gate disabled: " +
                    $"{HeartShapeTemplateAssetPath}.");
            }
        }

        public static bool TryValidateReleaseLevelReference(
            LevelData level,
            int stageNumber,
            StageGenerationConfig config,
            out string message)
        {
            message = string.Empty;
            var expectedPath = $"{GeneratedLevelDirectory}/Level_{stageNumber:000}.asset";
            if (level == null)
            {
                message = $"Release stage {stageNumber:000} is missing; expected {expectedPath}.";
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(level);
            if (!string.Equals(assetPath, expectedPath, StringComparison.Ordinal))
            {
                message =
                    $"Release stage {stageNumber:000} references {assetPath}, but only {expectedPath} is allowed. " +
                    "Shape preview assets must never be referenced by a release sequence.";
                return false;
            }

            if (assetPath.StartsWith(ShapePreviewDirectory + "/", StringComparison.Ordinal))
            {
                message = $"Release stage {stageNumber:000} references isolated shape preview content: {assetPath}.";
                return false;
            }

            var signature = level.GenerationSignature ?? string.Empty;
            if (IsPreviewOnlySignature(signature))
            {
                message =
                    $"Release stage {stageNumber:000} contains a preview-only generation signature: {signature}. " +
                    "Restore the production Level asset before building.";
                return false;
            }

            if (!StageGenerationSignature.TryGetInt(signature, "signature", out _))
            {
                message = $"Release stage {stageNumber:000} does not contain a production generation signature.";
                return false;
            }

            if (!StageGenerationSignature.TryGetInt(signature, "stage", out var signatureStage) ||
                signatureStage != stageNumber)
            {
                message =
                    $"Release stage {stageNumber:000} has generation signature stage {signatureStage:000}; " +
                    "the asset and sequence index do not match.";
                return false;
            }

            if (!StageGenerationSignature.TryGetInt(signature, "stageCount", out var signatureStageCount) ||
                signatureStageCount != RequiredReleaseStageCount)
            {
                message =
                    $"Release stage {stageNumber:000} was generated for {signatureStageCount} stages, while " +
                    $"this release requires {RequiredReleaseStageCount}.";
                return false;
            }

            return true;
        }

        public static bool IsPreviewOnlySignature(string signature)
        {
            if (string.IsNullOrEmpty(signature))
            {
                return false;
            }

            return signature.IndexOf(ShapePreviewSignaturePrefix, StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("previewOnly=generated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf(ManualShapeSignatureToken, StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("shapePreview=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("previewShape=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                signature.IndexOf("templatePreview=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                StageGenerationSignature.TryGetInt(signature, "layoutVariant", out var layoutVariant) &&
                layoutVariant < 0;
        }

        private static void ValidateSequenceOrThrow(
            LevelSequence sequence,
            string expectedSequencePath,
            string label,
            StageGenerationConfig config,
            bool validateLevels)
        {
            if (sequence == null)
            {
                throw new BuildFailedException($"The {label} is missing: {expectedSequencePath}.");
            }

            var sequencePath = AssetDatabase.GetAssetPath(sequence);
            if (!string.Equals(sequencePath, expectedSequencePath, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"The {label} resolved to {sequencePath}, but release builds require {expectedSequencePath}.");
            }

            if (!sequence.IsVerifiedGeneratedSet)
            {
                throw new BuildFailedException($"The {label} is not marked as a verified generated set.");
            }

            if (sequence.Count != RequiredReleaseStageCount)
            {
                throw new BuildFailedException(
                    $"The {label} contains {sequence.Count} stages; exactly " +
                    $"{RequiredReleaseStageCount} are required.");
            }

            if (config == null)
            {
                throw new BuildFailedException(
                    $"The {label} cannot be validated because StageGenerationConfig is missing.");
            }

            if (sequence.Count != config.GeneratedStageCount)
            {
                throw new BuildFailedException(
                    $"The {label} contains {sequence.Count} prebuilt stages, while StageGenerationConfig expects " +
                    $"{config.GeneratedStageCount}. Restore the release sequences from all existing generated levels.");
            }

            var referencedPaths = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < sequence.Count; index++)
            {
                var level = sequence.GetLevel(index);
                if (!TryValidateReleaseLevelReference(level, index + 1, config, out var referenceFailure))
                {
                    throw new BuildFailedException($"The {label} is invalid. {referenceFailure}");
                }

                var levelPath = AssetDatabase.GetAssetPath(level);
                if (!referencedPaths.Add(levelPath))
                {
                    throw new BuildFailedException(
                        $"The {label} references the same Level asset more than once: {levelPath}.");
                }

                if (!validateLevels)
                {
                    continue;
                }

                var validationReport = LevelValidator.Validate(level, true);
                if (validationReport.HasErrors)
                {
                    throw new BuildFailedException(validationReport.ToConsoleMessage(level.LevelName));
                }
            }
        }

        private static void ValidateReleaseLevelManifestOrThrow()
        {
            if (!File.Exists(ReleaseLevelManifestPath))
            {
                throw new BuildFailedException(
                    $"Immutable release manifest is missing: {ReleaseLevelManifestPath}.");
            }

            var expectedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            var lines = File.ReadAllLines(ReleaseLevelManifestPath);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex].Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf(' ');
                if (separatorIndex <= 0 || separatorIndex >= line.Length - 1)
                {
                    throw new BuildFailedException(
                        $"Malformed release manifest line {lineIndex + 1}: '{line}'.");
                }

                var assetName = line.Substring(0, separatorIndex);
                var hash = line.Substring(separatorIndex + 1).Trim();
                if (hash.Length != 64 || !expectedHashes.TryAdd(assetName, hash))
                {
                    throw new BuildFailedException(
                        $"Invalid or duplicate release manifest entry at line {lineIndex + 1}: '{line}'.");
                }
            }

            if (expectedHashes.Count != RequiredReleaseStageCount)
            {
                throw new BuildFailedException(
                    $"Release manifest contains {expectedHashes.Count} entries; expected " +
                    $"{RequiredReleaseStageCount}.");
            }

            for (var stageNumber = 1; stageNumber <= RequiredReleaseStageCount; stageNumber++)
            {
                var assetName = $"Level_{stageNumber:000}.asset";
                var assetPath = $"{GeneratedLevelDirectory}/{assetName}";
                if (!expectedHashes.TryGetValue(assetName, out var expectedHash))
                {
                    throw new BuildFailedException(
                        $"Release manifest has no entry for {assetName}.");
                }

                var actualHash = ComputeSha256(assetPath);
                if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BuildFailedException(
                        $"Release stage {stageNumber:000} content differs from the locked pre-shape release " +
                        $"manifest ({actualHash} != {expectedHash}). Restore the production asset before building.");
                }
            }
        }

        private static string ComputeSha256(string path)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = File.ReadAllBytes(path);
                return BitConverter.ToString(sha256.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

    }
}
#endif
