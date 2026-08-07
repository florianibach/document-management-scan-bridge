using System.Net.Http.Headers;
using System.Net.Http.Json;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Infrastructure.Paperless;

public sealed class PaperlessConnectionTester(IHttpClientFactory clients) : IPaperlessConnectionTester
{
    public async Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> ValidateAsync(string baseUrl, string apiToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clients.CreateClient("paperless-validation");
            async Task<HttpResponseMessage> Get(string path)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path));
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiToken);
                return await client.SendAsync(request, cancellationToken);
            }
            using var permission = await Get("api/documents/?page_size=1");
            if (!permission.IsSuccessStatusCode) return (Failure(permission.StatusCode), null);
            async Task<PaperlessChoice[]> Choices(string path)
            {
                using var response = await Get(path); if (!response.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
                var page = await response.Content.ReadFromJsonAsync<Page>(cancellationToken) ?? new([]);
                return page.Results.OrderBy(x => x.Name).ToArray();
            }
            var metadata = new PaperlessMetadata(await Choices("api/correspondents/?page_size=100000"), await Choices("api/document_types/?page_size=100000"), await Choices("api/tags/?page_size=100000"));
            return (new(true, "Connection, authentication, permissions, and metadata are valid."), metadata);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return (new(false, "Paperless hat nicht rechtzeitig geantwortet.", PaperlessFailure.Network), null); }
        catch (HttpRequestException) { return (new(false, "Paperless or the required metadata cannot be reached.", PaperlessFailure.Network), null); }
    }
    private static PaperlessResult Failure(System.Net.HttpStatusCode status) => status switch
    {
        System.Net.HttpStatusCode.Unauthorized => new(false, "Authentication failed. Check the API token.", PaperlessFailure.Authentication),
        System.Net.HttpStatusCode.Forbidden => new(false, "Dem API-Token fehlen erforderliche Leseberechtigungen.", PaperlessFailure.Authorization),
        _ => new(false, $"Paperless rejected validation (HTTP {(int)status}).", PaperlessFailure.Server)
    };
    private sealed record Page(PaperlessChoice[] Results);
}
