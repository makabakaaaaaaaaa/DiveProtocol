using UnityEngine;

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Run-scoped corpse activity value. Future run setup can seed this from RunState.Seed.
    /// </summary>
    public static class CorpseActivityProvider
    {
        private static float _currentActivity;

        public static float CurrentActivity => _currentActivity;

        public static void SetActivity(float value)
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(_currentActivity, clamped))
            {
                return;
            }

            _currentActivity = clamped;
            CorpseReanimationEvents.RaiseCorpseActivityChanged(_currentActivity);
        }

        public static float GenerateFromSeed(int seed)
        {
            System.Random random = new(seed);
            SetActivity((float)random.NextDouble());
            return _currentActivity;
        }

        public static string GetActivityLabel()
        {
            if (_currentActivity < 0.2f)
            {
                return "Dormant";
            }

            if (_currentActivity < 0.4f)
            {
                return "Low";
            }

            if (_currentActivity < 0.6f)
            {
                return "Unstable";
            }

            if (_currentActivity < 0.8f)
            {
                return "Active";
            }

            return "Violent";
        }
    }
}
