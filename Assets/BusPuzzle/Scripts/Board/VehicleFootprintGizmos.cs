using UnityEngine;

namespace BusPuzzle
{
    internal static class VehicleFootprintGizmos
    {
#if UNITY_EDITOR
        private const float FootprintY = 0.055f;
        private const float ForwardLineLengthFactor = 0.70f;

        public static void Draw(BusView bus, bool selected)
        {
            if (!Application.isPlaying || bus == null || !bus.isActiveAndEnabled || bus.IsDeparted)
            {
                return;
            }

            var vehicleColor = PuzzlePalette.ToColor(bus.Color);
            vehicleColor.a = selected ? 0.95f : 0.42f;
            DrawFootprint(bus.CurrentFootprint, vehicleColor);
            DrawForward(bus.CurrentFootprint, vehicleColor, selected);
        }

        private static void DrawFootprint(VehicleFootprint footprint, Color color)
        {
            var previousColor = Gizmos.color;
            Gizmos.color = color;

            var a = footprint.GetCorner(0, FootprintY);
            var b = footprint.GetCorner(1, FootprintY);
            var c = footprint.GetCorner(2, FootprintY);
            var d = footprint.GetCorner(3, FootprintY);

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            Gizmos.DrawLine(a, c);
            Gizmos.color = previousColor;
        }

        private static void DrawForward(VehicleFootprint footprint, Color color, bool selected)
        {
            var previousColor = Gizmos.color;
            Gizmos.color = selected ? Color.white : color;

            var center = footprint.GetCenter(FootprintY);
            var forward = new Vector3(footprint.Forward.x, 0f, footprint.Forward.y);
            var right = new Vector3(footprint.Right.x, 0f, footprint.Right.y);
            var lineLength = footprint.HalfLength * ForwardLineLengthFactor;
            var tip = center + forward * lineLength;

            Gizmos.DrawLine(center, tip);
            Gizmos.DrawLine(tip, tip - forward * lineLength * 0.28f + right * footprint.HalfWidth * 0.30f);
            Gizmos.DrawLine(tip, tip - forward * lineLength * 0.28f - right * footprint.HalfWidth * 0.30f);
            Gizmos.color = previousColor;
        }
#endif
    }
}
