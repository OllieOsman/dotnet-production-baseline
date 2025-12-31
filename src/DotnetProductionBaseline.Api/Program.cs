using DotnetProductionBaseline.Api.Extensions;
using DotnetProductionBaseline.Api.Healthcheck;
using DotnetProductionBaseline.Api.HostedServices;
using DotnetProductionBaseline.Api.Options;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProductionBaseline();

// Application lifetime state to track readiness
builder.Services.AddSingleton<ApplicationLifetimeState>();

builder.Services.AddHostedService<LifetimeHostedService>();
builder.Services.AddHostedService<GracefulWorker>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register health checks
builder.Services.AddHealthChecks()
    // Liveness
    .AddCheck("self", () => HealthCheckResult.Healthy("The application is running."), tags: ["live"])
    .AddCheck<ReadinessHealthcheck>("readiness", tags: ["ready"])
    // Readiness check: simulate DB or external service check
    .AddCheck("database", () =>
    {
        bool dbIsUp = true; // Replace with real DB check
        return dbIsUp
            ? HealthCheckResult.Healthy("Database OK")
            : HealthCheckResult.Unhealthy("Database DOWN");
    }, tags: ["ready"]);

var app = builder.Build();

// Register application lifetime events
var lifetime = app.Lifetime;
var state = app.Services.GetRequiredService<ApplicationLifetimeState>();

lifetime.RegisterApplicationLifetimeState(state);

app.UseProductionBaseline();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// Liveness endpoint
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteResponse
});

// Readiness endpoint
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteResponse
});

app.MapControllers();

app.Run();


// Custom JSON response writer
static Task WriteResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var result = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            description = e.Value.Description
        }),
        duration = report.TotalDuration.TotalMilliseconds
    });
    return context.Response.WriteAsync(result);
}