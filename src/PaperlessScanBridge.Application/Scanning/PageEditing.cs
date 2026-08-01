using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Application.Scanning;

public sealed record EditablePage(Guid Id, int Number, string FileName, int RotationDegrees, bool IsAvailable, string? Error);
public sealed record PageEditingSnapshot(Guid SessionId, IReadOnlyList<EditablePage> Pages, string? Message = null);
public interface IPageEditingSession
{
    PageEditingSnapshot? Current { get; }
    event Action? Changed;
    Task LoadAsync(Guid sessionId, bool manualDuplex, CancellationToken cancellationToken = default);
    void Rotate(Guid pageId);
    void Delete(Guid pageId);
}

public sealed class PageEditingSession(TemporaryStorageOptions storage, ILogger<PageEditingSession>? suppliedLogger = null) : IPageEditingSession
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly ILogger<PageEditingSession> logger = suppliedLogger ?? NullLogger<PageEditingSession>.Instance;
    private readonly object gate = new();
    private List<PageEntry> pages = [];
    public PageEditingSnapshot? Current { get; private set; }
    public event Action? Changed;

    public async Task LoadAsync(Guid sessionId, bool manualDuplex, CancellationToken cancellationToken = default)
    {
        var root = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"));
        var directory = manualDuplex ? Path.Combine(root, "ordered") : root;
        var files = Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.png").Order(StringComparer.Ordinal).ToArray() : [];
        var loaded = new List<PageEntry>(files.Length);
        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            loaded.Add(new(Guid.NewGuid(), Path.GetFileName(path), 0, await ValidatePngAsync(path, cancellationToken)));
        }
        lock (gate) { pages = loaded; Current = Snapshot(sessionId, files.Length == 0 ? "Keine vollständigen Seiten für die Vorschau gefunden." : null); }
        logger.LogInformation("Preview session {SessionId} loaded {PageCount} page(s), including {ErrorCount} unavailable page(s)", sessionId, loaded.Count, loaded.Count(p => p.Error is not null));
        Changed?.Invoke();
    }

    public void Rotate(Guid pageId)
    {
        lock (gate) { var page = Find(pageId); page.Rotation = (page.Rotation + 90) % 360; Current = Snapshot(Current!.SessionId); }
        Changed?.Invoke();
    }

    public void Delete(Guid pageId)
    {
        lock (gate)
        {
            if (Current is null || pages.RemoveAll(page => page.Id == pageId) == 0) throw new ArgumentException("The page is not part of the active session.", nameof(pageId));
            Current = Snapshot(Current.SessionId, pages.Count == 0 ? "Alle Seiten wurden aus der aktiven Sitzung entfernt." : null);
        }
        Changed?.Invoke();
    }

    private PageEntry Find(Guid id) => Current is null ? throw new InvalidOperationException("No editing session is active.") : pages.SingleOrDefault(page => page.Id == id) ?? throw new ArgumentException("The page is not part of the active session.", nameof(id));
    private PageEditingSnapshot Snapshot(Guid id, string? message = null) => new(id, pages.Select((page, index) => new EditablePage(page.Id, index + 1, page.FileName, page.Rotation, page.Error is null, page.Error)).ToArray(), message);
    private static async Task<string?> ValidatePngAsync(string path, CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var signature = new byte[8];
            return await stream.ReadAsync(signature, token) == 8 && signature.SequenceEqual(PngSignature) ? null : "Seitendaten fehlen oder sind beschädigt. Die übrigen Seiten können weiter bearbeitet werden.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return "Seitendaten fehlen oder können nicht gelesen werden. Die übrigen Seiten können weiter bearbeitet werden."; }
    }
    private sealed class PageEntry(Guid id, string fileName, int rotation, string? error)
    { public Guid Id { get; } = id; public string FileName { get; } = fileName; public int Rotation { get; set; } = rotation; public string? Error { get; } = error; }
}
