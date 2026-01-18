namespace DotnetProductionBaseline.Api.Options
{
    public sealed class DatabaseOptions
    {
        public const string SectionName = "Database";

        public string ConnectionString { get; set; } = string.Empty;

        // keep it small so health checks don’t hang your readiness endpoint
        public int HealthCheckTimeoutMs { get; set; } = 500;
    }
}
