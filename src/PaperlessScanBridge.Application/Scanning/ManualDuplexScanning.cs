using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Application.Scanning;

public enum ManualDuplexState
{
    Idle,
    ScanningFronts,
    AwaitingFlipConfirmation,
    ScanningBacks,
    PageCountMismatch,
    Completed,
    Cancelled,
    Failed
}

public sealed record ManualDuplexSnapshot(
    Guid SessionId,
    ManualDuplexState State,
    int FrontPageCount,
    int BackPageCount,
    int PageCount,
    string Message,
    DateTimeOffset UpdatedAt)
{
    public bool IsActive => State is ManualDuplexState.ScanningFronts or ManualDuplexState.AwaitingFlipConfirmation
        or ManualDuplexState.ScanningBacks or ManualDuplexState.PageCountMismatch;
}

public interface IManualDuplexWorkflow
{
    ManualDuplexSnapshot? Current { get; }
    event Action? Changed;
    Task StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default);
    Task ConfirmFlipAsync(bool finalBackIsBlank);
    Task CancelAsync();
    Task RestartAsync();
}

/// <summary>
/// Coordinates the two physical simplex passes. The verified feeder returns the second pass in
/// reverse reading order; consequently its result is reversed before alternating front/back pages.
/// </summary>
public sealed class ManualDuplexWorkflow(
    ISimplexScannerAdapter adapter,
    TemporaryStorageOptions storage,
    ILogger<ManualDuplexWorkflow>? suppliedLogger = null) : IManualDuplexWorkflow, IDisposable
{
    private readonly ILogger<ManualDuplexWorkflow> logger = suppliedLogger ?? NullLogger<ManualDuplexWorkflow>.Instance;
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private SimplexScanSettings? settings;
    private string[] fronts = [];
    private bool finalBackIsBlank;

    public ManualDuplexSnapshot? Current { get; private set; }
    public event Action? Changed;

    public Task StartAsync(SimplexScanSettings scanSettings, CancellationToken cancellationToken = default)
    {
        if (!scanSettings.Source.Contains("ADF", StringComparison.OrdinalIgnoreCase)
            && !scanSettings.Source.Contains("Feeder", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Manual duplex requires an automatic document feeder.", nameof(scanSettings));

        Guid sessionId;
        CancellationTokenSource source;
        lock (gate)
        {
            if (Current?.IsActive == true) throw new InvalidOperationException("A manual duplex session is already active.");
            cancellation?.Dispose();
            sessionId = Guid.NewGuid();
            settings = scanSettings;
            fronts = [];
            source = cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Current = new(sessionId, ManualDuplexState.ScanningFronts, 0, 0, 0,
                "Vorderseiten werden gescannt …", DateTimeOffset.UtcNow);
        }
        Changed?.Invoke();
        _ = CaptureFrontsAsync(sessionId, scanSettings, source);
        return Task.CompletedTask;
    }

    public Task ConfirmFlipAsync(bool lastBackIsBlank)
    {
        Guid sessionId;
        SimplexScanSettings scanSettings;
        CancellationTokenSource source;
        lock (gate)
        {
            if (Current?.State != ManualDuplexState.AwaitingFlipConfirmation || settings is null || cancellation is null)
                throw new InvalidOperationException("The workflow is not awaiting flip confirmation.");
            sessionId = Current.SessionId;
            scanSettings = settings;
            source = cancellation;
            finalBackIsBlank = lastBackIsBlank;
            Current = Current with { State = ManualDuplexState.ScanningBacks, Message = "Rückseiten werden gescannt …", UpdatedAt = DateTimeOffset.UtcNow };
        }
        Changed?.Invoke();
        _ = CaptureBacksAsync(sessionId, scanSettings, source);
        return Task.CompletedTask;
    }

    public async Task CancelAsync()
    {
        CancellationTokenSource? source;
        lock (gate) source = Current?.IsActive == true ? cancellation : null;
        if (source is null) return;
        source.Cancel();
        while (Current?.IsActive == true) await Task.Delay(20);
    }

    public Task RestartAsync()
    {
        string? directory = null;
        lock (gate)
        {
            if (Current is null || Current.State is ManualDuplexState.ScanningFronts or ManualDuplexState.ScanningBacks)
                throw new InvalidOperationException("An active scan must be cancelled before restarting.");
            directory = SessionDirectory(Current.SessionId);
            Current = null;
            settings = null;
            fronts = [];
            cancellation?.Dispose();
            cancellation = null;
        }
        DeleteSession(directory);
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    private async Task CaptureFrontsAsync(Guid id, SimplexScanSettings scanSettings, CancellationTokenSource source)
    {
        try
        {
            var directory = Path.Combine(SessionDirectory(id), "fronts");
            Directory.CreateDirectory(directory);
            var capture = await adapter.CaptureAsync(directory, scanSettings, source.Token);
            fronts = ValidPages(capture.PageFiles, directory);
            if (fronts.Length == 0) throw new InvalidOperationException("No front pages were captured.");
            Update(new(id, ManualDuplexState.AwaitingFlipConfirmation, fronts.Length, 0, 0,
                "Vorderseiten fertig. Stapel wie gezeigt wenden und den zweiten Durchlauf ausdrücklich bestätigen.", DateTimeOffset.UtcNow));
            logger.LogInformation("Manual duplex session {SessionId} captured {PageCount} front page(s)", id, fronts.Length);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { Cancelled(id); }
        catch (Exception exception) { Failed(id, exception); }
    }

    private async Task CaptureBacksAsync(Guid id, SimplexScanSettings scanSettings, CancellationTokenSource source)
    {
        try
        {
            var directory = Path.Combine(SessionDirectory(id), "backs");
            Directory.CreateDirectory(directory);
            var capture = await adapter.CaptureAsync(directory, scanSettings, source.Token);
            var backs = ValidPages(capture.PageFiles, directory);
            var expected = finalBackIsBlank ? fronts.Length - 1 : fronts.Length;
            if (backs.Length != expected && !(finalBackIsBlank && backs.Length == fronts.Length))
            {
                Update(new(id, ManualDuplexState.PageCountMismatch, fronts.Length, backs.Length, 0,
                    $"Die Durchläufe passen nicht zusammen ({fronts.Length} Vorder-, {backs.Length} Rückseiten). Stapel prüfen und neu starten.", DateTimeOffset.UtcNow));
                logger.LogWarning("Manual duplex session {SessionId} has incompatible pass counts {FrontCount}/{BackCount}", id, fronts.Length, backs.Length);
                return;
            }

            // The blank sheet-back is the first image produced by the verified reversed second pass.
            if (finalBackIsBlank && backs.Length == fronts.Length) backs = backs.Skip(1).ToArray();
            var ordered = MergeReadingOrder(fronts, backs);
            var output = Path.Combine(SessionDirectory(id), "ordered");
            Directory.CreateDirectory(output);
            for (var index = 0; index < ordered.Count; index++)
                File.Copy(ordered[index], Path.Combine(output, $"page-{index + 1:0000}.png"), overwrite: false);
            Update(new(id, ManualDuplexState.Completed, fronts.Length, backs.Length, ordered.Count,
                $"Manueller Duplex-Scan abgeschlossen: {ordered.Count} Seite(n) in Lesereihenfolge.", DateTimeOffset.UtcNow));
            logger.LogInformation("Manual duplex session {SessionId} completed with {PageCount} ordered page(s)", id, ordered.Count);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested) { Cancelled(id); }
        catch (Exception exception) { Failed(id, exception); }
    }

    public static IReadOnlyList<string> MergeReadingOrder(IReadOnlyList<string> frontPages, IReadOnlyList<string> reversedBackPages)
    {
        if (reversedBackPages.Count > frontPages.Count
            || frontPages.Count - reversedBackPages.Count > 1)
            throw new ArgumentException("Front and back page counts are incompatible.");
        var backs = reversedBackPages.Reverse().ToArray();
        var result = new List<string>(frontPages.Count + backs.Length);
        for (var index = 0; index < frontPages.Count; index++)
        {
            result.Add(frontPages[index]);
            if (index < backs.Length) result.Add(backs[index]);
        }
        return result;
    }

    private string SessionDirectory(Guid id) => Path.Combine(Path.GetFullPath(storage.Path), id.ToString("N"));
    private static string[] ValidPages(IEnumerable<string> pages, string directory) => pages
        .Where(path => Path.GetFullPath(path).StartsWith(Path.GetFullPath(directory) + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(path)).ToArray();
    private void Update(ManualDuplexSnapshot snapshot) { lock (gate) Current = snapshot; Changed?.Invoke(); }
    private void Cancelled(Guid id) { DeleteSession(SessionDirectory(id)); Update(new(id, ManualDuplexState.Cancelled, 0, 0, 0, "Duplex-Scan abgebrochen; unvollständige Seiten wurden entfernt.", DateTimeOffset.UtcNow)); }
    private void Failed(Guid id, Exception exception) { DeleteSession(SessionDirectory(id)); Update(new(id, ManualDuplexState.Failed, 0, 0, 0, "Duplex-Scan fehlgeschlagen. Scanner und Stapel prüfen und neu starten.", DateTimeOffset.UtcNow)); logger.LogWarning("Manual duplex session {SessionId} failed: {FailureType}", id, exception.GetType().Name); }
    private static void DeleteSession(string path) { if (Directory.Exists(path)) Directory.Delete(path, true); }
    public void Dispose() => cancellation?.Cancel();
}
