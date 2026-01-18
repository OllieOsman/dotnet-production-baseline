namespace DotnetProductionBaseline.Api.Options
{
    public sealed class ProductionBaselineOptions
    {
        public bool EnableRequestLogging { get; set; } = true;
        public int SlowRequestThresholdMs { get; set; } = 750;
        public string[] SuppressRequestLogPathPrefixes { get; set; } =
            ["/health", "/swagger", "/openapi"];
    }
}
