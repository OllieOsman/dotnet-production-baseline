namespace DotnetProductionBaseline.Api.Http
{
    public interface ISampleExternalApi
    {
        Task<string> GetIpAsync(CancellationToken ct);
    }
}
