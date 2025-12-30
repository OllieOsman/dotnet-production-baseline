using Microsoft.Extensions.Primitives;

namespace DotnetProductionBaseline.Api.Middleware;

public sealed class CorrelationIdMiddleware : IMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Put it somewhere standard
        context.TraceIdentifier = correlationId;

        // Ensure response contains it
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["correlation_id"] = correlationId
        }))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out StringValues values) &&
            !StringValues.IsNullOrEmpty(values))
        {
            // Use first value
            return values.ToString();
        }

        // Use Activity id if present, else GUID
        return System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString("n");
    }
}
