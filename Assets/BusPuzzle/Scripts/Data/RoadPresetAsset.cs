using UnityEngine;

namespace BusPuzzle
{
    [CreateAssetMenu(menuName = "Bus Puzzle/Road Preset", fileName = "RoadPreset")]
    public sealed class RoadPresetAsset : ScriptableObject
    {
        [SerializeField] private RotaryRoadPresetId id = RotaryRoadPresetId.Large;
        [SerializeField] private int maxCapacityUnits = LevelData.MaxRotaryUnitCapacity;
        [SerializeField] private float passengerSpeed = 0.029f;
        [SerializeField] private Vector2 start = new Vector2(0f, -0.82f);
        [SerializeField] private Vector2 rightBottom = new Vector2(0.66f, -0.82f);
        [SerializeField] private Vector2 rightTop = new Vector2(0.88f, 0.72f);
        [SerializeField] private Vector2 leftTop = new Vector2(-0.88f, 0.72f);
        [SerializeField] private Vector2 leftBottom = new Vector2(-0.66f, -0.82f);
        [SerializeField] private Vector2 rightControl = new Vector2(1.38f, -0.10f);
        [SerializeField] private Vector2 topControl = new Vector2(0f, 1.22f);
        [SerializeField] private Vector2 leftControl = new Vector2(-1.38f, -0.10f);
        [SerializeField] private int bottomSegments = 20;
        [SerializeField] private int sideSegments = 56;
        [SerializeField] private int topSegments = 66;
        [SerializeField] private float roadShoulder = 0.026f;
        [SerializeField] private float stationConnectProgress = 0f;
        [SerializeField] private float leftFeederProgress = 0.71f;
        [SerializeField] private float rightFeederProgress = 0.29f;
        [SerializeField] private float feederRowSpacing = 0.14f;
        [SerializeField] private int feederRowsPerStack = 12;

        public RoadPresetDefinition ToDefinition()
        {
            return new RoadPresetDefinition(
                id,
                Mathf.Clamp(maxCapacityUnits, LevelData.MinRotaryUnitCapacity, LevelData.MaxRotaryUnitCapacity),
                Mathf.Max(0.001f, passengerSpeed),
                start,
                rightBottom,
                rightTop,
                leftTop,
                leftBottom,
                rightControl,
                topControl,
                leftControl,
                Mathf.Max(2, bottomSegments),
                Mathf.Max(2, sideSegments),
                Mathf.Max(2, topSegments),
                Mathf.Max(0f, roadShoulder),
                stationConnectProgress,
                leftFeederProgress,
                rightFeederProgress,
                Mathf.Max(0.04f, feederRowSpacing),
                Mathf.Max(1, feederRowsPerStack));
        }
    }
}
