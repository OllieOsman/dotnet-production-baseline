using DotnetProductionBaseline.Api.Http;
using DotnetProductionBaseline.Api.Middleware;
using DotnetProductionBaseline.Api.Options;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Retry;

namespace DotnetProductionBaseline.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IServiceCollection AddProductionBaseline(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProductionBaselineOptions>()
            .Bind(configuration.GetSection("ProductionBaseline"))
            .Validate(o => o.SlowRequestThresholdMs > 0, "SlowRequestThresholdMs must be > 0")
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Database:ConnectionString is required")
            .Validate(o => o.HealthCheckTimeoutMs > 0 && o.HealthCheckTimeoutMs <= 5000, "Database:HealthCheckTimeoutMs must be 1-5000")
            .ValidateOnStart();

        services.AddOptions<OutboundHttpOptions>()
            .Bind(configuration.GetSection(OutboundHttpOptions.SectionName))
            .Validate(o => o.TimeoutMs is > 0 and <= 30000, "OutboundHttp:TimeoutMs must be 1-30000")
            .Validate(o => o.MaxRetryAttempts is >= 0 and <= 5, "OutboundHttp:MaxRetryAttempts must be 0-5")
            .Validate(o => o.BaseDelayMs is > 0 and <= 5000, "OutboundHttp:BaseDelayMs must be 1-5000")
            .ValidateOnStart();

        // HttpClient with resilience policies (can be registered multiple times for different APIs)
        services.AddHttpClient<ISampleExternalApi, SampleExternalApi>()
            .ConfigureHttpClient(c => c.Timeout = Timeout.InfiniteTimeSpan);

        // Centralized resilience pipeline configuration
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddResilienceHandler(ResiliencePipelines.Baseline, (builder, context) =>
            {
                var opts = context.ServiceProvider
                    .GetRequiredService<IOptions<OutboundHttpOptions>>()
                    .Value;

                // Per-attempt timeout
                builder.AddTimeout(TimeSpan.FromMilliseconds(opts.TimeoutMs));

                // Bounded retries + jitter + logging
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = opts.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(opts.BaseDelayMs),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,

                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(resp =>
                            (int)resp.StatusCode >= 500 ||
                            resp.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                            resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests),

                    OnRetry = args =>
                    {
                        var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger(OutboundHttpLog.Category);

                        var reason =
                            args.Outcome.Exception?.GetType().Name
                            ?? (args.Outcome.Result is HttpResponseMessage r ? $"HTTP {(int)r.StatusCode}" : "Unknown");

                        logger.LogDebug(
                            "Retrying outbound HTTP call. Attempt={Attempt} DelayMs={DelayMs} Reason={Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            reason);

                        return default;
                    }
                });
            });
        });

        // Typed client registration (once)
        services.AddHttpClient<ISampleExternalApi, SampleExternalApi>();

        // OpenTelemetry configuration
        services.AddOptions<OpenTelemetryOptions>()
            .Bind(configuration.GetSection(OpenTelemetryOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ServiceName), "OpenTelemetry:ServiceName is required")
            .ValidateOnStart();

        var otelOptions = configuration
            .GetSection(OpenTelemetryOptions.SectionName)
            .Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        // Shared resource builder for both tracing and metrics
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: otelOptions.ServiceName,
                serviceVersion: otelOptions.ServiceVersion);

        // Note: In production, you'd typically use an exporter like OTLP to send data to a backend (e.g., New Relic, Datadog, Splunk, etc.)
        services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                if (!otelOptions.Enabled)
                    return;

                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Reduce noise from probes
                        options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation();

                if (otelOptions.UseConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                if (!otelOptions.Enabled)
                    return;

                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                // For metrics, you would typically use a Prometheus exporter in production. Console exporter is not ideal for metrics.
                // metrics.AddPrometheusExporter();
            });

        services.AddTransient<CorrelationIdMiddleware>();
        services.AddTransient<RequestLoggingMiddleware>();
        services.AddTransient<ExceptionHandlingMiddleware>();
        services.AddTransient<SecurityHeadersMiddleware>();
        return services;
    }

    public static IApplicationBuilder UseProductionBaseline(this IApplicationBuilder app)
    {
        // Order matters:
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
