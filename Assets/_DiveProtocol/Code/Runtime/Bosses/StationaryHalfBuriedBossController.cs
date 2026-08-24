using System.Collections;
using DiveProtocol.Enemies;
using UnityEngine;

namespace DiveProtocol.Bosses
{
    /// <summary>
    /// Drives a stationary boss intro, animation lifecycle, and fixed gameplay position.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StationaryHalfBuriedBossController : MonoBehaviour
    {
        private static readonly int GettingUpState = Animator.StringToHash("GettingUp");
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int DeathState = Animator.StringToHash("Death");
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int DeadBool = Animator.StringToHash("Dead");

        [Header("References")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;
        [SerializeField] private HealthComponent _health;
        [SerializeField] private EnemyContactAttack _contactAttack;

        [Header("Intro")]
        [SerializeField, Min(0.01f)] private float _gettingUpDuration = 1f;
        [SerializeField] private float _initialVisualLocalY;
        [SerializeField] private float _finalVisualLocalY;
        [SerializeField] private bool _beginIntroOnStart = true;

        private PlayerSpawner _playerSpawner;
        private Vector3 _fixedWorldPosition;
        private Coroutine _introRoutine;
        private BossState _state = BossState.Dormant;

        public bool IsCombatEnabled => _state == BossState.Idle;
        public bool IsDead => _state == BossState.Dead;

        private void Awake()
        {
            _fixedWorldPosition = transform.position;

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
                _animator.SetBool(DeadBool, false);
            }

            _contactAttack?.SetAttackEnabled(false);
        }

        private void OnEnable()
        {
            SubscribeToHealth();
            SubscribeToPlayerSpawner();
        }

        private void Start()
        {
            if (_beginIntroOnStart)
            {
                BeginIntro();
            }
        }

        private void LateUpdate()
        {
            transform.position = _fixedWorldPosition;
        }

        private void OnDisable()
        {
            if (_introRoutine != null)
            {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }

            if (_health != null)
            {
                _health.Died -= HandleDied;
            }

            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
                _playerSpawner = null;
            }
        }

        /// <summary>
        /// Starts the one-shot getting-up sequence when the dormant boss is activated.
        /// </summary>
        public void BeginIntro()
        {
            if (_state != BossState.Dormant || IsDead)
            {
                return;
            }

            _introRoutine = StartCoroutine(PlayIntro());
        }

        /// <summary>
        /// Called by the fixed boss attack component when a valid contact attack begins.
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (_state != BossState.Idle || _animator == null)
            {
                return;
            }

            _animator.ResetTrigger(AttackTrigger);
            _animator.SetTrigger(AttackTrigger);
        }

        private IEnumerator PlayIntro()
        {
            _state = BossState.GettingUp;
            _contactAttack?.SetAttackEnabled(false);
            SetVisualY(_initialVisualLocalY);

            if (_animator != null)
            {
                _animator.Play(GettingUpState, 0, 0f);
            }

            float elapsed = 0f;
            while (elapsed < _gettingUpDuration)
            {
                elapsed += Time.deltaTime;
                SetVisualY(Mathf.Lerp(_initialVisualLocalY, _finalVisualLocalY, Mathf.Clamp01(elapsed / _gettingUpDuration)));
                yield return null;
            }

            SetVisualY(_finalVisualLocalY);
            if (IsDead)
            {
                yield break;
            }

            _state = BossState.Idle;
            if (_animator != null)
            {
                _animator.Play(IdleState, 0, 0f);
            }

            _contactAttack?.SetAttackEnabled(true);
            _introRoutine = null;
        }

        private void SubscribeToHealth()
        {
            if (_health == null)
            {
                _health = GetComponent<HealthComponent>();
            }

            if (_health == null)
            {
                return;
            }

            _health.Died -= HandleDied;
            _health.Died += HandleDied;
        }

        private void SubscribeToPlayerSpawner()
        {
            PlayerSpawner playerSpawner = FindFirstObjectByType<PlayerSpawner>();
            if (_playerSpawner == playerSpawner)
            {
                return;
            }

            if (_playerSpawner != null)
            {
                _playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
            }

            _playerSpawner = playerSpawner;
            if (_playerSpawner == null)
            {
                return;
            }

            _playerSpawner.PlayerSpawned += HandlePlayerSpawned;
            if (_playerSpawner.SpawnedPlayer != null)
            {
                HandlePlayerSpawned(_playerSpawner.SpawnedPlayer);
            }
        }

        private void HandlePlayerSpawned(Transform player)
        {
            _contactAttack?.SetTarget(player);
        }

        private void HandleDied(HealthComponent health)
        {
            if (health != _health || _state == BossState.Dead)
            {
                return;
            }

            _state = BossState.Dead;
            _contactAttack?.SetAttackEnabled(false);
            if (_introRoutine != null)
            {
                StopCoroutine(_introRoutine);
                _introRoutine = null;
            }

            if (_animator != null)
            {
                _animator.SetBool(DeadBool, true);
                _animator.Play(DeathState, 0, 0f);
            }
        }

        private void SetVisualY(float localY)
        {
            if (_visualRoot == null)
            {
                return;
            }

            Vector3 position = _visualRoot.localPosition;
            position.y = localY;
            _visualRoot.localPosition = position;
        }

        private enum BossState
        {
            Dormant,
            GettingUp,
            Idle,
            Dead
        }
    }
}
