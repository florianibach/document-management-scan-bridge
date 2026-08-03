using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Infrastructure.Paperless;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class PaperlessClientTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"paperless-client-{Guid.NewGuid():N}");
    [Fact]
    public async Task MapsMetadataAndMultipartUploadWithoutLeakingToken()
    {
        var handler = new Handler(); var client = Create(handler);
        var metadata = await client.GetMetadataAsync();
        Assert.True(metadata.Result.Succeeded); Assert.Equal("Example", metadata.Metadata!.Correspondents.Single().Name);
        var session = Guid.NewGuid(); Directory.CreateDirectory(Path.Combine(root, session.ToString("N"))); await File.WriteAllTextAsync(Path.Combine(root, session.ToString("N"), "document.pdf"), "pdf");
        var result = await client.UploadAsync(new(session, "Invoice", 1, 2, [3]));
        Assert.True(result.Succeeded); Assert.Equal("task-123", result.TaskId); Assert.Contains("name=title", handler.UploadBody); Assert.Contains("Invoice", handler.UploadBody); Assert.All(handler.AuthorizationValues, value => Assert.Equal("Token secret-token", value));
    }

    [Theory] [InlineData(HttpStatusCode.Unauthorized, PaperlessFailure.Authentication)] [InlineData(HttpStatusCode.Forbidden, PaperlessFailure.Authorization)] [InlineData(HttpStatusCode.InternalServerError, PaperlessFailure.Server)]
    public async Task ConnectivityDistinguishesHttpFailures(HttpStatusCode status, PaperlessFailure expected)
    { var result = await Create(new Handler(status)).CheckConnectivityAsync(); Assert.Equal(expected, result.Failure); }

    private PaperlessClient Create(HttpMessageHandler handler) => new(new HttpClient(handler) { BaseAddress = new("http://paperless/") }, new() { BaseUrl = "http://paperless", ApiToken = "secret-token" }, new() { Path = root }, NullLogger<PaperlessClient>.Instance);
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    private sealed class Handler(HttpStatusCode? failure = null) : HttpMessageHandler
    {
        public string UploadBody { get; private set; } = ""; public List<string> AuthorizationValues { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationValues.Add(request.Headers.Authorization!.ToString()); if (failure is { } status) return new(status);
            if (request.Method == HttpMethod.Post) { UploadBody = await request.Content!.ReadAsStringAsync(cancellationToken); return new(HttpStatusCode.OK) { Content = new StringContent("\"task-123\"") }; }
            if (request.RequestUri!.AbsolutePath.Contains("documents")) return Json("{\"results\":[]}");
            return Json("{\"results\":[{\"id\":1,\"name\":\"Example\"}]}");
        }
        private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, System.Text.Encoding.UTF8, "application/json") };
    }
}
