namespace PaperlessScanBridge.Application.Scanning;

public interface IScannerDiscoveryService
{
    Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken);
    Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken);
    Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken);
}

public interface IZeroconfBrowser
{
    Task<IReadOnlyList<ZeroconfAdvertisement>> ResolveAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed record ZeroconfAdvertisement(string ServiceName, string HostName, IReadOnlyList<string> Addresses, int Port,
    IReadOnlyDictionary<string, string> Properties);

public sealed record DiscoveredScanner(string DiscoveryId, string DisplayName, string IpAddress, int Port,
    string Protocol, string EsclUrl);

public sealed record ScannerNetworkDiscoveryResult(IReadOnlyList<DiscoveredScanner> Devices, IReadOnlyList<string> Diagnostics);
public sealed record ScannerSelectionResult(bool Succeeded, SelectedScanner? Scanner, string? Diagnostic = null);
public sealed record SelectedScanner(long Id, string DisplayName, string IpAddress, int Port, string Protocol,
    string EsclUrl, DateTimeOffset ValidatedAt);

public interface IScannerEndpointValidator
{
    Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken);
}

public sealed record ScannerEndpointValidationResult(bool Succeeded, string? Diagnostic = null);

public interface ISelectedScannerRepository
{
    Task<SelectedScanner?> GetAsync(CancellationToken cancellationToken);
    Task<SelectedScanner> SaveAsync(DiscoveredScanner scanner, DateTimeOffset validatedAt, CancellationToken cancellationToken);
}

public interface ISaneAirscanConfigurationWriter
{
    Task WriteAsync(SelectedScanner scanner, CancellationToken cancellationToken);
}
