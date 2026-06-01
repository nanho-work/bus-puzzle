using System;
using UnityEngine;

namespace BusPuzzle
{
    [Serializable]
    public struct BusDefinition
    {
        [SerializeField] private PuzzleColor color;
        [SerializeField, Min(1)] private int capacity;

        public BusDefinition(PuzzleColor color, int capacity)
        {
            this.color = color;
            this.capacity = Mathf.Max(1, capacity);
        }

        public PuzzleColor Color => color;
        public int Capacity => Mathf.Max(1, capacity);
    }
}
