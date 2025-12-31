namespace DotnetProductionBaseline.Api.Options
{
    using System.Threading;

    public sealed class ApplicationLifetimeState
    {
        // 0 = not started, 1 = started
        private int _started;

        // 0 = not stopping, 1 = stopping
        private int _stopping;

        public bool HasStarted => Volatile.Read(ref _started) == 1;
        public bool IsStopping => Volatile.Read(ref _stopping) == 1;

        public void MarkStarted() => Interlocked.Exchange(ref _started, 1);
        public void MarkStopping() => Interlocked.Exchange(ref _stopping, 1);
    }
}
