using System;
using System.Collections.Generic;

namespace DiveProtocol
{
    /// <summary>Minimal identifier-based inventory owned by one run.</summary>
    [Serializable]
    public sealed class InventoryState
    {
        private readonly HashSet<string> _keyItemIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _minorComponentIds = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> KeyItemIds => _keyItemIds;
        public IReadOnlyCollection<string> MinorComponentIds => _minorComponentIds;

        public bool AddKeyItem(string itemId)
        {
            return TryNormalizeId(itemId, out var normalizedId) && _keyItemIds.Add(normalizedId);
        }

        public bool HasKeyItem(string itemId)
        {
            return TryNormalizeId(itemId, out var normalizedId) && _keyItemIds.Contains(normalizedId);
        }

        public bool RemoveKeyItem(string itemId)
        {
            return TryNormalizeId(itemId, out var normalizedId) && _keyItemIds.Remove(normalizedId);
        }

        public bool AddMinorComponent(string componentId)
        {
            return TryNormalizeId(componentId, out var normalizedId) && _minorComponentIds.Add(normalizedId);
        }

        public bool HasMinorComponent(string componentId)
        {
            return TryNormalizeId(componentId, out var normalizedId) && _minorComponentIds.Contains(normalizedId);
        }

        private static bool TryNormalizeId(string value, out string normalizedValue)
        {
            normalizedValue = value?.Trim();
            return !string.IsNullOrWhiteSpace(normalizedValue);
        }
    }
}
