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

        var candidates = advertisements.SelectMany(item => Normalize(item.Type, item.Value))
            .GroupBy(DeviceKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => PreferSecure(group.ToArray(), diagnostics))
            .OrderBy(device => device.DisplayName, StringComparer.OrdinalIgnoreCase).ToArray();

        snapshot.Clear();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        foreach (var candidate in candidates) snapshot[candidate.DiscoveryId] = new(candidate, expiresAt);
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
        logger.LogInformation("Validating discovered scanner {DisplayName} at {Protocol}://{Address}:{Port}", entry.Scanner.DisplayName, entry.Scanner.Protocol, entry.Scanner.IpAddress, entry.Scanner.Port);
        var validation = await validator.ValidateAsync(entry.Scanner, cancellationToken);
        if (!validation.Succeeded)
        {
            logger.LogWarning("Validation failed for scanner {DisplayName}: {Diagnostic}", entry.Scanner.DisplayName, validation.Diagnostic);
            return new(false, null, validation.Diagnostic);
        }
        var selected = await repository.SaveAsync(entry.Scanner, DateTimeOffset.UtcNow, cancellationToken);
        await configurationWriter.WriteAsync(selected, cancellationToken);
        logger.LogInformation("Validated scanner {DisplayName} was persisted and the sane-airscan configuration was updated", selected.DisplayName);
        return new(true, selected);
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
    private static DiscoveredScanner PreferSecure(IReadOnlyList<DiscoveredScanner> values, List<string> diagnostics)
    {
        if (values.Select(value => value.Protocol).Distinct().Count() > 1)
            diagnostics.Add($"{values[0].DisplayName} advertised both HTTP and HTTPS; the HTTPS endpoint was preferred.");
        return values.OrderByDescending(value => value.Protocol == "https").ThenBy(value => value.Port).First();
    }

    private sealed record SnapshotEntry(DiscoveredScanner Scanner, DateTimeOffset ExpiresAt);
}
