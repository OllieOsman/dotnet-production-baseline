
namespace DotnetProductionBaseline.Api.HostedServices
{
    public class GracefulWorker : BackgroundService
    {
        private readonly ILogger<GracefulWorker> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        public GracefulWorker(ILogger<GracefulWorker> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GracefulWorker started.");

            using var stoppingReg = _lifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogInformation("ApplicationStopping triggered. Worker will stop accepting new work.");
                // Example: flip your own "accepting work" flag, pause consumers, etc.
            });

            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    // Do one unit of work per tick
                    await DoWorkOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                _logger.LogInformation("GracefulWorker canceled.");
            }
            catch (Exception ex)
            {
                // Unexpected crash — log and rethrow so the host can decide what to do
                _logger.LogError(ex, "GracefulWorker failed unexpectedly.");
                throw;
            }
            finally
            {
                // Best-effort cleanup
                await CleanupAsync(CancellationToken.None);
                _logger.LogInformation("GracefulWorker stopped.");
            }
        }

        private async Task DoWorkOnceAsync(CancellationToken ct)
        {
            // Example pattern:
            // 1) Acquire external resources (connections, locks, etc.)
            // 2) Observe ct frequently
            // 3) Keep each iteration bounded in time

            _logger.LogInformation("Worker tick: doing work...");

            // Simulated work
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        private Task CleanupAsync(CancellationToken ct)
        {
            // Close connections, flush buffers, etc.
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GracefulWorker StopAsync called.");
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("GracefulWorker StopAsync finished.");
        }
    }
}
