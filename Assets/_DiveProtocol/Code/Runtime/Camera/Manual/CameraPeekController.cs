using UnityEngine;
using DiveProtocol.Gameplay;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiveProtocol.CameraSystem
{
    /// <summary>
    /// 鎸佺画鎸変綇榧犳爣宸﹂敭鏃讹紝浣跨敤 WASD 鎴栨柟鍚戦敭绉诲姩鎽勫儚鏈鸿瀵熷亸绉汇€?
    /// 鏉惧紑宸﹂敭鍚庯紝鎽勫儚鏈哄钩婊戣繑鍥炵帺瀹躲€?
    ///
    /// 璇ヨ剼鏈笉鐩存帴淇敼 Camera Transform锛?
    /// 鍙悜 FixedAngleFollowCamera 鎻愪緵棰濆鍋忕Щ銆?
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FixedAngleFollowCamera))]
    public sealed class CameraPeekController : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private FixedAngleFollowCamera followCamera;

        [Header("Peek Movement")]
        [Tooltip("Movement speed while camera peek is held.")]
        [SerializeField, Min(0f)]
        private float moveSpeed = 4f;

        [Tooltip("Maximum ground-plane distance the peek camera can offset from the player.")]
        [SerializeField, Min(0f)]
        private float maximumDistance = 3f;

        [Tooltip("Smooth time used while moving the peek offset.")]
        [SerializeField, Min(0.01f)]
        private float movementSmoothTime = 0.08f;

        [Tooltip("Smooth time used when returning the peek offset to zero.")]
        [SerializeField, Min(0.01f)]
        private float returnSmoothTime = 0.18f;

        [Tooltip("Use unscaled time for camera peek movement.")]
        [SerializeField]
        private bool useUnscaledTime;

        [Header("Allowed Directions")]
        [SerializeField]
        private bool allowLeftRight = true;

        [SerializeField]
        private bool allowForwardBackward = true;

        private Vector3 targetOffset;
        private Vector3 currentOffset;
        private Vector3 offsetVelocity;
        private static int enabledControllerCount;
        private static bool isApplicationFocused = true;
        private static bool isAnyCameraPeeking;

        /// <summary>
        /// 褰撳墠杩欏彴鎽勫儚鏈烘槸鍚﹀浜庤瀵熸ā寮忋€?
        /// </summary>
        public bool IsPeeking { get; private set; }

        /// <summary>
        /// 渚?PlayerMovement 鍒ゆ柇鏄惁搴斿睆钄界帺瀹舵按骞崇Щ鍔ㄣ€?
        /// </summary>
        public static bool IsAnyCameraPeeking
        {
            get
            {
                return !GameplayInputLock.IsLocked &&
                       enabledControllerCount > 0 &&
                       isApplicationFocused &&
                       (isAnyCameraPeeking || ReadPeekButton());
            }

            private set => isAnyCameraPeeking = value;
        }

        private void Awake()
        {
            if (followCamera == null)
            {
                followCamera = GetComponent<FixedAngleFollowCamera>();
            }
        }

        private void OnEnable()
        {
            enabledControllerCount++;
            isApplicationFocused = Application.isFocused;
            ResetPeekState();
        }

        private void Update()
        {
            if (GameplayInputLock.IsLocked)
            {
                ResetPeekState();
                return;
            }

            IsPeeking = ReadPeekButton();
            IsAnyCameraPeeking = IsPeeking;

            float deltaTime = useUnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            if (IsPeeking)
            {
                UpdateTargetOffset(deltaTime);
            }
            else
            {
                targetOffset = Vector3.zero;
            }

            float smoothTime = IsPeeking
                ? movementSmoothTime
                : returnSmoothTime;

            currentOffset = Vector3.SmoothDamp(
                currentOffset,
                targetOffset,
                ref offsetVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);

            // 閬垮厤杩斿洖鍚庢畫鐣欓潪甯稿皬鐨勬诞鐐瑰亸绉汇€?
            if (!IsPeeking && currentOffset.sqrMagnitude < 0.0001f)
            {
                currentOffset = Vector3.zero;
                offsetVelocity = Vector3.zero;
            }

            if (followCamera != null)
            {
                followCamera.SetAdditionalOffset(currentOffset);
            }
        }

        private void UpdateTargetOffset(float deltaTime)
        {
            Vector2 input = ReadDirectionInput();

            if (!allowLeftRight)
            {
                input.x = 0f;
            }

            if (!allowForwardBackward)
            {
                input.y = 0f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            // 浣跨敤褰撳墠鐢婚潰鏂瑰悜锛岃€屼笉鏄笘鐣屽潗鏍囪酱鏂瑰悜銆?
            Vector3 cameraRight =
                Vector3.ProjectOnPlane(transform.right, Vector3.up);

            Vector3 cameraForward =
                Vector3.ProjectOnPlane(transform.forward, Vector3.up);

            if (cameraRight.sqrMagnitude > 0.0001f)
            {
                cameraRight.Normalize();
            }

            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                cameraForward.Normalize();
            }

            Vector3 direction =
                cameraRight * input.x +
                cameraForward * input.y;

            targetOffset += direction * moveSpeed * deltaTime;

            // 瑙傚療鎿嶄綔鍙湪鍦伴潰骞抽潰涓婂亸绉伙紝涓嶆敼鍙樻憚鍍忔満楂樺害銆?
            targetOffset.y = 0f;

            targetOffset = Vector3.ClampMagnitude(
                targetOffset,
                maximumDistance);
        }

        private static bool ReadPeekButton()
        {
            return Mouse.current != null &&
                   Mouse.current.leftButton.isPressed;
        }

        private static Vector2 ReadDirectionInput()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            // A / 宸︽柟鍚戦敭
            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            // D / 鍙虫柟鍚戦敭
            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            // S / 涓嬫柟鍚戦敭
            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            // W / 涓婃柟鍚戦敭
            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isApplicationFocused = hasFocus;
            if (!hasFocus)
            {
                ResetPeekState();
            }
        }

        private void OnDisable()
        {
            enabledControllerCount = Mathf.Max(0, enabledControllerCount - 1);
            ResetPeekState();
        }

        private void ResetPeekState()
        {
            IsPeeking = false;
            IsAnyCameraPeeking = false;

            targetOffset = Vector3.zero;
            currentOffset = Vector3.zero;
            offsetVelocity = Vector3.zero;

            if (followCamera != null)
            {
                followCamera.ClearAdditionalOffset();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            maximumDistance = Mathf.Max(0f, maximumDistance);
            movementSmoothTime = Mathf.Max(0.01f, movementSmoothTime);
            returnSmoothTime = Mathf.Max(0.01f, returnSmoothTime);
        }
#endif
    }
}
