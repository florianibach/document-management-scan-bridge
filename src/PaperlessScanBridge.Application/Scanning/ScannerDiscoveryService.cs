using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using PaperlessScanBridge.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace PaperlessScanBridge.Application.Scanning;

public sealed class ScannerDiscoveryService(
    IZeroconfBrowser browser,
    IScannerEndpointValidator validator,
    ISelectedScannerRepository repository,
    ISaneAirscanConfigurationWriter configurationWriter,
    ScannerDiscoveryOptions options,
    ILogger<ScannerDiscoveryService> logger) : IScannerDiscoveryService
{
    private static readonly string[] ServiceTypes = ["_uscan._tcp.local.", "_uscans._tcp.local."];
    private readonly ConcurrentDictionary<string, SnapshotEntry> snapshot = new(StringComparer.Ordinal);

    public async Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting scanner discovery with the .NET Zeroconf backend for {ServiceTypes}", string.Join(", ", ServiceTypes));
        var diagnostics = new List<string>();
        var advertisements = new List<(string Type, ZeroconfAdvertisement Value)>();
        foreach (var type in ServiceTypes)
        {
            try
            {
                var results = await browser.ResolveAsync(type, TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken);
                logger.LogInformation("Zeroconf query for {ServiceType} returned {AdvertisementCount} advertisement(s)", type, results.Count);
                advertisements.AddRange(results.Select(value => (type, value)));
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Zeroconf query for {ServiceType} timed out after {TimeoutSeconds} seconds", type, options.TimeoutSeconds);
                diagnostics.Add($"Multicast discovery for {type} timed out after {options.TimeoutSeconds} seconds.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Zeroconf query for {ServiceType} failed", type);
                diagnostics.Add($"Multicast discovery for {type} failed: {exception.GetType().Name}.");
            }
        }

        var groups = advertisements.SelectMany(item => Normalize(item.Type, item.Value))
            .GroupBy(DeviceKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => CreateCandidate(group.ToArray(), diagnostics))
            .OrderBy(candidate => candidate.Display.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();
        var candidates = groups.Select(candidate => candidate.Display).ToArray();

        snapshot.Clear();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        foreach (var candidate in groups) snapshot[candidate.Display.DiscoveryId] = new(candidate.Endpoints, expiresAt);
        if (candidates.Length == 0 && diagnostics.Count == 0)
            diagnostics.Add("No eSCL/AirScan multicast advertisements were received. Check UDP 5353, host networking and the scanner's network connection.");
        logger.LogInformation("Scanner discovery completed with {ScannerCount} unique scanner(s) and {DiagnosticCount} diagnostic(s)", candidates.Length, diagnostics.Count);
        return new(candidates, diagnostics);
    }

    public async Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken)
    {
        if (!snapshot.TryGetValue(discoveryId, out var entry) || entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            logger.LogWarning("Scanner selection rejected because discovery id {DiscoveryId} is unknown or expired", discoveryId);
            return new(false, null, "The discovery result is unknown or expired. Search for scanners again.");
        }
        var endpoint = entry.Endpoints[0];
        logger.LogInformation("Validating discovered scanner {DisplayName} at {Protocol}://{Address}:{Port}", endpoint.DisplayName, endpoint.Protocol, endpoint.IpAddress, endpoint.Port);
        var validation = await validator.ValidateAsync(endpoint, cancellationToken);
        string? successDiagnostic = null;
        if (!validation.Succeeded && validation.Failure == ScannerEndpointFailure.TlsCertificate && endpoint.Protocol == "https")
        {
            var httpEndpoint = entry.Endpoints.FirstOrDefault(candidate => candidate.Protocol == "http");
            if (httpEndpoint is not null)
            {
                logger.LogWarning("HTTPS validation for scanner {DisplayName} failed because its certificate is not trusted; validating its matching DNS-SD advertised HTTP endpoint", endpoint.DisplayName);
                endpoint = httpEndpoint;
                validation = await validator.ValidateAsync(endpoint, cancellationToken);
                if (validation.Succeeded)
                    successDiagnostic = "Der Scanner bietet HTTPS nur mit einem nicht vertrauenswürdigen Gerätezertifikat an. Der ebenfalls angekündigte und erfolgreich validierte HTTP-eSCL-Endpunkt wird verwendet.";
            }
        }
        if (!validation.Succeeded)
        {
            logger.LogWarning("Validation failed for scanner {DisplayName}: {Diagnostic}", endpoint.DisplayName, validation.Diagnostic);
            return new(false, null, validation.Diagnostic);
        }
        var selected = await repository.SaveAsync(endpoint, DateTimeOffset.UtcNow, cancellationToken);
        await configurationWriter.WriteAsync(selected, cancellationToken);
        logger.LogInformation("Validated scanner {DisplayName} was persisted and the sane-airscan configuration was updated", selected.DisplayName);
        return new(true, selected, successDiagnostic);
    }

    public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken) => repository.GetAsync(cancellationToken);

    internal static IReadOnlyList<DiscoveredScanner> Normalize(string serviceType, ZeroconfAdvertisement advertisement)
    {
        var protocol = serviceType.StartsWith("_uscans", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
        var path = advertisement.Properties.TryGetValue("rs", out var resource) ? resource : "eSCL";
        path = path.Trim().Trim('/');
        var name = advertisement.Properties.TryGetValue("ty", out var model) && !string.IsNullOrWhiteSpace(model)
            ? model : advertisement.ServiceName;
        return advertisement.Addresses.Where(address => System.Net.IPAddress.TryParse(address, out _)).Distinct()
            .Select(address =>
            {
                var host = address.Contains(':') ? $"[{address}]" : address;
                var url = $"{protocol}://{host}:{advertisement.Port}/{path}";
                var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{advertisement.ServiceName}|{address}|{advertisement.Port}|{protocol}|{path}")))[..24];
                return new DiscoveredScanner(id, name, address, advertisement.Port, protocol, url);
            }).ToArray();
    }

    private static string DeviceKey(DiscoveredScanner value) => $"{value.DisplayName}|{value.IpAddress}|{new Uri(value.EsclUrl).AbsolutePath}";
    private static CandidateGroup CreateCandidate(IReadOnlyList<DiscoveredScanner> values, List<string> diagnostics)
    {
        if (values.Select(value => value.Protocol).Distinct().Count() > 1)
            diagnostics.Add($"{values[0].DisplayName} advertised both HTTP and HTTPS; HTTPS will be validated first.");
        var endpoints = values.OrderByDescending(value => value.Protocol == "https").ThenBy(value => value.Port).ToArray();
        var preferred = endpoints[0];
        var groupId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(DeviceKey(preferred))))[..24];
        var display = preferred with { DiscoveryId = groupId };
        return new(display, endpoints);
    }

    private sealed record CandidateGroup(DiscoveredScanner Display, IReadOnlyList<DiscoveredScanner> Endpoints);
    private sealed record SnapshotEntry(IReadOnlyList<DiscoveredScanner> Endpoints, DateTimeOffset ExpiresAt);
}
