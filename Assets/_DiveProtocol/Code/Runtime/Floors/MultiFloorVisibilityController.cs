using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Controls visual floor visibility for multi-floor levels.</summary>
    [DisallowMultipleComponent]
    public sealed class MultiFloorVisibilityController : MonoBehaviour
    {
        [SerializeField] private FloorVisibilityGroup _floor01;
        [SerializeField] private FloorVisibilityGroup _floor02;
        [SerializeField] private FloorVisibilityState _startingState = FloorVisibilityState.Floor01Only;

        public FloorVisibilityState CurrentState { get; private set; }
        public FloorVisibilityGroup Floor01 => _floor01;
        public FloorVisibilityGroup Floor02 => _floor02;

        private void Start()
        {
            ApplyState(_startingState, force: true);
        }

        public void Configure(FloorVisibilityGroup floor01, FloorVisibilityGroup floor02)
        {
            _floor01 = floor01 != null ? floor01 : _floor01;
            _floor02 = floor02 != null ? floor02 : _floor02;
        }

        public void SetStartingState(FloorVisibilityState state)
        {
            _startingState = state;
        }

        public void ApplyState(FloorVisibilityState state, bool force = false)
        {
            if (!force && CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            var showFloor01 = state == FloorVisibilityState.Floor01Only || state == FloorVisibilityState.TransitionBoth;
            var showFloor02 = state == FloorVisibilityState.Floor02Only || state == FloorVisibilityState.TransitionBoth;

            _floor01?.SetVisualsVisible(showFloor01);
            _floor02?.SetVisualsVisible(showFloor02);
        }
    }
}
