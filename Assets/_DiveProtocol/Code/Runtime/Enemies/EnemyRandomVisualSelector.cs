using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace DiveProtocol.Enemies
{
    /// <summary>
    /// Randomizes the visual-only model used by a fixed enemy logic prefab.
    /// </summary>
    public sealed class EnemyRandomVisualSelector : MonoBehaviour
    {
        [Header("Visual Pool")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private List<GameObject> _visualPrefabPool = new List<GameObject>();
        [SerializeField] private bool _randomizeOnAwake = true;
        [SerializeField] private bool _clearExistingVisualChildren = true;

        [Header("Disable Visual Logic")]
        [SerializeField] private bool _disableCollidersOnVisual = true;
        [SerializeField] private bool _disableRigidbodiesOnVisual = true;
        [SerializeField] private bool _disableNavMeshAgentsOnVisual = true;

        [Header("Animator")]
        [SerializeField] private RuntimeAnimatorController _overrideAnimatorController;
        [SerializeField] private bool _forceDisableRootMotion = true;

        [Header("Transform Offset")]
        [SerializeField] private Vector3 _localPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 _localEulerOffset = Vector3.zero;
        [SerializeField] private Vector3 _localScale = Vector3.one;

        [Header("Debug")]
        [SerializeField] private bool _logSelection = false;

        /// <summary>
        /// Gets the currently spawned visual instance, if one exists.
        /// </summary>
        public GameObject CurrentVisualInstance { get; private set; }

        /// <summary>
        /// Gets the Animator found on the current visual instance, if one exists.
        /// </summary>
        public Animator CurrentAnimator { get; private set; }

        private void Awake()
        {
            EnsureVisualRoot();

            if (_randomizeOnAwake)
            {
                RandomizeVisual();
            }
        }

        /// <summary>
        /// Clears the current visual and spawns a randomly selected visual prefab.
        /// </summary>
        public void RandomizeVisual()
        {
            EnsureVisualRoot();

            GameObject visualPrefab = PickRandomVisualPrefab();
            if (visualPrefab == null)
            {
                Debug.LogWarning($"{nameof(EnemyRandomVisualSelector)} on {name} has no valid visual prefab to spawn.", this);
                return;
            }

            if (_clearExistingVisualChildren)
            {
                ClearCurrentVisual();
            }

            CurrentVisualInstance = Instantiate(visualPrefab, _visualRoot);
            CurrentVisualInstance.name = visualPrefab.name + "_RuntimeVisual";

            Transform visualTransform = CurrentVisualInstance.transform;
            visualTransform.localPosition = _localPositionOffset;
            visualTransform.localRotation = Quaternion.Euler(_localEulerOffset);
            visualTransform.localScale = _localScale;

            SanitizeVisualInstance(CurrentVisualInstance);
            CurrentAnimator = CurrentVisualInstance.GetComponentInChildren<Animator>(true);
            ConfigureAnimator(CurrentAnimator);
            NotifyAnimatorBridge();

            if (_logSelection)
            {
                Debug.Log($"{nameof(EnemyRandomVisualSelector)} selected visual '{visualPrefab.name}' for enemy '{name}'.", this);
            }
        }

        /// <summary>
        /// Removes the current visual instance or all children under the visual root.
        /// </summary>
        public void ClearCurrentVisual()
        {
            EnsureVisualRoot();

            CurrentVisualInstance = null;
            CurrentAnimator = null;

            if (_visualRoot == null)
            {
                return;
            }

            for (int i = _visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _visualRoot.GetChild(i);
                DestroyObject(child.gameObject);
            }
        }

        private GameObject PickRandomVisualPrefab()
        {
            if (_visualPrefabPool == null || _visualPrefabPool.Count == 0)
            {
                return null;
            }

            List<GameObject> validPrefabs = new List<GameObject>();
            for (int i = 0; i < _visualPrefabPool.Count; i++)
            {
                if (_visualPrefabPool[i] != null)
                {
                    validPrefabs.Add(_visualPrefabPool[i]);
                }
            }

            if (validPrefabs.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, validPrefabs.Count);
            return validPrefabs[index];
        }

        private void EnsureVisualRoot()
        {
            if (_visualRoot != null)
            {
                return;
            }

            Transform existingVisualRoot = transform.Find("Visual");
            if (existingVisualRoot != null)
            {
                _visualRoot = existingVisualRoot;
                return;
            }

            GameObject visualRootObject = new GameObject("Visual");
            _visualRoot = visualRootObject.transform;
            _visualRoot.SetParent(transform, false);
        }

        private void SanitizeVisualInstance(GameObject visualInstance)
        {
            if (visualInstance == null)
            {
                return;
            }

            if (_disableCollidersOnVisual)
            {
                Collider[] colliders = visualInstance.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = false;
                }
            }

            if (_disableRigidbodiesOnVisual)
            {
                Rigidbody[] rigidbodies = visualInstance.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    rigidbodies[i].isKinematic = true;
                    rigidbodies[i].detectCollisions = false;
                }
            }

            if (_disableNavMeshAgentsOnVisual)
            {
                NavMeshAgent[] navMeshAgents = visualInstance.GetComponentsInChildren<NavMeshAgent>(true);
                for (int i = 0; i < navMeshAgents.Length; i++)
                {
                    navMeshAgents[i].enabled = false;
                }
            }
        }

        private void ConfigureAnimator(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            if (_forceDisableRootMotion)
            {
                animator.applyRootMotion = false;
            }

            if (_overrideAnimatorController != null)
            {
                animator.runtimeAnimatorController = _overrideAnimatorController;
            }
        }

        private void NotifyAnimatorBridge()
        {
            SendMessage("RefreshAnimatorReference", SendMessageOptions.DontRequireReceiver);
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
