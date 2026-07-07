using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Persistent composition root for the minimal application services.</summary>
    public sealed class AppRoot : MonoBehaviour
    {
        private static AppRoot _instance;

#if UNITY_INCLUDE_TESTS
        private static System.Func<SaveManager> _testSaveManagerFactory;
#endif

        private GameStateMachine _gameStateMachine;
        private SceneLoader _sceneLoader;
        private RunManager _runManager;
        private SaveManager _saveManager;

        public static AppRoot Instance => _instance;
        public GameStateMachine GameStateMachine => _gameStateMachine;
        public SceneLoader SceneLoader => _sceneLoader;
        public RunManager RunManager => _runManager;
        public SaveManager SaveManager => _saveManager;
        public bool IsReady { get; private set; }

        /// <summary>Returns the initialized global root without creating hidden dependencies.</summary>
        public static bool TryGetInstance(out AppRoot appRoot)
        {
            appRoot = _instance;
            return appRoot != null && appRoot.IsReady;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("A duplicate AppRoot was found and will be destroyed.");
                enabled = false;
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _gameStateMachine = GetComponent<GameStateMachine>();
            _sceneLoader = GetComponent<SceneLoader>();
            _runManager = GetComponent<RunManager>();

            if (transform.parent != null || transform.childCount != 0)
            {
                Debug.LogError("AppRoot must be a root GameObject with no children so content objects cannot enter DontDestroyOnLoad.");
                _instance = null;
                enabled = false;
                return;
            }

            if (_gameStateMachine == null || _sceneLoader == null || _runManager == null)
            {
                Debug.LogError("AppRoot requires GameStateMachine, SceneLoader, and RunManager components on the same GameObject.");
                _instance = null;
                enabled = false;
                return;
            }

            DontDestroyOnLoad(gameObject);
            _sceneLoader.Initialize(_gameStateMachine);
            _runManager.ClearRun();
#if UNITY_INCLUDE_TESTS
            _saveManager = _testSaveManagerFactory?.Invoke();
#endif
            if (_saveManager == null)
            {
                _saveManager = new SaveManager(Application.persistentDataPath);
            }

            _saveManager.Initialize();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var debugOverlay = gameObject.AddComponent<DebugOverlay>();
            debugOverlay.Initialize(this);
#endif

            IsReady = true;
        }

        private void Start()
        {
            if (IsReady && !_sceneLoader.LoadScene(SceneNames.MainMenu, GameState.MainMenu))
            {
                Debug.LogError("Bootstrap failed to begin loading the main menu.");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            _saveManager?.FlushPendingMetaChanges();
        }

#if UNITY_INCLUDE_TESTS
        internal static void SetSaveManagerFactoryForTests(System.Func<SaveManager> factory)
        {
            _testSaveManagerFactory = factory;
        }

        internal static void ResetTestOverrides()
        {
            _testSaveManagerFactory = null;
        }
#endif
    }
}
