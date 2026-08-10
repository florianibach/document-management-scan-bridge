using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Application.Documents;

public enum BatchDocumentState { Pending, CreatingPdf, Ready, Uploading, Uploaded, Failed }

public sealed record BatchDocumentMetadata(string? Title = null, int? CorrespondentId = null,
    int? DocumentTypeId = null, IReadOnlyList<int>? TagIds = null)
{
    public IReadOnlyList<int> Tags => TagIds ?? [];
}

public sealed record BatchDocument(Guid Id, int Number, IReadOnlyList<EditablePage> Pages,
    BatchDocumentMetadata Metadata, BatchDocumentState State = BatchDocumentState.Pending,
    string? Message = null, string? TaskId = null);

public sealed record ScanBatchSnapshot(Guid SessionId, IReadOnlyList<int> SplitPoints,
    IReadOnlyList<BatchDocument> Documents)
{
    public int PageCount => Documents.Sum(document => document.Pages.Count);
    public int CompletedCount => Documents.Count(document => document.State == BatchDocumentState.Uploaded);
}

public interface IScanBatchStore
{
    Task<ScanBatchSnapshot?> LoadAsync(Guid sessionId, string profileId, CancellationToken token = default);
    Task SaveAsync(ScanBatchSnapshot batch, string profileId, CancellationToken token = default);
}

public interface IScanBatchProcessor
{
    Task CreatePdfAsync(Guid sessionId, BatchDocument document, CancellationToken token);
    Task<PaperlessResult> UploadAsync(Guid sessionId, BatchDocument document, CancellationToken token);
}

public interface IScanBatchWorkflow
{
    ScanBatchSnapshot? Current { get; }
    event Action? Changed;
    Task LoadAsync(PageEditingSnapshot pages, string profileId, CancellationToken token = default);
    Task ToggleSplitAfterAsync(Guid pageId, CancellationToken token = default);
    Task SetMetadataAsync(Guid documentId, BatchDocumentMetadata metadata, CancellationToken token = default);
    Task CreatePdfAsync(Guid documentId, CancellationToken token = default);
    Task UploadAsync(Guid documentId, CancellationToken token = default);
}

public sealed class ScanBatchWorkflow(IScanBatchStore store, IScanBatchProcessor processor) : IScanBatchWorkflow
{
    private string? profileId;
    private readonly SemaphoreSlim mutex = new(1, 1);
    public ScanBatchSnapshot? Current { get; private set; }
    public event Action? Changed;

    public async Task LoadAsync(PageEditingSnapshot pages, string owner, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        profileId = owner;
        var saved = await store.LoadAsync(pages.SessionId, owner, token);
        var splits = ResolveSplits(pages.Pages, saved);
        Current = Build(pages.SessionId, pages.Pages, splits, saved?.Documents);
        await PersistAsync(token);
        Changed?.Invoke();
    }

    public async Task ToggleSplitAfterAsync(Guid pageId, CancellationToken token = default)
    {
        EnsureLoaded();
        var allPages = Current!.Documents.SelectMany(document => document.Pages).ToArray();
        var index = Array.FindIndex(allPages, page => page.Id == pageId) + 1;
        if (index <= 0) throw new ArgumentException("The page is not part of this batch.", nameof(pageId));
        if (index == allPages.Length) throw new InvalidOperationException("A split cannot follow the final page.");
        var splits = Current.SplitPoints.ToHashSet();
        if (!splits.Add(index)) splits.Remove(index);
        Current = Build(Current.SessionId, allPages, splits.Order().ToArray(), Current.Documents);
        await PersistAsync(token); Changed?.Invoke();
    }

    public Task SetMetadataAsync(Guid documentId, BatchDocumentMetadata metadata, CancellationToken token = default) =>
        MutateAsync(documentId, document => document with { Metadata = metadata with { TagIds = metadata.Tags.Distinct().ToArray() } }, token);

    public async Task CreatePdfAsync(Guid documentId, CancellationToken token = default)
    {
        var document = Find(documentId);
        if (document.State == BatchDocumentState.Uploaded) return;
        await SetStateAsync(documentId, BatchDocumentState.CreatingPdf, "Creating PDF …", token);
        try { await processor.CreatePdfAsync(Current!.SessionId, Find(documentId), token); await SetStateAsync(documentId, BatchDocumentState.Ready, "PDF ready.", token); }
        catch (OperationCanceledException) { await SetStateAsync(documentId, BatchDocumentState.Failed, "PDF creation cancelled; retry is available.", CancellationToken.None); }
        catch { await SetStateAsync(documentId, BatchDocumentState.Failed, "PDF creation failed; pages remain available for retry.", CancellationToken.None); }
    }

