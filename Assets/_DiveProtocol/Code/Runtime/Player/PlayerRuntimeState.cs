using System;

namespace DiveProtocol
{
    /// <summary>Mutable player values owned exclusively by the current run.</summary>
    [Serializable]
    public sealed class PlayerRuntimeState
    {
        public PlayerRuntimeState(int maxHealth, int loadedAmmo, int reserveAmmo)
        {
            if (maxHealth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be at least 1.");
            }

            if (loadedAmmo < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(loadedAmmo), "Loaded ammo cannot be negative.");
            }

            if (reserveAmmo < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reserveAmmo), "Reserve ammo cannot be negative.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            LoadedAmmo = loadedAmmo;
            ReserveAmmo = reserveAmmo;
        }

        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; }
        public int LoadedAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }

        /// <summary>Applies non-negative damage and returns the amount actually taken.</summary>
        public int TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var applied = Math.Min(amount, CurrentHealth);
            CurrentHealth -= applied;
            return applied;
        }

        /// <summary>Restores health without exceeding MaxHealth.</summary>
        public int Heal(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            var applied = Math.Min(amount, MaxHealth - CurrentHealth);
            CurrentHealth += applied;
            return applied;
        }

        /// <summary>Spends health while preventing a lethal cost unless explicitly allowed.</summary>
        public bool TrySpendHealth(int amount, bool allowLethal = false)
        {
            if (amount < 0)
            {
                return false;
            }

            var remainingHealth = CurrentHealth - amount;
            var minimumHealth = allowLethal ? 0 : 1;
            if (remainingHealth < minimumHealth)
            {
                return false;
            }

            CurrentHealth = remainingHealth;
            return true;
        }

        /// <summary>Adds reserve ammunition without allowing integer overflow.</summary>
        public void AddReserveAmmo(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            ReserveAmmo = (int)Math.Min((long)ReserveAmmo + amount, int.MaxValue);
        }

        /// <summary>Consumes loaded ammunition when the requested amount is available.</summary>
        public bool TryConsumeLoadedAmmo(int amount = 1)
        {
            if (amount <= 0 || LoadedAmmo < amount)
            {
                return false;
            }

            LoadedAmmo -= amount;
            return true;
        }

        /// <summary>Moves reserve ammunition into the magazine and returns the amount moved.</summary>
        public int Reload(int magazineCapacity)
        {
            if (magazineCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(magazineCapacity), "Magazine capacity must be at least 1.");
            }

            if (LoadedAmmo >= magazineCapacity || ReserveAmmo == 0)
            {
                return 0;
            }

            var transferred = Math.Min(magazineCapacity - LoadedAmmo, ReserveAmmo);
            LoadedAmmo += transferred;
            ReserveAmmo -= transferred;
            return transferred;
        }
    }
}
