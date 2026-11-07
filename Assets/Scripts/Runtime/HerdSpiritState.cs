namespace YesterdayConvoy
{
    /// <summary>One final death and its replacement share this immutable simulation timeline.</summary>
    public sealed class HerdSpiritState
    {
        public const double FallDuration = .7;
        public const double RiseDuration = .55;
        public string SourceId { get; }
        public string EntityId { get; }
        public double StartedAt { get; }
        public double ReadyAt => StartedAt + FallDuration + RiseDuration;

        internal HerdSpiritState(string sourceId, string entityId, double startedAt)
        { SourceId = sourceId; EntityId = entityId; StartedAt = startedAt; }
    }
}
