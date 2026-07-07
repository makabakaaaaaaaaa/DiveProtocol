using UnityEngine;

namespace DiveProtocol.CameraSystem
{
    /// <summary>
    /// 固定世界角度跟随目标。
    /// 挂在普通 Main Camera 上，不依赖 Cinemachine。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class FixedAngleFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerByTag = true;
        [SerializeField] private string playerTag = "Player";
        [SerializeField, Min(0.05f)] private float searchInterval = 0.25f;
        [SerializeField] private bool snapWhenTargetFound = true;

        [Header("Fixed View")]
        [Tooltip("摄像机相对于玩家的世界坐标偏移。")]
        [SerializeField]
        private Vector3 followOffset =
            new Vector3(-8f, 10f, -8f);

        [Tooltip("摄像机始终保持的世界旋转角度。")]
        [SerializeField]
        private Vector3 fixedEulerAngles =
            new Vector3(35f, 45f, 0f);

        [Header("Follow Axes")]
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY = true;
        [SerializeField] private bool followZ = true;

        [Header("Smoothing")]
        [Tooltip("0代表立即跟随。推荐0.08到0.20。")]
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        [SerializeField, Min(0f)] private float maximumSpeed = 100f;

        private Vector3 additionalOffset;
        private Vector3 movementVelocity;
        private float nextSearchTime;

        public Transform Target => target;

        private void Awake()
        {
            ApplyFixedRotation();
        }

        private void OnEnable()
        {
            ApplyFixedRotation();

            if (target != null && snapWhenTargetFound)
            {
                SnapToTarget();
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                TryFindPlayer();

                if (target == null)
                {
                    return;
                }
            }

            Vector3 desiredPosition = CalculateDesiredPosition();

            if (smoothTime <= 0f)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref movementVelocity,
                    smoothTime,
                    maximumSpeed,
                    Time.deltaTime);
            }

            ApplyFixedRotation();
        }

        public void SetTarget(
            Transform newTarget,
            bool snapImmediately = true)
        {
            target = newTarget;
            movementVelocity = Vector3.zero;

            if (target != null && snapImmediately)
            {
                SnapToTarget();
            }
        }

        public void ClearTarget()
        {
            target = null;
            movementVelocity = Vector3.zero;
        }

        public void SetAdditionalOffset(Vector3 offset)
        {
            additionalOffset = offset;
        }

        public void ClearAdditionalOffset()
        {
            additionalOffset = Vector3.zero;
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = CalculateDesiredPosition();
            ApplyFixedRotation();
            movementVelocity = Vector3.zero;
        }

        private Vector3 CalculateDesiredPosition()
        {
            Vector3 targetPosition =
                target.position +
                followOffset +
                additionalOffset;

            Vector3 currentPosition = transform.position;

            return new Vector3(
                followX ? targetPosition.x : currentPosition.x,
                followY ? targetPosition.y : currentPosition.y,
                followZ ? targetPosition.z : currentPosition.z);
        }

        private void ApplyFixedRotation()
        {
            transform.rotation =
                Quaternion.Euler(fixedEulerAngles);
        }

        private void TryFindPlayer()
        {
            if (!autoFindPlayerByTag ||
                Time.unscaledTime < nextSearchTime)
            {
                return;
            }

            nextSearchTime =
                Time.unscaledTime + searchInterval;

            if (string.IsNullOrWhiteSpace(playerTag))
            {
                return;
            }

            GameObject playerObject;

            try
            {
                playerObject =
                    GameObject.FindGameObjectWithTag(playerTag);
            }
            catch (UnityException exception)
            {
                Debug.LogError(
                    $"[{nameof(FixedAngleFollowCamera)}] " +
                    $"找不到Tag：'{playerTag}'。\n{exception.Message}",
                    this);

                autoFindPlayerByTag = false;
                return;
            }

            if (playerObject == null)
            {
                return;
            }

            SetTarget(
                playerObject.transform,
                snapWhenTargetFound);

            Debug.Log(
                $"[{nameof(FixedAngleFollowCamera)}] " +
                $"已绑定玩家：{playerObject.name}",
                this);
        }

        private void OnDisable()
        {
            movementVelocity = Vector3.zero;
            additionalOffset = Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            searchInterval = Mathf.Max(0.05f, searchInterval);
            smoothTime = Mathf.Max(0f, smoothTime);
            maximumSpeed = Mathf.Max(0f, maximumSpeed);
        }
#endif
    }
}