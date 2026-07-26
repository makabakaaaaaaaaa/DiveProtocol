using UnityEngine;

namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Persistent meta progression flag that gates corpse reanimation before the first full clear.
    /// </summary>
    public static class CorpseReanimationMetaProgress
    {
        private const string FinalBossClearedKey = "DiveProtocol.HasClearedFinalBossOnce";

        public static bool HasClearedFinalBossOnce => PlayerPrefs.GetInt(FinalBossClearedKey, 0) == 1;

        /// <summary>
        /// Marks the final boss as cleared once, enabling corpse reanimation in future runs.
        /// </summary>
        public static void MarkFinalBossCleared()
        {
            PlayerPrefs.SetInt(FinalBossClearedKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clears the final-boss flag for testing only.
        /// </summary>
        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(FinalBossClearedKey);
            PlayerPrefs.Save();
        }
    }
}
