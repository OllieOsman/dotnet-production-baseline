using DotnetProductionBaseline.Api.Http;
using DotnetProductionBaseline.Api.Middleware;
using DotnetProductionBaseline.Api.Options;
using Microsoft.Extensions.Options;
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

        services.AddHttpClient<ISampleExternalApi, SampleExternalApi>()
            .ConfigureHttpClient(c => c.Timeout = Timeout.InfiniteTimeSpan);

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
