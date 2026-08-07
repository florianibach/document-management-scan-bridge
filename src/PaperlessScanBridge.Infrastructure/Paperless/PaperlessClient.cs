using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Infrastructure.Paperless;

public sealed class PaperlessClient(HttpClient http, IProfileServiceConfigurationService configurations, TemporaryStorageOptions storage, ILogger<PaperlessClient> logger) : IPaperlessClient
{
    public async Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken = default)
    {
        var options = await configurations.GetEffectiveAsync(cancellationToken);
        if (!options.IsConfigured) return new(false, "No effective Paperless configuration is available.", PaperlessFailure.Configuration);
        try
        {
            using var response = await SendAsync(options, HttpMethod.Get, "api/documents/?page_size=1", null, cancellationToken);
            return response.IsSuccessStatusCode ? new(true, "Connection and API permission are valid.") : Failure(response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(false, "Paperless hat nicht rechtzeitig geantwortet.", PaperlessFailure.Network); }
        catch (HttpRequestException) { return new(false, "Paperless cannot be reached over the network.", PaperlessFailure.Network); }
    }

    public async Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var check = await CheckConnectivityAsync(cancellationToken);
        if (!check.Succeeded) return (check, null);
        var options = await configurations.GetEffectiveAsync(cancellationToken);
        try
        {
            var correspondents = await GetChoicesAsync(options, "api/correspondents/?page_size=100000", cancellationToken);
            var types = await GetChoicesAsync(options, "api/document_types/?page_size=100000", cancellationToken);
            var tags = await GetChoicesAsync(options, "api/tags/?page_size=100000", cancellationToken);
            return (new(true, "Metadaten wurden geladen."), new(correspondents, types, tags));
        }
        catch (PaperlessHttpException exception) { return (Failure(exception.StatusCode), null); }
        catch (HttpRequestException) { return (new(false, "Metadaten konnten wegen eines Netzwerkfehlers nicht geladen werden.", PaperlessFailure.Network), null); }
        catch (JsonException) { return (new(false, "Paperless returned invalid metadata.", PaperlessFailure.InvalidResponse), null); }
    }

    public async Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetFullPath(storage.Path), request.SessionId.ToString("N"), "document.pdf");
        if (!File.Exists(path)) return new(false, "The generated PDF is no longer available. Create it again.", PaperlessFailure.FileMissing);
        var options = await configurations.GetEffectiveAsync(cancellationToken);
        if (!options.IsConfigured) return new(false, "No effective Paperless configuration is available.", PaperlessFailure.Configuration);
        try
        {
            await using var stream = File.OpenRead(path);
            using var multipart = new MultipartFormDataContent();
            multipart.Add(new StreamContent(stream) { Headers = { ContentType = new MediaTypeHeaderValue("application/pdf") } }, "document", "scan.pdf");
            if (!string.IsNullOrWhiteSpace(request.Title)) multipart.Add(new StringContent(request.Title.Trim()), "title");
            if (request.CorrespondentId is { } correspondent) multipart.Add(new StringContent(correspondent.ToString()), "correspondent");
            if (request.DocumentTypeId is { } type) multipart.Add(new StringContent(type.ToString()), "document_type");
            foreach (var tag in request.TagIds.Distinct()) multipart.Add(new StringContent(tag.ToString()), "tags");
            using var content = new ProgressContent(multipart, progress);
            using var response = await SendAsync(options, HttpMethod.Post, "api/documents/post_document/", content, cancellationToken);
            if (!response.IsSuccessStatusCode) return Failure(response.StatusCode);
            var taskId = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim().Trim('"');
            logger.LogInformation("Paperless accepted upload for scan session {SessionId}.", request.SessionId);
            return new(true, "Paperless accepted the document and is processing it now.", TaskId: string.IsNullOrWhiteSpace(taskId) ? null : taskId);
        }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException) { return new(false, "Upload wegen eines Netzwerkfehlers fehlgeschlagen; die PDF bleibt erhalten.", PaperlessFailure.Network); }
    }

    private async Task<IReadOnlyList<PaperlessChoice>> GetChoicesAsync(EffectivePaperlessConfiguration options, string uri, CancellationToken token)
    {
        using var response = await SendAsync(options, HttpMethod.Get, uri, null, token);
        if (!response.IsSuccessStatusCode) throw new PaperlessHttpException(response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(token), cancellationToken: token);
        return json.RootElement.GetProperty("results").EnumerateArray().Select(item => new PaperlessChoice(item.GetProperty("id").GetInt32(), item.GetProperty("name").GetString() ?? "")).OrderBy(x => x.Name).ToArray();
    }

    private async Task<HttpResponseMessage> SendAsync(EffectivePaperlessConfiguration options, HttpMethod method, string uri, HttpContent? content, CancellationToken token)
    {
        using var request = new HttpRequestMessage(method, new Uri(new Uri(options.BaseUrl!.TrimEnd('/') + "/"), uri)) { Content = content };
        request.Headers.Authorization = new("Token", options.ApiToken);
        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
    }
    private static PaperlessResult Failure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => new(false, "Authentication failed. Check the API token.", PaperlessFailure.Authentication),
        HttpStatusCode.Forbidden => new(false, "Authentifiziert, aber die erforderliche Berechtigung fehlt.", PaperlessFailure.Authorization),
        >= HttpStatusCode.InternalServerError => new(false, $"Paperless meldet einen Serverfehler ({(int)status}).", PaperlessFailure.Server),
        _ => new(false, $"Paperless hat die Anfrage abgelehnt (HTTP {(int)status}).", PaperlessFailure.Unknown)
    };
    private sealed class PaperlessHttpException(HttpStatusCode statusCode) : Exception { public HttpStatusCode StatusCode { get; } = statusCode; }
}

internal sealed class ProgressContent : HttpContent
{
    private readonly HttpContent inner;
    private readonly IProgress<int>? progress;

    public ProgressContent(HttpContent inner, IProgress<int>? progress)
    {
        this.inner = inner;
        this.progress = progress;

        // HttpContent wrappers do not inherit the wrapped content headers. In particular,
        // dropping multipart/form-data (including its boundary) makes Paperless reject the
        // otherwise valid body with HTTP 415 Unsupported Media Type.
        foreach (var header in inner.Headers)
            Headers.TryAddWithoutValidation(header.Key, header.Value);
    }

    protected override bool TryComputeLength(out long length) { length = inner.Headers.ContentLength ?? -1; return length >= 0; }
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        await using var source = await inner.ReadAsStreamAsync(); var buffer = new byte[81920]; long sent = 0; var total = inner.Headers.ContentLength;
        int read; while ((read = await source.ReadAsync(buffer)) > 0) { await stream.WriteAsync(buffer.AsMemory(0, read)); sent += read; if (total > 0) progress?.Report((int)Math.Min(99, sent * 100 / total.Value)); }
    }
    protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
}
