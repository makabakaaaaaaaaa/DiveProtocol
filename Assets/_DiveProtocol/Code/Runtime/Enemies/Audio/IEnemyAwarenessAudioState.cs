namespace DiveProtocol
{
    /// <summary>
    /// Read-only enemy awareness state used by audio systems without changing AI behavior.
    /// </summary>
    public interface IEnemyAwarenessAudioState
    {
        bool IsPlayerDetectedForAudio { get; }
        bool IsDeadForAudio { get; }
    }
}
