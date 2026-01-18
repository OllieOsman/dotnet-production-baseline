using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetProductionBaseline.Api.Healthcheck
{
    public class ReadinessHealthcheck : IHealthCheck
    {
        private readonly ApplicationLifetimeState _state;

        public ReadinessHealthcheck(ApplicationLifetimeState state) => _state = state;

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
            _state.IsReady
                ? HealthCheckResult.Healthy("Service is ready.")
                : HealthCheckResult.Unhealthy("Service is not ready."));
        }
    }
}
