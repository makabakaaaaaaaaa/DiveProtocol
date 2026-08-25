using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiveProtocol.UI
{
    /// <summary>
    /// Binds the independent CRT HUD to the currently spawned player's weapon and level status state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayCrtHudController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private TMP_Text _sectorText;
        [SerializeField] private TMP_Text _ammoReadoutText;
        [SerializeField] private Image[] _ammoIndicators;
        [SerializeField] private GameplayPauseController _pauseController;

        [Header("Binding")]
        [SerializeField, Min(0.05f)] private float _playerSearchInterval = 0.25f;

        private PlayerMovement _player;
        private PlayerHitscanWeapon _weapon;
        private float _nextPlayerSearchTime;

        private void Awake()
        {
            RefreshSectorText();
            BindCamera();
            RefreshAmmoReadout();
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            TryBindPlayer();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            UnbindPlayer();
        }

        private void Update()
        {
            BindCamera();

            if (_player != null)
            {
                return;
            }

            if (Time.unscaledTime < _nextPlayerSearchTime)
            {
                return;
            }

            _nextPlayerSearchTime = Time.unscaledTime + _playerSearchInterval;
            TryBindPlayer();
        }

        private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            RefreshSectorText();
            UnbindPlayer();
            _nextPlayerSearchTime = 0f;
        }

        private void TryBindPlayer()
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player == null || player == _player)
            {
                return;
            }

            UnbindPlayer();
            _player = player;
            _weapon = player.GetComponent<PlayerHitscanWeapon>();

            if (_weapon != null)
            {
                _weapon.AmmoChanged += HandleAmmoChanged;
            }

            _pauseController?.RegisterPlayer(player.transform);
            RefreshAmmoReadout();
        }

        private void UnbindPlayer()
        {
            if (_weapon != null)
            {
                _weapon.AmmoChanged -= HandleAmmoChanged;
            }

            _player = null;
            _weapon = null;
        }

        private void BindCamera()
        {
            if (_canvas != null && _canvas.worldCamera != Camera.main)
            {
                _canvas.worldCamera = Camera.main;
            }
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            RefreshAmmoReadout();
        }

        private void RefreshSectorText()
        {
            if (_sectorText == null)
            {
                return;
            }

            _sectorText.text = LevelStatusCatalog.GetForActiveScene().ToHudText();
        }

        private void RefreshAmmoReadout()
        {
            int currentAmmo = _weapon != null ? _weapon.CurrentAmmo : 0;
            int maxAmmo = _weapon != null ? _weapon.MaxAmmo : 0;

            if (_ammoReadoutText != null)
            {
                _ammoReadoutText.enableWordWrapping = false;
                _ammoReadoutText.overflowMode = TextOverflowModes.Overflow;
                _ammoReadoutText.text =
                    $"AMMO LOG\n\n7.82mm        {currentAmmo:00} / {maxAmmo:00}\n\n\nSTATUS : {(currentAmmo > 0 ? "LOADED" : "EMPTY")}";
            }

            if (_ammoIndicators == null || _ammoIndicators.Length == 0)
            {
                return;
            }

            int visibleIndicators = maxAmmo > 0
                ? Mathf.Clamp(Mathf.CeilToInt((float)currentAmmo / maxAmmo * _ammoIndicators.Length), 0, _ammoIndicators.Length)
                : 0;

            for (int index = 0; index < _ammoIndicators.Length; index++)
            {
                if (_ammoIndicators[index] != null)
                {
                    _ammoIndicators[index].enabled = index < visibleIndicators;
                }
            }
        }
    }
}
