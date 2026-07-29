using System.Net;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Infrastructure.Scanning;

public sealed class EsclScannerEndpointValidator(IHttpClientFactory clients, IOptions<ScannerDiscoveryOptions> options) : IScannerEndpointValidator
{
    public async Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ValidationTimeoutSeconds));
        try
        {
            var endpoint = new Uri(scanner.EsclUrl.TrimEnd('/') + "/ScannerCapabilities");
            using var response = await clients.CreateClient("escl-validation").GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode != HttpStatusCode.OK)
                return new(false, $"Scanner capability validation failed with HTTP {(int)response.StatusCode}.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            var document = await XDocument.LoadAsync(stream, LoadOptions.None, timeout.Token);
            if (!document.Descendants().Any(element => element.Name.LocalName == "ScannerCapabilities"))
                return new(false, "The endpoint did not return an eSCL ScannerCapabilities document.");
            return new(true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, $"Scanner capability validation timed out after {options.Value.ValidationTimeoutSeconds} seconds.");
        }
        catch (HttpRequestException exception)
        {
            return new(false, $"Scanner capability validation failed: {exception.Message}");
        }
        catch (System.Xml.XmlException)
        {
            return new(false, "The scanner returned malformed capability XML.");
        }
    }
}
