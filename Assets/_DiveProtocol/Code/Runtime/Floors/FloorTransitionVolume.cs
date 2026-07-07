using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Trigger volume used by stairs/elevators to switch floor visibility state.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FloorTransitionVolume : MonoBehaviour
    {
        [SerializeField] private MultiFloorVisibilityController _controller;
        [SerializeField] private FloorVisibilityState _stateOnEnter = FloorVisibilityState.TransitionBoth;
        [SerializeField] private FloorVisibilityState _stateOnExit = FloorVisibilityState.TransitionBoth;
        [SerializeField] private bool _applyOnExit;

        private bool _loggedMissingController;

        private void Reset()
        {
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        public void Configure(MultiFloorVisibilityController controller, FloorVisibilityState stateOnEnter, FloorVisibilityState stateOnExit, bool applyOnExit)
        {
            _controller = controller != null ? controller : _controller;
            _stateOnEnter = stateOnEnter;
            _stateOnExit = stateOnExit;
            _applyOnExit = applyOnExit;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (!HasController())
            {
                return;
            }

            _controller.ApplyState(_stateOnEnter);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_applyOnExit || !IsPlayer(other))
            {
                return;
            }

            if (!HasController())
            {
                return;
            }

            _controller.ApplyState(_stateOnExit);
        }

        private bool HasController()
        {
            if (_controller != null)
            {
                return true;
            }

            if (!_loggedMissingController)
            {
                _loggedMissingController = true;
                Debug.LogWarning(
                    $"[{nameof(FloorTransitionVolume)}] '{name}' requires a MultiFloorVisibilityController reference.",
                    this);
            }

            return false;
        }

        private static bool IsPlayer(Collider other)
        {
            return other != null &&
                   (other.GetComponentInParent<Interaction.PlayerInteractor>() != null ||
                    other.GetComponentInParent<PlayerMovement>() != null ||
                    other.GetComponentInParent<CharacterController>() != null);
        }
    }
}
