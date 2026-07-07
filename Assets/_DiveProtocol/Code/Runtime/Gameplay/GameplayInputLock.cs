using System.Collections.Generic;
using UnityEngine;

namespace DiveProtocol.Gameplay
{
    /// <summary>
    /// Tracks temporary gameplay input locks owned by runtime UI or modal states.
    /// </summary>
    public static class GameplayInputLock
    {
        private static readonly HashSet<int> OwnerInstanceIds = new();

        /// <summary>
        /// True while at least one valid owner is blocking normal gameplay input.
        /// </summary>
        public static bool IsLocked => OwnerInstanceIds.Count > 0;

        /// <summary>
        /// Adds a lock owner. Calling this repeatedly with the same owner is safe.
        /// </summary>
        public static void Acquire(Object owner)
        {
            if (owner == null)
            {
                Debug.LogWarning("[Input] Ignored gameplay input lock acquire with a null owner.");
                return;
            }

            OwnerInstanceIds.Add(owner.GetInstanceID());
        }

        /// <summary>
        /// Releases a lock owner without affecting locks held by other owners.
        /// </summary>
        public static void Release(Object owner)
        {
            if (owner == null)
            {
                return;
            }

            OwnerInstanceIds.Remove(owner.GetInstanceID());
        }

        /// <summary>
        /// Clears every active gameplay input lock.
        /// </summary>
        public static void ClearAll()
        {
            OwnerInstanceIds.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForRuntimeLoad()
        {
            ClearAll();
        }
    }
}
