using UnityEngine;
using UnityEngine.EventSystems;

namespace BusPuzzle
{
    internal sealed class GameInputController
    {
        private Camera gameCamera;

        public GameInputController(Camera gameCamera)
        {
            this.gameCamera = gameCamera;
        }

        public void SetCamera(Camera camera)
        {
            gameCamera = camera;
        }

        public bool TryTakeBusTap(out BusView bus)
        {
            bus = null;
            if (!TryGetPointerDown(out var screenPosition, out var pointerId) || IsPointerOverUi(pointerId))
            {
                return false;
            }

            bus = TryGetBusAtScreenPosition(screenPosition);
            return bus != null;
        }

        public bool TryTakeStationUnlockTap(out StationSlotUnlockTarget unlockTarget)
        {
            unlockTarget = null;
            if (!TryGetPointerDown(out var screenPosition, out var pointerId) || IsPointerOverUi(pointerId))
            {
                return false;
            }

            unlockTarget = TryGetStationUnlockTargetAtScreenPosition(screenPosition);
            return unlockTarget != null;
        }

        public bool IsPassengerFastForwardHeld()
        {
            if (!TryGetHeldPointer(out var screenPosition, out var pointerId) || IsPointerOverUi(pointerId))
            {
                return false;
            }

            return TryGetBusAtScreenPosition(screenPosition) == null;
        }

        private BusView TryGetBusAtScreenPosition(Vector2 screenPosition)
        {
            if (gameCamera == null)
            {
                return null;
            }

            var ray = gameCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 100f);
            BusView nearestBus = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < hits.Length; index++)
            {
                var hit = hits[index];
                var bus = hit.collider.GetComponentInParent<BusView>();
                if (bus != null && hit.distance < nearestDistance)
                {
                    nearestBus = bus;
                    nearestDistance = hit.distance;
                }
            }

            return nearestBus;
        }

        private StationSlotUnlockTarget TryGetStationUnlockTargetAtScreenPosition(Vector2 screenPosition)
        {
            if (gameCamera == null)
            {
                return null;
            }

            var ray = gameCamera.ScreenPointToRay(screenPosition);
            var hits = Physics.RaycastAll(ray, 100f);
            StationSlotUnlockTarget nearestTarget = null;
            var nearestDistance = float.PositiveInfinity;
            for (var index = 0; index < hits.Length; index++)
            {
                var hit = hits[index];
                var target = hit.collider.GetComponentInParent<StationSlotUnlockTarget>();
                if (target != null && hit.distance < nearestDistance)
                {
                    nearestTarget = target;
                    nearestDistance = hit.distance;
                }
            }

            return nearestTarget;
        }

        private static bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            screenPosition = Vector2.zero;
            pointerId = -1;
            return false;
        }

        private static bool TryGetHeldPointer(out Vector2 screenPosition, out int pointerId)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    screenPosition = touch.position;
                    pointerId = touch.fingerId;
                    return true;
                }
            }

            if (Input.GetMouseButton(0))
            {
                screenPosition = Input.mousePosition;
                pointerId = -1;
                return true;
            }

            screenPosition = Vector2.zero;
            pointerId = -1;
            return false;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
