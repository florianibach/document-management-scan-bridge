using System.Net;
using System.Security.Authentication;
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
        Assert.Equal(ScannerEndpointFailure.None, result.Failure);
        Assert.Equal("http://10.0.0.2/eSCL/ScannerCapabilities", handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task RejectsNonCapabilityXml()
    {
        var validator = new EsclScannerEndpointValidator(new Factory(new Handler(HttpStatusCode.OK, "<html/>")), Options.Create(new ScannerDiscoveryOptions()));
        var result = await validator.ValidateAsync(new("id", "HP", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL"), default);
        Assert.False(result.Succeeded);
        Assert.Equal(ScannerEndpointFailure.InvalidCapabilities, result.Failure);
        Assert.Contains("ScannerCapabilities", result.Diagnostic);
    }

    [Fact]
    public async Task ClassifiesTlsCertificateFailure()
    {
        var exception = new HttpRequestException("TLS failed", new AuthenticationException("certificate invalid"));
        var validator = new EsclScannerEndpointValidator(new Factory(new Handler(exception)), Options.Create(new ScannerDiscoveryOptions()));
        var result = await validator.ValidateAsync(new("id", "HP", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL"), default);
        Assert.False(result.Succeeded);
        Assert.Equal(ScannerEndpointFailure.TlsCertificate, result.Failure);
        Assert.Contains("certificate", result.Diagnostic, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsOversizedCapabilities()
    {
        var validator = new EsclScannerEndpointValidator(new Factory(new Handler(HttpStatusCode.OK, new string('x', 1024 * 1024 + 1))), Options.Create(new ScannerDiscoveryOptions()));
        var result = await validator.ValidateAsync(new("id", "HP", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL"), default);
        Assert.Equal(ScannerEndpointFailure.InvalidCapabilities, result.Failure);
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, disposeHandler: false); }
    private sealed class Handler(HttpStatusCode status, string content) : HttpMessageHandler
    {
        public Handler(Exception exception) : this(HttpStatusCode.OK, "") => Exception = exception;
        private Exception? Exception { get; }
        public Uri? RequestUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Exception is null
                ? Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(content) })
                : Task.FromException<HttpResponseMessage>(Exception);
        }
    }
}
