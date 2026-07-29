using System.Net;
using System.Xml.Linq;
using System.Security.Authentication;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Infrastructure.Scanning;

public sealed class EsclScannerEndpointValidator(IHttpClientFactory clients, IOptions<ScannerDiscoveryOptions> options) : IScannerEndpointValidator
{
    private const int MaximumCapabilitiesBytes = 1024 * 1024;
    public async Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.ValidationTimeoutSeconds));
        try
        {
            var endpoint = new Uri(scanner.EsclUrl.TrimEnd('/') + "/ScannerCapabilities");
            using var response = await clients.CreateClient("escl-validation").GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (response.StatusCode != HttpStatusCode.OK)
                return new(false, ScannerEndpointFailure.HttpStatus, $"Scanner capability validation failed with HTTP {(int)response.StatusCode}.");
            if (response.Content.Headers.ContentLength > MaximumCapabilitiesBytes)
                return new(false, ScannerEndpointFailure.InvalidCapabilities, "The scanner capability response exceeded the allowed size.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            await using var buffer = new MemoryStream();
            var bytes = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(bytes, timeout.Token)) > 0)
            {
                if (buffer.Length + read > MaximumCapabilitiesBytes)
                    return new(false, ScannerEndpointFailure.InvalidCapabilities, "The scanner capability response exceeded the allowed size.");
                await buffer.WriteAsync(bytes.AsMemory(0, read), timeout.Token);
            }
            buffer.Position = 0;
            var document = await XDocument.LoadAsync(buffer, LoadOptions.None, timeout.Token);
            if (document.Root?.Name.LocalName != "ScannerCapabilities")
                return new(false, ScannerEndpointFailure.InvalidCapabilities, "The endpoint did not return an eSCL ScannerCapabilities document.");
            return new(true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, ScannerEndpointFailure.Timeout, $"Scanner capability validation timed out after {options.Value.ValidationTimeoutSeconds} seconds.");
        }
        catch (HttpRequestException exception) when (Contains<AuthenticationException>(exception))
        {
            return new(false, ScannerEndpointFailure.TlsCertificate, "The scanner's HTTPS certificate is not trusted or does not match its advertised address.");
        }
        catch (HttpRequestException exception)
        {
            return new(false, ScannerEndpointFailure.Connection, $"Scanner capability validation failed: {exception.Message}");
        }
        catch (System.Xml.XmlException)
        {
            return new(false, ScannerEndpointFailure.InvalidXml, "The scanner returned malformed capability XML.");
        }
    }

    private static bool Contains<TException>(Exception exception) where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is TException) return true;
        return false;
    }
}
