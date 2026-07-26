using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Lightweight timed confusion marker for future corpse reanimation AI.
    /// </summary>
    public sealed class TemporaryConfusion : MonoBehaviour
    {
        private float _endsAtTime;

        public bool IsConfused => Time.time < _endsAtTime;

        public static void Apply(GameObject target, float durationSeconds)
        {
            if (target == null)
            {
                return;
            }

            TemporaryConfusion confusion = target.GetComponent<TemporaryConfusion>();
            if (confusion == null)
            {
                confusion = target.AddComponent<TemporaryConfusion>();
            }

            confusion._endsAtTime = Time.time + Mathf.Max(0.01f, durationSeconds);
        }
    }
}
