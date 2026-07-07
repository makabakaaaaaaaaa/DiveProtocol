using System;
using UnityEngine;

namespace DiveProtocol.Loot
{
    /// <summary>
    /// One independently rolled whitebox loot entry.
    /// </summary>
    [Serializable]
    public sealed class LootDropEntry
    {
        public GameObject prefab;

        [Range(0f, 1f)]
        public float chance = 0.1f;

        [Min(1)]
        public int minAmount = 1;

        [Min(1)]
        public int maxAmount = 1;
    }
}
