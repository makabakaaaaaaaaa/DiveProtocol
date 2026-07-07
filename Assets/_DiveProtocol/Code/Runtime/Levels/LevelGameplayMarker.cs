using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Non-gameplay marker used to document Level 01 progression and spawn positions.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelGameplayMarker : MonoBehaviour
    {
        [SerializeField] private string _uniqueId;
        [SerializeField] private LevelMarkerType _markerType;
        [SerializeField] private string _roomId;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private bool _criticalProgression;
        [SerializeField] private bool _randomlyDisableable;

        public string UniqueId => _uniqueId;
        public LevelMarkerType MarkerType => _markerType;
        public string RoomId => _roomId;
        public string Description => _description;
        public bool CriticalProgression => _criticalProgression;
        public bool RandomlyDisableable => _randomlyDisableable;

        public void Initialize(string uniqueId, LevelMarkerType markerType, string roomId, string description, bool criticalProgression, bool randomlyDisableable)
        {
            _uniqueId = uniqueId?.Trim() ?? string.Empty;
            _markerType = markerType;
            _roomId = roomId?.Trim() ?? string.Empty;
            _description = description ?? string.Empty;
            _criticalProgression = criticalProgression;
            _randomlyDisableable = randomlyDisableable && !criticalProgression;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = MarkerColor(_markerType);
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
        }

        private static Color MarkerColor(LevelMarkerType markerType)
        {
            return markerType switch
            {
                LevelMarkerType.KeyItem => Color.yellow,
                LevelMarkerType.Resource => Color.green,
                LevelMarkerType.Enemy => Color.red,
                LevelMarkerType.ConditionalEnemy => new Color(0.4f, 0.1f, 0.9f),
                LevelMarkerType.Door => Color.cyan,
                LevelMarkerType.LockedDoor => new Color(1f, 0.5f, 0f),
                LevelMarkerType.OneWayDoorSide => new Color(0f, 0.8f, 1f),
                LevelMarkerType.Vent => Color.magenta,
                LevelMarkerType.BuildOption => new Color(0.2f, 0.8f, 1f),
                LevelMarkerType.Exit => Color.white,
                LevelMarkerType.Boss => new Color(1f, 0.15f, 0.05f),
                LevelMarkerType.Terminal => new Color(0.2f, 0.9f, 1f),
                LevelMarkerType.Code => new Color(1f, 0.85f, 0.1f),
                LevelMarkerType.ArenaSeal => new Color(1f, 0.35f, 0.1f),
                LevelMarkerType.Elevator => new Color(0.8f, 0.8f, 1f),
                LevelMarkerType.Transition => Color.white,
                LevelMarkerType.Observation => new Color(0.35f, 0.9f, 1f),
                LevelMarkerType.Stair => new Color(0.8f, 0.6f, 0.35f),
                LevelMarkerType.Survival => new Color(1f, 0.25f, 0.25f),
                LevelMarkerType.Fuse => new Color(1f, 0.75f, 0.15f),
                LevelMarkerType.VulnerablePoint => new Color(1f, 0.2f, 0.9f),
                _ => Color.gray
            };
        }
    }

    public enum LevelMarkerType
    {
        PlayerStart,
        Door,
        LockedDoor,
        OneWayDoorSide,
        KeyItem,
        Resource,
        Enemy,
        ConditionalEnemy,
        Corpse,
        Debris,
        Vent,
        BuildOption,
        BuildSelectionArea,
        DelayedRewardArea,
        Exit,
        Boss,
        Terminal,
        Code,
        ArenaSeal,
        Elevator,
        Transition,
        Observation,
        Stair,
        Survival,
        Fuse,
        VulnerablePoint
    }
}