    public async Task UploadAsync(Guid documentId, CancellationToken token = default)
    {
        var document = Find(documentId);
        if (document.State == BatchDocumentState.Uploaded) return;
        if (document.State != BatchDocumentState.Ready) await CreatePdfAsync(documentId, token);
        document = Find(documentId); if (document.State != BatchDocumentState.Ready) return;
        await SetStateAsync(documentId, BatchDocumentState.Uploading, "Uploading …", token);
        try
        {
            var result = await processor.UploadAsync(Current!.SessionId, Find(documentId), token);
            await MutateAsync(documentId, value => value with { State = result.Succeeded ? BatchDocumentState.Uploaded : BatchDocumentState.Failed, Message = result.Message, TaskId = result.TaskId }, token);
        }
        catch (OperationCanceledException) { await SetStateAsync(documentId, BatchDocumentState.Failed, "Upload cancelled; the PDF remains available for retry.", CancellationToken.None); }
    }

    private async Task MutateAsync(Guid id, Func<BatchDocument, BatchDocument> change, CancellationToken token)
    {
        await mutex.WaitAsync(token); try
        {
            EnsureLoaded(); var documents = Current!.Documents.Select(document => document.Id == id ? change(document) : document).ToArray();
            if (!documents.Any(document => document.Id == id)) throw new ArgumentException("The document is not part of this batch.", nameof(id));
            Current = Current with { Documents = documents }; await PersistAsync(token);
        }
        finally { mutex.Release(); }
        Changed?.Invoke();
    }
    private Task SetStateAsync(Guid id, BatchDocumentState state, string message, CancellationToken token) => MutateAsync(id, document => document with { State = state, Message = message }, token);
    private BatchDocument Find(Guid id) { EnsureLoaded(); return Current!.Documents.SingleOrDefault(document => document.Id == id) ?? throw new ArgumentException("The document is not part of this batch.", nameof(id)); }
    private void EnsureLoaded() { if (Current is null || profileId is null) throw new InvalidOperationException("No scan batch is loaded."); }
    private Task PersistAsync(CancellationToken token) => store.SaveAsync(Current!, profileId!, token);

    private static int[] ResolveSplits(IReadOnlyList<EditablePage> pages, ScanBatchSnapshot? saved)
    {
        if (saved is null) return [];

        // A boundary means "this remaining page starts the next document", rather than
        // "split at this numeric index". The saved document membership gives us that
        // semantic anchor without changing the persisted format. If its first page was
        // removed, the boundary advances to the next surviving page in that document.
        // If the whole document was removed, no empty boundary is retained.
        if (saved.Documents.Count > 1)
        {
            var remainingIndexes = pages.Select((page, index) => (page.Id, Index: index))
                .ToDictionary(value => value.Id, value => value.Index);
            return saved.Documents.Skip(1)
                .Select(document => document.Pages
                    .Select(page => remainingIndexes.GetValueOrDefault(page.Id, -1))
                    .FirstOrDefault(index => index >= 0, -1))
                .Where(index => index > 0 && index < pages.Count)
                .Distinct()
                .Order()
                .ToArray();
        }

        // Compatibility for snapshots created before document membership was persisted.
        return saved.SplitPoints.Where(point => point > 0 && point < pages.Count).Distinct().Order().ToArray();
    }

    private static ScanBatchSnapshot Build(Guid sessionId, IReadOnlyList<EditablePage> pages, IReadOnlyList<int> splits, IReadOnlyList<BatchDocument>? previous)
    {
        var boundaries = new[] { 0 }.Concat(splits).Concat([pages.Count]).ToArray();
        var documents = new List<BatchDocument>();
        for (var i = 0; i < boundaries.Length - 1; i++)
        {
            var part = pages.Skip(boundaries[i]).Take(boundaries[i + 1] - boundaries[i]).ToArray();
            if (part.Length == 0) throw new InvalidOperationException("Empty documents are not allowed.");
            var old = previous?.FirstOrDefault(document => document.Pages.Select(page => page.Id).SequenceEqual(part.Select(page => page.Id)));
            documents.Add(old is null ? new(Guid.NewGuid(), i + 1, part, new()) : old with { Number = i + 1, Pages = part });
        }
        return new(sessionId, splits, documents);
    }
}
