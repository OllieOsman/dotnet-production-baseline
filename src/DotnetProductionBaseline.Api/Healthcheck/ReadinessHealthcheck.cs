using DotnetProductionBaseline.Api.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetProductionBaseline.Api.Healthcheck
{
    public class ReadinessHealthcheck : IHealthCheck
    {
        private readonly ApplicationLifetimeState _state;

        public ReadinessHealthcheck(ApplicationLifetimeState state)
        {
            _state = state;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // not ready until app is started
            if (!_state.HasStarted)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Application has not started yet."));
            }

            // as soon as a shutdown is initiated, immediately fail readiness so traffic drains
            if (_state.IsStopping)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Application is stopping; not ready."));
            }

            return Task.FromResult(HealthCheckResult.Healthy("Application is healthy."));
        }
    }
}
