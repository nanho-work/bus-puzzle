using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerModel
    {
        private readonly Transform[] personRoots;
        private readonly Vector3[] defaultPersonLocalPositions;
        private readonly Transform[] leftLegs;
        private readonly Transform[] rightLegs;
        private readonly Quaternion[] defaultLeftLegLocalRotations;
        private readonly Quaternion[] defaultRightLegLocalRotations;
        private readonly bool swingLegsAroundLocalX;

        public PassengerModel(
            Transform[] personRoots,
            Vector3[] defaultPersonLocalPositions,
            Transform[] leftLegs,
            Transform[] rightLegs,
            bool swingLegsAroundLocalX = false)
        {
            this.personRoots = personRoots;
            this.defaultPersonLocalPositions = defaultPersonLocalPositions;
            this.leftLegs = leftLegs;
            this.rightLegs = rightLegs;
            this.swingLegsAroundLocalX = swingLegsAroundLocalX;
            defaultLeftLegLocalRotations = CaptureLocalRotations(leftLegs);
            defaultRightLegLocalRotations = CaptureLocalRotations(rightLegs);
        }

        public int PersonCount => personRoots != null ? personRoots.Length : 0;

        public Transform GetPersonRoot(int index)
        {
            return IsValidPersonIndex(index) ? personRoots[index] : null;
        }

        public bool IsValidPersonIndex(int index)
        {
            return personRoots != null &&
                index >= 0 &&
                index < personRoots.Length &&
                personRoots[index] != null;
        }

        public void ApplyWalkCycle(float swing)
        {
            if (leftLegs == null || rightLegs == null)
            {
                return;
            }

            for (var index = 0; index < leftLegs.Length; index++)
            {
                var offsetSwing = index % 2 == 0 ? swing : -swing;
                if (leftLegs[index] != null)
                {
                    leftLegs[index].localRotation = GetDefaultLegRotation(defaultLeftLegLocalRotations, index) * GetLegSwingRotation(offsetSwing);
                }

                if (rightLegs[index] != null)
                {
                    rightLegs[index].localRotation = GetDefaultLegRotation(defaultRightLegLocalRotations, index) * GetLegSwingRotation(-offsetSwing);
                }
            }
        }

        public void ResetWalkCycle()
        {
            ResetLegRotations(leftLegs, defaultLeftLegLocalRotations);
            ResetLegRotations(rightLegs, defaultRightLegLocalRotations);
        }

        public void ApplyPosePersonLocalPositions(PassengerUnitRoadPose pose)
        {
            if (!pose.HasCustomPersonLocalPositions)
            {
                ApplyDefaultPersonLocalPositions();
                return;
            }

            for (var index = 0; index < PersonCount; index++)
            {
                var root = GetPersonRoot(index);
                if (root != null)
                {
                    root.localPosition = GetPosePersonLocalPosition(pose, index);
                }
            }
        }

        public void ApplyInterpolatedPersonLocalPositions(PassengerUnitRoadPose startPose, PassengerUnitRoadPose endPose, float t)
        {
            for (var index = 0; index < PersonCount; index++)
            {
                var root = GetPersonRoot(index);
                if (root == null)
                {
                    continue;
                }

                root.localPosition = Vector3.Lerp(
                    GetPosePersonLocalPosition(startPose, index),
                    GetPosePersonLocalPosition(endPose, index),
                    t);
            }
        }

        public void ApplyDefaultPersonLocalPositions()
        {
            for (var index = 0; index < PersonCount; index++)
            {
                var root = GetPersonRoot(index);
                if (root != null)
                {
                    root.localPosition = GetDefaultPersonLocalPosition(index);
                }
            }
        }

        public Vector3 GetPosePersonLocalPosition(PassengerUnitRoadPose pose, int index)
        {
            if (!pose.HasCustomPersonLocalPositions)
            {
                return GetDefaultPersonLocalPosition(index);
            }

            switch (index)
            {
                case 0:
                    return pose.Person1LocalPosition;
                case 1:
                    return pose.Person2LocalPosition;
                case 2:
                    return pose.Person3LocalPosition;
                default:
                    return pose.Person4LocalPosition;
            }
        }

        public Vector3 GetDefaultPersonLocalPosition(int index)
        {
            if (defaultPersonLocalPositions == null || defaultPersonLocalPositions.Length == 0)
            {
                return Vector3.zero;
            }

            return defaultPersonLocalPositions[Mathf.Clamp(index, 0, defaultPersonLocalPositions.Length - 1)];
        }

        public float GetPersonDistanceToEntry(int index, Vector3 entryPosition)
        {
            var root = GetPersonRoot(index);
            return root != null ? Vector3.SqrMagnitude(root.position - entryPosition) : float.MaxValue;
        }

        private Quaternion GetLegSwingRotation(float swing)
        {
            return swingLegsAroundLocalX
                ? Quaternion.Euler(swing, 0f, 0f)
                : Quaternion.Euler(0f, 0f, swing);
        }

        private static Quaternion[] CaptureLocalRotations(Transform[] transforms)
        {
            if (transforms == null)
            {
                return null;
            }

            var rotations = new Quaternion[transforms.Length];
            for (var index = 0; index < transforms.Length; index++)
            {
                rotations[index] = transforms[index] != null ? transforms[index].localRotation : Quaternion.identity;
            }

            return rotations;
        }

        private static Quaternion GetDefaultLegRotation(Quaternion[] rotations, int index)
        {
            return rotations != null && index >= 0 && index < rotations.Length
                ? rotations[index]
                : Quaternion.identity;
        }

        private static void ResetLegRotations(Transform[] transforms, Quaternion[] rotations)
        {
            if (transforms == null || rotations == null)
            {
                return;
            }

            for (var index = 0; index < transforms.Length && index < rotations.Length; index++)
            {
                if (transforms[index] != null)
                {
                    transforms[index].localRotation = rotations[index];
                }
            }
        }
    }
}
