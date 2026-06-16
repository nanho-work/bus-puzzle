using System.Globalization;
using System.Text;

namespace BusPuzzle
{
    public static class StageGenerationSignature
    {
        private const int SignatureVersion = 3;

        public static string Create(StageGenerationConfig config, StageGenerationRequest request)
        {
            var profile = request.Profile ?? LevelDifficultyProfile.DefaultFor(request.Difficulty);
            var passengerRule = profile.PassengerFlowRule;
            var garageRule = config != null ? config.SuperHardGarageRule : new GarageGenerationRule();
            var builder = new StringBuilder(256);

            Append(builder, "signature", SignatureVersion);
            Append(builder, "stage", request.StageNumber);
            Append(builder, "seed", request.Seed);
            Append(builder, "difficulty", (int)request.Difficulty);
            Append(builder, "modifiers", (int)request.Modifiers);
            Append(builder, "progress", request.Progress);
            Append(builder, "post50", request.Post50Pressure);
            Append(builder, "road", (int)request.RoadPresetId);
            Append(builder, "layoutVariant", request.VehicleLayoutVariantIndex);
            Append(builder, "layoutPool", request.VehicleLayoutVariantPoolSize);
            Append(builder, "garages", request.GarageCount);
            Append(builder, "garageQueueMin", request.MinGarageQueuedVehicles);
            Append(builder, "garageQueueMax", request.MaxGarageQueuedVehicles);
            Append(builder, "rotary", request.RotaryCapacity);
            Append(builder, "mysteryEnabled", request.MysteryVehicleProfile.Enabled ? 1 : 0);
            Append(builder, "mysteryMin", request.MysteryVehicleProfile.MinVehicles);
            Append(builder, "mysteryMax", request.MysteryVehicleProfile.MaxVehicles);
            Append(builder, "mysteryRatio", request.MysteryVehicleProfile.Ratio);
            Append(builder, "minSolutions", request.MinSolutionCount);
            Append(builder, "maxSolutions", request.MaxSolutionCount);
            Append(builder, "vehicles", profile.TargetVehicleCount);
            Append(builder, "colors", profile.TargetColorCount);
            Append(builder, "parking", profile.ParkingTension);
            Append(builder, "station", profile.StationPressure);
            Append(builder, "route", profile.RequireSolutionRoute ? 1 : 0);
            Append(builder, "flowMin", passengerRule.MinMainGroupRatio);
            Append(builder, "flowMax", passengerRule.MaxMainGroupRatio);
            Append(builder, "groupMin", passengerRule.MinGroupUnits);
            Append(builder, "groupMax", passengerRule.MaxGroupUnits);
            Append(builder, "interference", passengerRule.InterferenceRatio);
            Append(builder, "preserve", passengerRule.PreserveSolutionRoute ? 1 : 0);
            Append(builder, "garageEnabled", garageRule.Enabled ? 1 : 0);
            Append(builder, "garageRuleQueueMin", garageRule.MinQueuedVehiclesPerGarage);
            Append(builder, "garageRuleQueueMax", garageRule.MaxQueuedVehiclesPerGarage);
            Append(builder, "garageRulePostQueueMin", garageRule.Post50MinQueuedVehiclesPerGarage);
            Append(builder, "garageRulePostQueueMax", garageRule.Post50MaxQueuedVehiclesPerGarage);

            if (config != null)
            {
                Append(builder, "stageCount", config.GeneratedStageCount);
                Append(builder, "rampStart", config.DifficultyRampStartStage);
                Append(builder, "rampReference", config.DifficultyRampReferenceStage);
                Append(builder, "rampMax", config.DifficultyRampMaxStage);
                Append(builder, "postRampMax", config.Post50RampMaxStage);
                Append(builder, "baseSeed", config.BaseSeed);
                Append(builder, "releaseVehicleAttempts", config.ReleaseVehicleGenerationAttempts);
                Append(builder, "solutionLimit", config.SolutionCountLimit);
            }

            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string key, int value)
        {
            builder.Append(key).Append('=').Append(value).Append(';');
        }

        private static void Append(StringBuilder builder, string key, float value)
        {
            builder.Append(key)
                .Append('=')
                .Append(value.ToString("0.####", CultureInfo.InvariantCulture))
                .Append(';');
        }
    }
}
