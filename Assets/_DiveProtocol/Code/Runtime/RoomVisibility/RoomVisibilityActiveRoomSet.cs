using System.Collections.Generic;

namespace DiveProtocol.RoomVisibility
{
    /// <summary>Owns active-room transition state and preserves the last valid set across tiny volume gaps.</summary>
    internal sealed class RoomVisibilityActiveRoomSet
    {
        private readonly HashSet<RoomVisibilityRoomEntry> _current = new HashSet<RoomVisibilityRoomEntry>();
        private readonly HashSet<RoomVisibilityRoomEntry> _lastValid = new HashSet<RoomVisibilityRoomEntry>();

        public IReadOnlyCollection<RoomVisibilityRoomEntry> Current => _current;
        public IReadOnlyCollection<RoomVisibilityRoomEntry> LastValid => _lastValid;

        public bool Update(IReadOnlyCollection<RoomVisibilityRoomEntry> detected)
        {
            var effective = new HashSet<RoomVisibilityRoomEntry>();
            if (detected != null && detected.Count > 0)
            {
                foreach (RoomVisibilityRoomEntry entry in detected)
                {
                    if (entry != null) effective.Add(entry);
                }

                _lastValid.Clear();
                foreach (RoomVisibilityRoomEntry entry in effective) _lastValid.Add(entry);
            }
            else
            {
                foreach (RoomVisibilityRoomEntry entry in _lastValid) effective.Add(entry);
            }

            if (_current.SetEquals(effective)) return false;
            _current.Clear();
            foreach (RoomVisibilityRoomEntry entry in effective) _current.Add(entry);
            return true;
        }
    }
}
