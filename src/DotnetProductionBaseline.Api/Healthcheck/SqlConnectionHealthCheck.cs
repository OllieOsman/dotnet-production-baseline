using DotnetProductionBaseline.Api.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DotnetProductionBaseline.Api.Healthcheck
{
    public sealed class SqlConnectionHealthCheck : IHealthCheck
    {
        private readonly DatabaseOptions _opts;

        public SqlConnectionHealthCheck(IOptions<DatabaseOptions> opts)
            => _opts = opts.Value;

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_opts.ConnectionString))
                return HealthCheckResult.Unhealthy("Database connection string is not configured.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(_opts.HealthCheckTimeoutMs));

            try
            {
                await using var conn = new SqlConnection(_opts.ConnectionString);
                await conn.OpenAsync(cts.Token);

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                cmd.CommandTimeout = Math.Max(1, _opts.HealthCheckTimeoutMs / 1000);

                _ = await cmd.ExecuteScalarAsync(cts.Token);

                return HealthCheckResult.Healthy("Database reachable.");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"Database health check timed out after {_opts.HealthCheckTimeoutMs}ms.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check failed.", ex);
            }
        }
    }
}
