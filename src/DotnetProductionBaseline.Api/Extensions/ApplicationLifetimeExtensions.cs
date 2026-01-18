using DotnetProductionBaseline.Api.Healthcheck;

namespace DotnetProductionBaseline.Api.Extensions
{
    public static class ApplicationLifetimeExtensions
    {
        public static void RegisterApplicationLifetimeState(this IHostApplicationLifetime lifetime, ApplicationLifetimeState state)
        {
            // Default: not ready until warmup marks it ready
            state.MarkNotReady();

            lifetime.ApplicationStopped.Register(state.MarkNotReady);
        }
    }
}