namespace DotnetProductionBaseline.Api.Http
{
    public sealed class SampleExternalApi : ISampleExternalApi
    {
        private readonly HttpClient _http;

        public SampleExternalApi(HttpClient http) => _http = http;

        public async Task<string> GetIpAsync(CancellationToken ct)
        {
            // Simple, public endpoint (fine for demo). Replace in real apps.
            using var resp = await _http.GetAsync("https://api.ipify.org?format=json", ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }
    }
}
