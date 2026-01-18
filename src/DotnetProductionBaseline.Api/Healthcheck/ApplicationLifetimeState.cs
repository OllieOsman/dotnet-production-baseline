namespace DotnetProductionBaseline.Api.Healthcheck
{
    using System.Threading;

    public sealed class ApplicationLifetimeState
    {
        // 0 = not ready, 1 = ready
        private int _ready;

        public bool IsReady => Volatile.Read(ref _ready) == 1;

        public void MarkReady() => Interlocked.Exchange(ref _ready, 1);
        public void MarkNotReady() => Interlocked.Exchange(ref _ready, 0);
    }
}
