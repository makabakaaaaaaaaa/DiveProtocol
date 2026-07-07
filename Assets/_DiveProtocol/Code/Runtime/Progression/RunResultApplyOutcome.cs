namespace DiveProtocol
{
    /// <summary>Outcome of attempting to settle one run result.</summary>
    public readonly struct RunResultApplyOutcome
    {
        public RunResultApplyOutcome(RunResultApplyStatus status, int currencyGained)
        {
            Status = status;
            CurrencyGained = currencyGained;
        }

        public RunResultApplyStatus Status { get; }
        public int CurrencyGained { get; }
    }
}
