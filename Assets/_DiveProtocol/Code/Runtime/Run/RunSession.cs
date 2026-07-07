namespace DiveProtocol
{
    /// <summary>Minimal in-memory state for one run.</summary>
    public sealed class RunSession
    {
        internal RunSession(int seed)
        {
            Seed = seed;
            IsActive = true;
        }

        public int Seed { get; }
        public bool IsActive { get; private set; }

        internal void MarkEnded()
        {
            IsActive = false;
        }
    }
}
