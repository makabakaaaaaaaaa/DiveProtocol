using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiveProtocol.UI
{
    /// <summary>
    /// Creates the shared CRT HUD only while a gameplay level is active.
    /// </summary>
    public sealed class GameplayCrtUiBootstrap : MonoBehaviour
    {
        private const string PrefabResourcePath = "UI/GameplayCRT_UI";

        private static GameObject _activeHud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _activeHud = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneListener()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForInitialScene()
        {
            CreateOrDestroyFor(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            CreateOrDestroyFor(scene);
        }

        private static void CreateOrDestroyFor(Scene scene)
        {
            if (!IsGameplayScene(scene.name))
            {
                if (_activeHud != null)
                {
                    Object.Destroy(_activeHud);
                    _activeHud = null;
                }

                return;
            }

            if (_activeHud != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[{nameof(GameplayCrtUiBootstrap)}] Missing Resources prefab '{PrefabResourcePath}'.");
                return;
            }

            _activeHud = Object.Instantiate(prefab);
            _activeHud.name = "GameplayCRT_UI";
            SceneManager.MoveGameObjectToScene(_activeHud, scene);
        }

        private static bool IsGameplayScene(string sceneName)
        {
            return sceneName == SceneNames.Level01Drainage ||
                   sceneName == SceneNames.Level02Containment ||
                   sceneName == SceneNames.Level03MaintenanceTransfer ||
                   sceneName == SceneNames.Level04FacilityCore;
        }
    }
}
