using DotnetProductionBaseline.Api.Http;
using DotnetProductionBaseline.Api.Middleware;
using DotnetProductionBaseline.Api.Options;
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
            // Important: DO NOT rely solely on HttpClient.Timeout (it’s coarse and can be problematic).
            // We’ll do a resilience timeout per request.
            .AddStandardResilienceHandler(options =>
            {
                // We'll override defaults using our own config via post-config below.
            });

        services.ConfigureHttpClientDefaults(static http =>
        {
            _ = http.AddResilienceHandler(ResiliencePipelines.Baseline, (builder, context) =>
            {
                var opts = context.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboundHttpOptions>>()
                    .Value;

                // 1) Timeout (per attempt)
                builder.AddTimeout(TimeSpan.FromMilliseconds(opts.TimeoutMs));

                // 2) Retry (bounded, jittered backoff, transient failures only)
                _ = builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = opts.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(opts.BaseDelayMs),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,

                    // Only retry transient failures: 5xx, 408, network errors
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .HandleResult(r =>
                                r is HttpResponseMessage resp &&
                                ((int)resp.StatusCode >= 500 ||
                                resp.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                                resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)),

                    OnRetry = args =>
                    {
                        var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger(OutboundHttpLog.Category);

                        string reason;

                        if (args.Outcome.Exception != null)
                        {
                            reason = args.Outcome.Exception.GetType().Name;
                        }
                        else if (args.Outcome.Result is HttpResponseMessage resp)
                        {
                            reason = $"HTTP {(int)resp.StatusCode}";
                        }
                        else
                        {
                            reason = "Unknown";
                        }

                        logger.LogDebug(
                            "Retrying outbound HTTP call. Attempt={Attempt} DelayMs={DelayMs} Reason={Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            reason);

                        return default;
                    }
                });
            });

            // Apply the baseline pipeline to every client unless overridden
            http.AddHttpMessageHandler(sp =>
            {
                // Use DelegatingHandler, not HttpClientHandler, as required by AddHttpMessageHandler.
                return new DelegatingHandlerStub();
            });
        });

        services.AddHttpClient<ISampleExternalApi, SampleExternalApi>()
            .AddResilienceHandler(ResiliencePipelines.Baseline, (builder, context) =>
            {
                var opts = context.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<OutboundHttpOptions>>()
                    .Value;

                builder.AddTimeout(TimeSpan.FromMilliseconds(opts.TimeoutMs));
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = opts.MaxRetryAttempts,
                    Delay = TimeSpan.FromMilliseconds(opts.BaseDelayMs),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r =>
                            r is HttpResponseMessage resp &&
                            ((int)resp.StatusCode >= 500 ||
                            resp.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                            resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests))
                });
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

internal class DelegatingHandlerStub : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // No-op handler, just pass through to the next handler in the pipeline.
        return base.SendAsync(request, cancellationToken);
    }
}
