using DotnetProductionBaseline.Api.Options;

namespace DotnetProductionBaseline.Api.Extensions
{
  public static class ApplicationLifetimeExtensions
  {
    public static void RegisterApplicationLifetimeState(this IHostApplicationLifetime lifetime, ApplicationLifetimeState state)
    {
      lifetime.ApplicationStarted.Register(() =>
      {
        state.MarkStarted();
      });

      lifetime.ApplicationStopped.Register(() =>
      {
        state.MarkStopping();
      });
    }
  }
}