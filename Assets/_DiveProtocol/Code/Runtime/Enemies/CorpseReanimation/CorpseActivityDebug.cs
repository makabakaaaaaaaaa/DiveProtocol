using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Optional runtime debug helper for corpse reanimation meta progress and activity.
    /// </summary>
    public sealed class CorpseActivityDebug : MonoBehaviour
    {
        [SerializeField] private bool enableDebugKeys;
        [SerializeField] private int debugSeed = 12345;

        private void Update()
        {
            if (!enableDebugKeys)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                MarkFinalBossCleared();
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                ResetForDebug();
            }

            if (keyboard.f11Key.wasPressedThisFrame)
            {
                GenerateActivityFromDebugSeed();
            }

            if (keyboard.f12Key.wasPressedThisFrame)
            {
                PrintState();
            }
#endif
        }

        public void MarkFinalBossCleared()
        {
            CorpseReanimationMetaProgress.MarkFinalBossCleared();
            Debug.Log("[Corpse] Final boss cleared flag set.", this);
        }

        public void ResetForDebug()
        {
            CorpseReanimationMetaProgress.ResetForDebug();
            Debug.Log("[Corpse] Final boss cleared flag reset for debug.", this);
        }

        public void GenerateActivityFromDebugSeed()
        {
            float activity = CorpseActivityProvider.GenerateFromSeed(debugSeed);
            Debug.Log($"[Corpse] Generated activity {activity:0.000} ({CorpseActivityProvider.GetActivityLabel()}) from seed {debugSeed}.", this);
        }

        public void SetActivity(float activity)
        {
            CorpseActivityProvider.SetActivity(activity);
            Debug.Log($"[Corpse] Set activity {CorpseActivityProvider.CurrentActivity:0.000} ({CorpseActivityProvider.GetActivityLabel()}).", this);
        }

        public void PrintState()
        {
            Debug.Log(
                $"[Corpse] HasClearedFinalBossOnce={CorpseReanimationMetaProgress.HasClearedFinalBossOnce}, Activity={CorpseActivityProvider.CurrentActivity:0.000}, Label={CorpseActivityProvider.GetActivityLabel()}",
                this);
        }
    }
}
