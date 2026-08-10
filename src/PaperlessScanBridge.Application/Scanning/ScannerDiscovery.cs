namespace PaperlessScanBridge.Application.Scanning;

public interface IScannerDiscoveryService
{
    Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken);
    Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken);
    Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken);
    Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId, CancellationToken cancellationToken);
    Task<SelectedScanner> SaveSaneProfileAsync(long scannerId, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken cancellationToken);
    Task<ForgetScannerResult> ForgetAsync(long scannerId, CancellationToken cancellationToken) =>
        Task.FromResult(new ForgetScannerResult(false, null, "Forgetting scanners is not supported by this implementation."));
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
public sealed record ScannerSelectionResult(bool Succeeded, SelectedScanner? Scanner, string? Diagnostic = null,
    ScannerEndpointFailure Failure = ScannerEndpointFailure.None);
public sealed record SelectedScanner(long Id, string DisplayName, string IpAddress, int Port, string Protocol,
    string EsclUrl, DateTimeOffset ValidatedAt, string? SaneDeviceId = null,
    IReadOnlyList<string>? Sources = null, IReadOnlyList<int>? Resolutions = null);
public sealed record ForgetScannerResult(bool Succeeded, SelectedScanner? Scanner, string Message, bool ActiveScanConflict = false);

public interface IScannerEndpointValidator
{
    Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken);
}

public enum ScannerEndpointFailure
{
    None,
    TlsCertificate,
    Timeout,
    Connection,
    HttpStatus,
    InvalidXml,
    InvalidCapabilities
}

public sealed record ScannerEndpointValidationResult(
    bool Succeeded,
    ScannerEndpointFailure Failure = ScannerEndpointFailure.None,
    string? Diagnostic = null);

public interface ISelectedScannerRepository
{
    Task<SelectedScanner?> GetAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SelectedScanner>> ListAsync(CancellationToken cancellationToken);
    Task<SelectedScanner?> GetByIdAsync(long scannerId, CancellationToken cancellationToken);
    Task<SelectedScanner> SaveAsync(DiscoveredScanner scanner, DateTimeOffset validatedAt, CancellationToken cancellationToken);
    Task<SelectedScanner> SaveSaneProfileAsync(long scannerId, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken cancellationToken);
    Task<ScannerRemoval?> RemoveAsync(long scannerId, CancellationToken cancellationToken) => Task.FromResult<ScannerRemoval?>(null);
}

public sealed record ScannerRemoval(SelectedScanner Removed, SelectedScanner? Replacement, int RepairedProfileCount);

public interface ISaneAirscanConfigurationWriter
{
    Task WriteAsync(SelectedScanner scanner, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface IScannerOperationGuard
{
    IDisposable BeginScan(string deviceId);
    IDisposable? TryBeginForget(string? deviceId);
}

public sealed class ScannerOperationGuard : IScannerOperationGuard
{
    private readonly object gate = new();
    private readonly HashSet<string> scanning = new(StringComparer.Ordinal);
    private readonly HashSet<string> forgetting = new(StringComparer.Ordinal);

    public IDisposable BeginScan(string deviceId)
    {
        lock (gate)
        {
            if (forgetting.Contains(deviceId)) throw new InvalidOperationException("The scanner is being forgotten.");
            scanning.Add(deviceId);
            return new Lease(() => { lock (gate) scanning.Remove(deviceId); });
        }
    }

    public IDisposable? TryBeginForget(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return new Lease(() => { });
        lock (gate)
        {
            if (scanning.Contains(deviceId) || forgetting.Contains(deviceId)) return null;
            forgetting.Add(deviceId);
            return new Lease(() => { lock (gate) forgetting.Remove(deviceId); });
        }
    }

    private sealed class Lease(Action release) : IDisposable
    {
        private Action? release = release;
        public void Dispose() => Interlocked.Exchange(ref release, null)?.Invoke();
    }
}
