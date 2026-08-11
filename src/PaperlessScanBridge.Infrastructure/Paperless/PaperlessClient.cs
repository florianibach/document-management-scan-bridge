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
        if (!options.IsConfigured) return ConfigurationFailure(options);
        try
        {
            using var response = await SendAsync(options, HttpMethod.Get, "api/documents/?page_size=1", null, cancellationToken);
            return response.IsSuccessStatusCode ? new(true, "Connection and API permission are valid.") : Failure(response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Failure(PaperlessFailure.Timeout, "Paperless did not respond before the timeout."); }
        catch (HttpRequestException) { return Failure(PaperlessFailure.Network, "Paperless cannot be reached over the network."); }
        catch (Exception exception) { return Unexpected(exception); }
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
            return (new(true, "Paperless metadata was loaded."), new(correspondents, types, tags));
        }
        catch (PaperlessHttpException exception) { return (Failure(exception.StatusCode), null); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return (Failure(PaperlessFailure.Timeout, "Paperless metadata did not arrive before the timeout."), null); }
        catch (HttpRequestException) { return (Failure(PaperlessFailure.Network, "Paperless metadata could not be loaded because of a network failure."), null); }
        catch (JsonException exception) { return (Failure(PaperlessFailure.InvalidResponse, "Paperless returned an invalid or unexpected metadata response.", exception), null); }
        catch (Exception exception) { return (Unexpected(exception), null); }
    }

    public async Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetFullPath(storage.Path), request.SessionId.ToString("N"), "document.pdf");
        if (!File.Exists(path)) return new(false, "The generated PDF is no longer available. Create it again.", PaperlessFailure.FileMissing);
        var options = await configurations.GetEffectiveAsync(cancellationToken);
        if (!options.IsConfigured) return ConfigurationFailure(options);
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
            if (string.IsNullOrWhiteSpace(taskId)) return Failure(PaperlessFailure.InvalidResponse, "Paperless returned an invalid upload acceptance response. The document was not marked as sent.");
            logger.LogInformation("Paperless accepted upload for scan session {SessionId}.", request.SessionId);
            return new(true, "Paperless accepted the document and is processing it now.", TaskId: string.IsNullOrWhiteSpace(taskId) ? null : taskId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return Failure(PaperlessFailure.Timeout, "The upload timed out; the PDF remains available for retry."); }
        catch (OperationCanceledException) { throw; }
        catch (HttpRequestException) { return Failure(PaperlessFailure.Network, "The upload failed because of a network error; the PDF remains available for retry."); }
        catch (Exception exception) { return Unexpected(exception, "The upload failed unexpectedly; the PDF remains available for retry."); }
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
    private PaperlessResult Failure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => Failure(PaperlessFailure.Authentication, "Paperless authentication failed. Check the API token."),
        HttpStatusCode.Forbidden => Failure(PaperlessFailure.Authorization, "Paperless denied the required permission. Check the token user's permissions."),
        >= HttpStatusCode.InternalServerError => Failure(PaperlessFailure.Server, "Paperless reported a server failure. Try again later."),
        _ => Failure(PaperlessFailure.InvalidResponse, "Paperless rejected the operation with an unexpected response.")
    };
    private PaperlessResult ConfigurationFailure(EffectivePaperlessConfiguration options)
    {
        var missing = !PaperlessUrlPolicy.TryParse(options.BaseUrl, out _) ? "URL" : "API token";
        return Failure(PaperlessFailure.Configuration, $"The effective Paperless {missing} is missing or invalid. Open Paperless settings to configure it.");
    }
    private PaperlessResult Failure(PaperlessFailure category, string message, Exception? exception = null)
    {
        var id = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        logger.LogWarning("Paperless operation failed ({Failure}); diagnostic ID {DiagnosticId}; exception type {ExceptionType}.",
            category, id, exception?.GetType().Name ?? "none");
        return new(false, message, category, DiagnosticId: id);
    }
    private PaperlessResult Unexpected(Exception exception, string message = "The Paperless operation failed unexpectedly. Try again.") =>
        Failure(PaperlessFailure.Unknown, message, exception);
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
