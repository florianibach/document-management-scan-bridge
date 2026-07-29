using System.Net;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Infrastructure.Scanning;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class EsclScannerEndpointValidatorTests
{
    [Fact]
    public async Task AcceptsScannerCapabilitiesDocument()
    {
        var handler = new Handler(HttpStatusCode.OK, "<scan:ScannerCapabilities xmlns:scan='urn:schemas-canon-com:service:scan:1'/>");
        var validator = new EsclScannerEndpointValidator(new Factory(handler), Options.Create(new ScannerDiscoveryOptions()));
        var result = await validator.ValidateAsync(new("id", "HP", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL"), default);
        Assert.True(result.Succeeded);
        Assert.Equal("http://10.0.0.2/eSCL/ScannerCapabilities", handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task RejectsNonCapabilityXml()
    {
        var validator = new EsclScannerEndpointValidator(new Factory(new Handler(HttpStatusCode.OK, "<html/>")), Options.Create(new ScannerDiscoveryOptions()));
        var result = await validator.ValidateAsync(new("id", "HP", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL"), default);
        Assert.False(result.Succeeded);
        Assert.Contains("ScannerCapabilities", result.Diagnostic);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, disposeHandler: false); }
    private sealed class Handler(HttpStatusCode status, string content) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { RequestUri = request.RequestUri; return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) }); }
    }
}
