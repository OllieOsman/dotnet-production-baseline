namespace DotnetProductionBaseline.Api.Middleware;

public sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;

            _logger.LogError(ex,
                "Unhandled exception. CorrelationId={CorrelationId} Path={Path}",
                correlationId,
                context.Request.Path.Value);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var body = new
            {
                type = "https://httpstatuses.com/500",
                title = "An unexpected error occurred.",
                status = 500,
                traceId = correlationId,
                detail = _env.IsDevelopment() ? ex.ToString() : null
            };

            await context.Response.WriteAsJsonAsync(body);
        }
    }
}
