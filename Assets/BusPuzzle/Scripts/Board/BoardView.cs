using System.Collections.Generic;
using UnityEngine;

namespace BusPuzzle
{
    public sealed class BoardView : MonoBehaviour
    {
        private Transform passengerRoot;
        private Transform busRoot;

        public void BuildLevel(LevelData levelData, List<PassengerView> passengers, List<BusView> buses)
        {
            ClearBoard();
            CreateRoots();
            CreateGround();

            passengers.Clear();
            buses.Clear();

            for (var index = 0; index < levelData.PassengerQueue.Count; index++)
            {
                var passenger = PassengerView.Create(levelData.PassengerQueue[index], passengerRoot);
                passenger.SetPosition(GetQueuePosition(index));
                passengers.Add(passenger);
            }

            for (var index = 0; index < levelData.Buses.Count; index++)
            {
                var bus = BusView.Create(levelData.Buses[index], busRoot);
                bus.transform.position = GetBusPosition(index, levelData.Buses.Count);
                buses.Add(bus);
            }

            HighlightFrontPassenger(passengers);
        }

        public void LayoutWaitingPassengers(IReadOnlyList<PassengerView> passengers, bool animate)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                var passenger = passengers[index];
                var position = GetQueuePosition(index);

                if (animate)
                {
                    passenger.MoveTo(position, 0.22f);
                }
                else
                {
                    passenger.SetPosition(position);
                }
            }

            HighlightFrontPassenger(passengers);
        }

        private void ClearBoard()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private void CreateRoots()
        {
            passengerRoot = new GameObject("Passengers").transform;
            passengerRoot.SetParent(transform, false);

            busRoot = new GameObject("Buses").transform;
            busRoot.SetParent(transform, false);
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Board Floor";
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(0f, -0.06f, -0.45f);
            ground.transform.localScale = new Vector3(7.2f, 0.1f, 11.2f);
            ground.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial("Board Floor", new Color(0.82f, 0.86f, 0.88f));

            var queueLane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            queueLane.name = "Queue Lane";
            queueLane.transform.SetParent(transform, false);
            queueLane.transform.position = new Vector3(0f, 0.01f, 3.25f);
            queueLane.transform.localScale = new Vector3(6.3f, 0.04f, 2.6f);
            queueLane.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial("Queue Lane", new Color(0.91f, 0.93f, 0.94f));

            var busLane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            busLane.name = "Bus Lane";
            busLane.transform.SetParent(transform, false);
            busLane.transform.position = new Vector3(0f, 0.015f, -2.55f);
            busLane.transform.localScale = new Vector3(6.4f, 0.04f, 5.4f);
            busLane.GetComponent<Renderer>().sharedMaterial = PuzzlePalette.CreateSolidMaterial("Bus Lane", new Color(0.75f, 0.78f, 0.80f));
        }

        private static Vector3 GetQueuePosition(int index)
        {
            const int columns = 4;
            const float spacingX = 0.88f;
            const float spacingZ = 0.78f;

            var column = index % columns;
            var row = index / columns;
            var x = (column - (columns - 1) * 0.5f) * spacingX;
            var z = 4.25f - row * spacingZ;
            return new Vector3(x, 0.58f, z);
        }

        private static Vector3 GetBusPosition(int index, int busCount)
        {
            var columns = busCount <= 3 ? busCount : 2;
            columns = Mathf.Max(1, columns);

            const float spacingX = 3.1f;
            const float spacingZ = 1.85f;

            var column = index % columns;
            var row = index / columns;
            var x = (column - (columns - 1) * 0.5f) * spacingX;
            var z = -1.35f - row * spacingZ;
            return new Vector3(x, 0f, z);
        }

        private static void HighlightFrontPassenger(IReadOnlyList<PassengerView> passengers)
        {
            for (var index = 0; index < passengers.Count; index++)
            {
                passengers[index].SetEmphasis(index == 0);
            }
        }
    }
}
