namespace DotnetProductionBaseline.Api.Options
{
    public sealed class ApplicationLifetimeState
    {
        public bool IsReady { get; private set; } = false;

        public void MarkReady() => IsReady = true;
        public void MarkNotReady() => IsReady = false;
    }
}
