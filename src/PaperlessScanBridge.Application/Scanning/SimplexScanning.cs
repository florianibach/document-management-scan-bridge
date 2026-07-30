using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Application.Scanning;

public enum ScanColorMode { Color, Grayscale, BlackAndWhite }
public enum ScanJobState { Queued, Running, Completed, Cancelled, Failed }

public sealed record SimplexScanSettings(string DeviceId, string Source, ScanColorMode ColorMode, int ResolutionDpi);
public sealed record ScanCaptureResult(IReadOnlyList<string> PageFiles);

public interface ISimplexScannerAdapter
{
    Task<ScanCaptureResult> CaptureAsync(string sessionDirectory, SimplexScanSettings settings, CancellationToken cancellationToken);
}

public sealed record ScanJobSnapshot(Guid SessionId, ScanJobState State, int PageCount, string Message, DateTimeOffset UpdatedAt)
{
    public bool IsActive => State is ScanJobState.Queued or ScanJobState.Running;
}

public interface ISimplexScanWorkflow
{
    ScanJobSnapshot? Current { get; }
    event Action? Changed;
    Task<ScanJobSnapshot> StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default);
    Task CancelAsync();
}

public sealed class SimplexScanWorkflow(
    ISimplexScannerAdapter adapter,
    TemporaryStorageOptions storage,
    ILogger<SimplexScanWorkflow>? suppliedLogger = null) : ISimplexScanWorkflow, IDisposable
{
    private static readonly int[] SupportedResolutions = [100, 200, 300, 600];
    private readonly ILogger<SimplexScanWorkflow> logger = suppliedLogger ?? NullLogger<SimplexScanWorkflow>.Instance;
    private readonly object gate = new();
    private CancellationTokenSource? activeCancellation;
    public ScanJobSnapshot? Current { get; private set; }
    public event Action? Changed;

    public Task<ScanJobSnapshot> StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default)
    {
        if (!SupportedResolutions.Contains(settings.ResolutionDpi))
            throw new ArgumentOutOfRangeException(nameof(settings), "The selected resolution is not supported.");
        if (string.IsNullOrWhiteSpace(settings.DeviceId) || string.IsNullOrWhiteSpace(settings.Source))
            throw new ArgumentException("A scanner and one of its supported sources must be selected.", nameof(settings));

        CancellationTokenSource cancellation;
        ScanJobSnapshot queued;
        lock (gate)
        {
            if (Current?.IsActive == true) throw new InvalidOperationException("A scan job is already active.");
            cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            activeCancellation = cancellation;
            queued = Current = new(Guid.NewGuid(), ScanJobState.Queued, 0, "Scanauftrag wurde eingereiht.", DateTimeOffset.UtcNow);
        }
        Changed?.Invoke();
        _ = RunAsync(queued.SessionId, settings, cancellation);
        return Task.FromResult(queued);
    }

    public async Task CancelAsync()
    {
        CancellationTokenSource? cancellation;
        lock (gate) cancellation = Current?.IsActive == true ? activeCancellation : null;
        if (cancellation is null) return;
        cancellation.Cancel();
        while (Current?.IsActive == true) await Task.Delay(20);
    }

    private async Task RunAsync(Guid sessionId, SimplexScanSettings settings, CancellationTokenSource cancellation)
    {
        var sessionDirectory = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"));
        try
        {
            Directory.CreateDirectory(sessionDirectory);
            Update(sessionId, ScanJobState.Running, 0, "Scanner erfasst Seiten …");
            logger.LogInformation("Simplex scan session {SessionId} started", sessionId);
            var capture = await adapter.CaptureAsync(sessionDirectory, settings, cancellation.Token);
            var pages = capture.PageFiles.Where(path => IsInsideSession(path, sessionDirectory) && File.Exists(path)).ToArray();
            if (pages.Length == 0) throw new InvalidOperationException("The scanner returned no complete pages.");
            Update(sessionId, ScanJobState.Completed, pages.Length, $"Scan abgeschlossen: {pages.Length} Seite(n).");
            logger.LogInformation("Simplex scan session {SessionId} completed with {PageCount} page(s)", sessionId, pages.Length);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            DeleteSession(sessionDirectory);
            Update(sessionId, ScanJobState.Cancelled, 0, "Scan wurde abgebrochen; unvollständige Seiten wurden entfernt.");
            logger.LogInformation("Simplex scan session {SessionId} was cancelled", sessionId);
        }
        catch (Exception exception)
        {
            DeleteSession(sessionDirectory);
            var message = exception switch
            {
                ProcessTimeoutException => "Der Scan hat das Zeitlimit überschritten. Scanner und Netzwerk prüfen.",
                ProcessExecutionException => "Der Scannerbefehl konnte nicht gestartet werden. SANE-Installation prüfen.",
                _ => "Der Scan ist fehlgeschlagen oder hat keine vollständige Seite geliefert. Scannerverfügbarkeit prüfen."
            };
            Update(sessionId, ScanJobState.Failed, 0, message);
            logger.LogWarning("Simplex scan session {SessionId} failed: {FailureType}", sessionId, exception.GetType().Name);
        }
        finally
        {
            lock (gate)
            {
                cancellation.Dispose();
                if (ReferenceEquals(activeCancellation, cancellation)) activeCancellation = null;
            }
        }
    }

    private void Update(Guid id, ScanJobState state, int pages, string message)
    {
        lock (gate) Current = new(id, state, pages, message, DateTimeOffset.UtcNow);
        Changed?.Invoke();
    }

    private static bool IsInsideSession(string path, string directory) =>
        Path.GetFullPath(path).StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static void DeleteSession(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }

    public void Dispose() => activeCancellation?.Cancel();
}
