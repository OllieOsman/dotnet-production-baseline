using System.Diagnostics;

namespace DotnetProductionBaseline.Api.Middleware;

public sealed class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly TimeSpan _slowRequestThreshold = TimeSpan.FromMilliseconds(750);

    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await next(context);
            sw.Stop();

            var statusCode = context.Response.StatusCode;

            // Avoid noisy logs for health checks if you want:
            // if (context.Request.Path.StartsWithSegments("/health")) return;

            var level = sw.Elapsed >= _slowRequestThreshold
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(level,
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                sw.Elapsed.TotalMilliseconds);
        }
        catch
        {
            sw.Stop();
            // Exception middleware will log details; this is optional.
            _logger.LogDebug("Request aborted after {ElapsedMs} ms", sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
