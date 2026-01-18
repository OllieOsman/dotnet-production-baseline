namespace DotnetProductionBaseline.Api.Healthcheck;

public sealed class StartupWarmupHostedService : BackgroundService
{
    private readonly ApplicationLifetimeState _state;
    private readonly ILogger<StartupWarmupHostedService> _logger;

    public StartupWarmupHostedService(
        ApplicationLifetimeState state,
        ILogger<StartupWarmupHostedService> logger)
    {
        _state = state;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Startup warmup beginning. Marking service NOT READY.");
        _state.MarkNotReady();

        try
        {
            // Put real warmup work here later:
            // - migrations
            // - cache preloads
            // - dependency handshake
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

            _state.MarkReady();
            _logger.LogInformation("Startup warmup complete. Service marked READY.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Startup warmup canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup warmup failed. Service will remain NOT READY.");
            throw; // fail-fast is a good baseline default
        }

        // Keep the hosted service alive until shutdown (optional).
        // This avoids it being treated as "completed work" in some mental models.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
