using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Optional runtime debug helper for manually granting build cores.
    /// </summary>
    public sealed class BuildDebugGrant : MonoBehaviour
    {
        [SerializeField] private bool enableDebugKeys;
        [SerializeField] private PlayerBuildController buildController;

        private void Awake()
        {
            if (buildController == null)
            {
                buildController = GetComponent<PlayerBuildController>();
            }
        }

        private void Update()
        {
            if (!enableDebugKeys || buildController == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                GrantRedMarrowCore();
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                GrantOpticNerveCore();
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                GrantHumusCore();
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                PrintOwnedUpgrades();
            }
#endif
        }

        public void GrantRedMarrowCore()
        {
            buildController?.GrantUpgrade(BuildUpgradeId.RedMarrow_Overdraft);
        }

        public void GrantOpticNerveCore()
        {
            buildController?.GrantUpgrade(BuildUpgradeId.OpticNerve_Calibration);
        }

        public void GrantHumusCore()
        {
            buildController?.GrantUpgrade(BuildUpgradeId.Humus_Sympathy);
        }

        public void PrintOwnedUpgrades()
        {
            if (buildController == null)
            {
                return;
            }

            foreach (BuildUpgradeId id in buildController.State.OwnedUpgrades)
            {
                Debug.Log($"[Builds] Owned upgrade: {id}", this);
            }
        }
    }
}
