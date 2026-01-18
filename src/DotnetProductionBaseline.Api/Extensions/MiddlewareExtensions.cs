using DotnetProductionBaseline.Api.Middleware;
using DotnetProductionBaseline.Api.Options;

namespace DotnetProductionBaseline.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IServiceCollection AddProductionBaseline(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProductionBaselineOptions>()
            .Bind(configuration.GetSection("ProductionBaseline"))
            .Validate(o => o.SlowRequestThresholdMs > 0, "SlowRequestThresholdMs must be > 0")
            .ValidateOnStart();

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
