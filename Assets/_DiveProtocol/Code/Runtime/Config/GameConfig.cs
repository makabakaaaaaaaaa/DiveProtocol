using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Static defaults copied into new runtime state.</summary>
    [CreateAssetMenu(fileName = "SO_GameConfig_Default", menuName = "Dive Protocol/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _initialMaxHealth = 100;
        [SerializeField, Min(0)] private int _initialLoadedAmmo = 6;
        [SerializeField, Min(0)] private int _initialReserveAmmo = 12;
        [SerializeField, Min(1)] private int _defaultMagazineSize = 6;
        [SerializeField, Min(0)] private int _forceDoorHealthCost = 5;
        [SerializeField] private string _startingLevelSceneName = SceneNames.Level01Drainage;

        public int InitialMaxHealth => _initialMaxHealth;
        public int InitialLoadedAmmo => _initialLoadedAmmo;
        public int InitialReserveAmmo => _initialReserveAmmo;
        public int DefaultMagazineSize => _defaultMagazineSize;
        public int ForceDoorHealthCost => _forceDoorHealthCost;
        public string StartingLevelSceneName => string.IsNullOrWhiteSpace(_startingLevelSceneName)
            ? SceneNames.Level01Drainage
            : _startingLevelSceneName.Trim();

        private void OnValidate()
        {
            _initialMaxHealth = Mathf.Max(1, _initialMaxHealth);
            _defaultMagazineSize = Mathf.Max(1, _defaultMagazineSize);
            _initialLoadedAmmo = Mathf.Clamp(_initialLoadedAmmo, 0, _defaultMagazineSize);
            _initialReserveAmmo = Mathf.Max(0, _initialReserveAmmo);
            _forceDoorHealthCost = Mathf.Clamp(_forceDoorHealthCost, 0, _initialMaxHealth);
            if (string.IsNullOrWhiteSpace(_startingLevelSceneName))
            {
                _startingLevelSceneName = SceneNames.Level01Drainage;
            }
        }
    }
}
