namespace DotnetProductionBaseline.Api.Options;

public sealed class BaselineOptions
{
    public string CorrelationHeaderName { get; set; } = "X-Correlation-Id";
    public bool IncludeTraceIdWhenMissing { get; set; } = true;

    public bool EnableRequestLogging { get; set; } = true;
    public bool LogRequestHeaders { get; set; } = false;   // default safe
    public bool LogResponseHeaders { get; set; } = false;  // default safe

    public int SlowRequestThresholdMs { get; set; } = 1500;

    public bool EnableSecurityHeaders { get; set; } = true;
}
