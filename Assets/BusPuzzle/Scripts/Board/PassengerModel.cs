using UnityEngine;

namespace BusPuzzle
{
    internal sealed class PassengerModel
    {
        private readonly Transform[] personRoots;
        private readonly Vector3[] defaultPersonLocalPositions;
        private readonly Transform[] leftLegs;
        private readonly Transform[] rightLegs;
        private readonly Transform[] leftLowerLegs;
        private readonly Transform[] rightLowerLegs;
        private readonly Transform[] leftFeet;
        private readonly Transform[] rightFeet;
        private readonly Transform[] leftArms;
        private readonly Transform[] rightArms;
        private readonly Quaternion[] defaultLeftLegLocalRotations;
        private readonly Quaternion[] defaultRightLegLocalRotations;
        private readonly Quaternion[] defaultLeftLowerLegLocalRotations;
        private readonly Quaternion[] defaultRightLowerLegLocalRotations;
        private readonly Quaternion[] defaultLeftFootLocalRotations;
        private readonly Quaternion[] defaultRightFootLocalRotations;
        private readonly Quaternion[] defaultLeftArmLocalRotations;
        private readonly Quaternion[] defaultRightArmLocalRotations;
        private readonly bool swingLegsAroundLocalX;

        public PassengerModel(
            Transform[] personRoots,
            Vector3[] defaultPersonLocalPositions,
            Transform[] leftLegs,
            Transform[] rightLegs,
            bool swingLegsAroundLocalX = false,
            Transform[] leftLowerLegs = null,
            Transform[] rightLowerLegs = null,
            Transform[] leftFeet = null,
            Transform[] rightFeet = null,
            Transform[] leftArms = null,
            Transform[] rightArms = null)
        {
            this.personRoots = personRoots;
            this.defaultPersonLocalPositions = defaultPersonLocalPositions;
            this.leftLegs = leftLegs;
            this.rightLegs = rightLegs;
            this.leftLowerLegs = leftLowerLegs;
            this.rightLowerLegs = rightLowerLegs;
            this.leftFeet = leftFeet;
            this.rightFeet = rightFeet;
            this.leftArms = leftArms;
            this.rightArms = rightArms;
            this.swingLegsAroundLocalX = swingLegsAroundLocalX;
            defaultLeftLegLocalRotations = CaptureLocalRotations(leftLegs);
            defaultRightLegLocalRotations = CaptureLocalRotations(rightLegs);
            defaultLeftLowerLegLocalRotations = CaptureLocalRotations(leftLowerLegs);
            defaultRightLowerLegLocalRotations = CaptureLocalRotations(rightLowerLegs);
            defaultLeftFootLocalRotations = CaptureLocalRotations(leftFeet);
            defaultRightFootLocalRotations = CaptureLocalRotations(rightFeet);
            defaultLeftArmLocalRotations = CaptureLocalRotations(leftArms);
            defaultRightArmLocalRotations = CaptureLocalRotations(rightArms);
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
            var count = GetLargestTransformArrayLength(leftLegs, rightLegs, leftLowerLegs, rightLowerLegs, leftFeet, rightFeet, leftArms, rightArms);
            if (count <= 0)
            {
                return;
            }

            for (var index = 0; index < count; index++)
            {
                var offsetSwing = index % 2 == 0 ? swing : -swing;
                ApplyLegSwing(leftLegs, defaultLeftLegLocalRotations, index, offsetSwing);
                ApplyLegSwing(rightLegs, defaultRightLegLocalRotations, index, -offsetSwing);
                ApplyLegSwing(leftLowerLegs, defaultLeftLowerLegLocalRotations, index, -offsetSwing * 0.52f);
                ApplyLegSwing(rightLowerLegs, defaultRightLowerLegLocalRotations, index, offsetSwing * 0.52f);
                ApplyLegSwing(leftFeet, defaultLeftFootLocalRotations, index, offsetSwing * 0.36f);
                ApplyLegSwing(rightFeet, defaultRightFootLocalRotations, index, -offsetSwing * 0.36f);
                ApplyArmSwing(leftArms, defaultLeftArmLocalRotations, index, -offsetSwing * 0.26f);
                ApplyArmSwing(rightArms, defaultRightArmLocalRotations, index, offsetSwing * 0.26f);
            }
        }

        public void ResetWalkCycle()
        {
            ResetLegRotations(leftLegs, defaultLeftLegLocalRotations);
            ResetLegRotations(rightLegs, defaultRightLegLocalRotations);
            ResetLegRotations(leftLowerLegs, defaultLeftLowerLegLocalRotations);
            ResetLegRotations(rightLowerLegs, defaultRightLowerLegLocalRotations);
            ResetLegRotations(leftFeet, defaultLeftFootLocalRotations);
            ResetLegRotations(rightFeet, defaultRightFootLocalRotations);
            ResetLegRotations(leftArms, defaultLeftArmLocalRotations);
            ResetLegRotations(rightArms, defaultRightArmLocalRotations);
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

        private void ApplyLegSwing(Transform[] transforms, Quaternion[] rotations, int index, float swing)
        {
            if (transforms == null || index < 0 || index >= transforms.Length || transforms[index] == null)
            {
                return;
            }

            transforms[index].localRotation = GetDefaultLegRotation(rotations, index) * GetLegSwingRotation(swing);
        }

        private void ApplyArmSwing(Transform[] transforms, Quaternion[] rotations, int index, float swing)
        {
            if (transforms == null || index < 0 || index >= transforms.Length || transforms[index] == null)
            {
                return;
            }

            transforms[index].localRotation = GetDefaultLegRotation(rotations, index) * Quaternion.Euler(0f, swing, 0f);
        }

        private static int GetLargestTransformArrayLength(params Transform[][] arrays)
        {
            var length = 0;
            if (arrays == null)
            {
                return length;
            }

            for (var index = 0; index < arrays.Length; index++)
            {
                if (arrays[index] != null && arrays[index].Length > length)
                {
                    length = arrays[index].Length;
                }
            }

            return length;
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
