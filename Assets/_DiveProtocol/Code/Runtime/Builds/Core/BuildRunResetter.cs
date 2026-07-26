using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Optional component that clears build state when a new run or scene flow asks it to.
    /// </summary>
    public sealed class BuildRunResetter : MonoBehaviour
    {
        [SerializeField] private PlayerBuildController buildController;
        [SerializeField] private bool resetOnStart;

        private void Awake()
        {
            if (buildController == null)
            {
                buildController = GetComponent<PlayerBuildController>();
            }
        }

        private void Start()
        {
            if (resetOnStart)
            {
                ResetBuilds();
            }
        }

        /// <summary>
        /// Clears all upgrades from the configured controller.
        /// </summary>
        public void ResetBuilds()
        {
            if (buildController != null)
            {
                buildController.State.ClearAllUpgrades();
            }
        }
    }
}
