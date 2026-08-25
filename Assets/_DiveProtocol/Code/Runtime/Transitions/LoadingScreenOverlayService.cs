using DiveProtocol.UI;
using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Creates and controls the reusable loading overlay without coupling callers to a scene canvas.</summary>
    public static class LoadingScreenOverlayService
    {
        private const string ResourcePath = "UI/LoadingScreenOverlay";

        /// <summary>Shows the reusable terminal presentation for a pending transition.</summary>
        public static void Show(string routeName, string routeDescription)
        {
            LoadingScreenOverlay overlay = EnsureInstance();
            if (overlay != null)
            {
                overlay.Show(routeName, routeDescription);
            }
        }

        /// <summary>Updates the currently visible loading progress, when an overlay exists.</summary>
        public static void SetProgress(float normalizedProgress)
        {
            LoadingScreenOverlay.Instance?.SetProgress(normalizedProgress);
        }

        /// <summary>Defers hiding until the target scene replaces the loading scene.</summary>
        public static void HideAfterNextSceneActivation()
        {
            LoadingScreenOverlay.Instance?.HideAfterNextSceneActivation();
        }

        /// <summary>Hides the currently visible overlay immediately.</summary>
        public static void Hide()
        {
            LoadingScreenOverlay.Instance?.Hide();
        }

        private static LoadingScreenOverlay EnsureInstance()
        {
            if (LoadingScreenOverlay.Instance != null)
            {
                return LoadingScreenOverlay.Instance;
            }

            LoadingScreenOverlay prefab =
                Resources.Load<LoadingScreenOverlay>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError(
                    $"[Loading] Missing Resources/{ResourcePath} prefab.");
                return null;
            }

            return Object.Instantiate(prefab);
        }
    }
}
