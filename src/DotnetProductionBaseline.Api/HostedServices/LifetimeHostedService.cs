
using DotnetProductionBaseline.Api.Options;

namespace DotnetProductionBaseline.Api.HostedServices
{
    public class LifetimeHostedService : IHostedService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ApplicationLifetimeState _state;

        public LifetimeHostedService(IHostApplicationLifetime lifetime, ApplicationLifetimeState state)
        {
            _lifetime = lifetime;
            _state = state;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Mark "started" once the host has fully started.
            _lifetime.ApplicationStarted.Register(_state.MarkStarted);

            // Mark "stopping" as soon as shutdown begins.
            _lifetime.ApplicationStopping.Register(_state.MarkStopping);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
