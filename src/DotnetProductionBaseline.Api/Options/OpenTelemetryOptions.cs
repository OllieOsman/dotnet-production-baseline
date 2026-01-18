namespace DotnetProductionBaseline.Api.Options
{
    public sealed class OpenTelemetryOptions
    {
        public const string SectionName = "OpenTelemetry";

        public bool Enabled { get; set; } = true;

        // Service identity
        public string ServiceName { get; set; } = "dotnet-production-baseline";
        public string ServiceVersion { get; set; } = "0.1.0";

        // Exporters
        public bool UseConsoleExporter { get; set; } = true;
    }
}
