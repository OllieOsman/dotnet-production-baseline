using System.Diagnostics;

namespace DotnetProductionBaseline.Api.Middleware
{
    public sealed class RequestContextLoggingScopeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestContextLoggingScopeMiddleware> _logger;

        public RequestContextLoggingScopeMiddleware(
            RequestDelegate next,
            ILogger<RequestContextLoggingScopeMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId =
                context.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : null;

            var activity = Activity.Current;

            var scope = new Dictionary<string, object?>
            {
                ["correlation_id"] = correlationId,
                ["trace_id"] = activity?.TraceId.ToString(),
                ["span_id"] = activity?.SpanId.ToString()
            };

            using (_logger.BeginScope(scope))
            {
                await _next(context);
            }
        }
    }

}
