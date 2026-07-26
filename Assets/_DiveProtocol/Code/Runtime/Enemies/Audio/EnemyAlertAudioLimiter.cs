using System.Collections.Generic;

namespace DiveProtocol
{
    /// <summary>
    /// Global voice limiter for individual enemy alert growls/groans.
    /// </summary>
    public static class EnemyAlertAudioLimiter
    {
        public const int DefaultMaxConcurrentAlertVoices = 2;

        private static readonly HashSet<EnemyAlertAudioEmitter> ActiveEmitters = new();
        private static int _maxConcurrentAlertVoices = DefaultMaxConcurrentAlertVoices;

        public static int MaxConcurrentAlertVoices
        {
            get => _maxConcurrentAlertVoices;
            set => _maxConcurrentAlertVoices = value < 1 ? 1 : value;
        }

        public static int ActiveCount => ActiveEmitters.Count;

        /// <summary>
        /// Attempts to reserve a voice slot for an enemy alert sound.
        /// </summary>
        public static bool TryAcquire(EnemyAlertAudioEmitter emitter)
        {
            if (emitter == null)
            {
                return false;
            }

            if (ActiveEmitters.Contains(emitter))
            {
                return true;
            }

            PruneNullEmitters();

            if (ActiveEmitters.Count >= MaxConcurrentAlertVoices)
            {
                return false;
            }

            return ActiveEmitters.Add(emitter);
        }

        /// <summary>
        /// Releases a previously reserved alert voice slot.
        /// </summary>
        public static void Release(EnemyAlertAudioEmitter emitter)
        {
            if (emitter == null)
            {
                return;
            }

            ActiveEmitters.Remove(emitter);
        }

        private static void PruneNullEmitters()
        {
            ActiveEmitters.RemoveWhere(emitter => emitter == null);
        }
    }
}
