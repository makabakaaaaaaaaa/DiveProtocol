using UnityEngine;

namespace DiveProtocol
{
    /// <summary>
    /// Static data describing a loading-scene transition from an exit trigger to a target scene.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SO_SceneTransitionProfile",
        menuName = "Dive Protocol/Transitions/Scene Transition Profile")]
    public sealed class SceneTransitionProfile : ScriptableObject
    {
        [Header("Scenes")]
        [SerializeField]
        private string loadingSceneName = "SCN_Loading";

        [SerializeField]
        private string targetSceneName;

        [Header("Presentation")]
        [SerializeField]
        private string transitionTitle = "LOADING";

        [TextArea]
        [SerializeField]
        private string transitionDescription;

        [SerializeField, Min(0f)]
        private float minimumDisplaySeconds = 1f;

        [SerializeField]
        private GameObject optionalPresentationPrefab;

        [SerializeField, Min(0f)]
        private float optionalPresentationDurationSeconds;

        public string LoadingSceneName => loadingSceneName;
        public string TargetSceneName => targetSceneName;
        public string TransitionTitle => transitionTitle;
        public string TransitionDescription => transitionDescription;
        public float MinimumDisplaySeconds => minimumDisplaySeconds;
        public GameObject OptionalPresentationPrefab => optionalPresentationPrefab;
        public float OptionalPresentationDurationSeconds => optionalPresentationDurationSeconds;

#if UNITY_EDITOR
        private void OnValidate()
        {
            minimumDisplaySeconds = Mathf.Max(0f, minimumDisplaySeconds);
            optionalPresentationDurationSeconds = Mathf.Max(0f, optionalPresentationDurationSeconds);
        }
#endif
    }
}
