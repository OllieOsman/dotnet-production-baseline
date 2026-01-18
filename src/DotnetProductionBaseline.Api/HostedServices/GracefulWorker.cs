namespace DotnetProductionBaseline.Api.HostedServices;

public sealed class GracefulWorker : BackgroundService
{
    private readonly ILogger<GracefulWorker> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public GracefulWorker(
        ILogger<GracefulWorker> logger,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GracefulWorker started.");

        // Optional: if you later add a queue consumer, you can pause intake here.
        using var stoppingReg = _lifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("ApplicationStopping signaled. Worker will stop accepting new work.");
        });

        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("GracefulWorker cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GracefulWorker failed unexpectedly.");
            throw;
        }
        finally
        {
            // Best-effort cleanup (close connections, flush buffers, etc.)
            _logger.LogInformation("GracefulWorker stopped.");
        }
    }

    private async Task DoWorkOnceAsync(CancellationToken ct)
    {
        // Keep iterations short and cancellation-aware.
        _logger.LogInformation("Worker tick started.");

        // Simulated work – replace with real unit of work later.
        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        _logger.LogInformation("Worker tick complete.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GracefulWorker StopAsync invoked.");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("GracefulWorker StopAsync completed.");
    }
}