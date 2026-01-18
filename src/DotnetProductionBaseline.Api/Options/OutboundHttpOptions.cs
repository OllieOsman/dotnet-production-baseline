namespace DotnetProductionBaseline.Api.Options
{
    public sealed class OutboundHttpOptions
    {
        public const string SectionName = "OutboundHttp";

        public int TimeoutMs { get; set; } = 2000;

        public int MaxRetryAttempts { get; set; } = 2;

        public int BaseDelayMs { get; set; } = 200; // backoff base
    }
}
