using DotnetProductionBaseline.Api.Middleware;
using DotnetProductionBaseline.Api.Options;

namespace DotnetProductionBaseline.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseProductionBaseline(
        this IApplicationBuilder app,
        Action<BaselineOptions>? configure = null)
    {
        var options = new BaselineOptions();
        configure?.Invoke(options);

        // Order matters: correlation -> exception -> logging -> security -> rest
        app.UseMiddleware<CorrelationIdMiddleware>(options.CorrelationHeaderName, options.IncludeTraceIdWhenMissing);
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (options.EnableRequestLogging)
        {
            app.UseMiddleware<RequestLoggingMiddleware>(
                options.SlowRequestThresholdMs,
                options.LogRequestHeaders,
                options.LogResponseHeaders);
        }

        if (options.EnableSecurityHeaders)
            app.UseMiddleware<SecurityHeadersMiddleware>();

        return app;
    }
}
