using TMPro;
using UnityEngine;

namespace DiveProtocol.UI
{
    /// <summary>
    /// Persistent gameplay HUD showing runtime player health and ammo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayStatusHUD : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text ammoText;

        [Header("Format")]
        [SerializeField] private string healthFormat = "HP {0:0} / {1:0}";
        [SerializeField] private string ammoFormat = "AMMO {0} / {1}";

        private HealthComponent _health;
        private PlayerHitscanWeapon _weapon;

        private void OnDisable()
        {
            UnbindPlayer();
        }

        /// <summary>
        /// Binds HUD text to a runtime-spawned player transform.
        /// </summary>
        public void BindPlayer(Transform player)
        {
            BindPlayer(player != null ? player.gameObject : null);
        }

        /// <summary>
        /// Binds HUD text to a runtime-spawned player object.
        /// </summary>
        public void BindPlayer(GameObject player)
        {
            UnbindPlayer();

            if (player == null)
            {
                Refresh();
                return;
            }

            _health = FindPlayerComponent<HealthComponent>(player);
            _weapon = FindPlayerComponent<PlayerHitscanWeapon>(player);

            if (_health != null)
            {
                _health.HealthChanged += HandleHealthChanged;
            }

            if (_weapon != null)
            {
                _weapon.AmmoChanged += HandleAmmoChanged;
            }

            Refresh();
        }

        /// <summary>
        /// Refreshes HUD text from the currently bound player components.
        /// </summary>
        public void Refresh()
        {
            if (healthText != null)
            {
                healthText.text = _health != null
                    ? string.Format(healthFormat, _health.CurrentHealth, _health.MaxHealth)
                    : "HP -- / --";
            }

            if (ammoText != null)
            {
                ammoText.text = _weapon != null
                    ? string.Format(ammoFormat, _weapon.CurrentAmmo, _weapon.MaxAmmo)
                    : "AMMO -- / --";
            }
        }

        private void UnbindPlayer()
        {
            if (_health != null)
            {
                _health.HealthChanged -= HandleHealthChanged;
            }

            if (_weapon != null)
            {
                _weapon.AmmoChanged -= HandleAmmoChanged;
            }

            _health = null;
            _weapon = null;
        }

        private void HandleHealthChanged(HealthComponent health, float currentHealth, float maxHealth)
        {
            Refresh();
        }

        private void HandleAmmoChanged(int currentAmmo, int maxAmmo)
        {
            Refresh();
        }

        private static T FindPlayerComponent<T>(GameObject player)
            where T : Component
        {
            if (player.TryGetComponent(out T component))
            {
                return component;
            }

            component = player.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }

            return player.GetComponentInParent<T>();
        }
    }
}
