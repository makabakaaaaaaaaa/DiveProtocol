using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Optional corpse/readable hint extension for Dead Matter Whisper.
    /// </summary>
    public sealed class CorpseWhisperHint : MonoBehaviour
    {
        [SerializeField, TextArea] private string whisperHint = "它不像死了，只像是在等待。";

        public string GetWhisperHint(PlayerBuildController playerBuild)
        {
            if (playerBuild == null ||
                !playerBuild.HasUpgrade(BuildUpgradeId.Humus_DeadMatterWhisper))
            {
                return string.Empty;
            }

            return whisperHint;
        }
    }
}
