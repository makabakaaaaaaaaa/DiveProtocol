namespace DiveProtocol.Enemies.CorpseReanimation
{
    /// <summary>
    /// Runtime state for a corpse that may reanimate in later runs.
    /// </summary>
    public enum CorpseReanimationState
    {
        Dormant,
        CheckedAndStill,
        Reanimating,
        Active,
        Disabled
    }
}
