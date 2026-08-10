namespace PaperlessScanBridge.Application.Paperless;

public sealed record PaperlessChoice(int Id, string Name);
public sealed record PaperlessMetadata(IReadOnlyList<PaperlessChoice> Correspondents, IReadOnlyList<PaperlessChoice> DocumentTypes, IReadOnlyList<PaperlessChoice> Tags);
public enum PaperlessFailure { None, Configuration, Authentication, Authorization, Network, Server, InvalidResponse, FileMissing, Cancelled, Unknown }
public sealed record PaperlessResult(bool Succeeded, string Message, PaperlessFailure Failure = PaperlessFailure.None, string? TaskId = null);
public sealed record PaperlessUploadRequest(Guid SessionId, string? Title, int? CorrespondentId, int? DocumentTypeId, IReadOnlyList<int> TagIds);

public interface IPaperlessClient
{
    Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken = default);
    Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken = default);
    Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}

public enum PaperlessUploadState { Idle, Uploading, Accepted, Failed, Cancelled }
public sealed record PaperlessUploadSnapshot(Guid SessionId, PaperlessUploadState State, int ProgressPercent, string Message, string? TaskId = null)
{ public bool IsActive => State == PaperlessUploadState.Uploading; }

public interface IPaperlessUploadWorkflow
{
    PaperlessUploadSnapshot? Current { get; }
    event Action? Changed;
    Task UploadAsync(PaperlessUploadRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync();
}

public sealed class PaperlessUploadWorkflow(IPaperlessClient client) : IPaperlessUploadWorkflow, IDisposable
{
    private CancellationTokenSource? active;
    public PaperlessUploadSnapshot? Current { get; private set; }
    public event Action? Changed;

    public async Task UploadAsync(PaperlessUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (active is not null || Current is { State: PaperlessUploadState.Accepted, SessionId: var id } && id == request.SessionId) return;
        active = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Set(new(request.SessionId, PaperlessUploadState.Uploading, 0, "Upload wird vorbereitet …"));
        try
        {
            var progress = new InlineProgress(value =>
            {
                if (Current?.State == PaperlessUploadState.Uploading)
                    Set(new(request.SessionId, PaperlessUploadState.Uploading, value, $"Upload in progress: {value} %"));
            });
            var result = await client.UploadAsync(request, progress, active.Token);
            Set(new(request.SessionId, result.Succeeded ? PaperlessUploadState.Accepted : PaperlessUploadState.Failed,
                result.Succeeded ? 100 : Current?.ProgressPercent ?? 0, result.Message, result.TaskId));
        }
        catch (OperationCanceledException) { Set(new(request.SessionId, PaperlessUploadState.Cancelled, Current?.ProgressPercent ?? 0, "Upload cancelled; the PDF remains available for another attempt.")); }
        finally { active?.Dispose(); active = null; }
    }

    public Task CancelAsync() { active?.Cancel(); return Task.CompletedTask; }
    private void Set(PaperlessUploadSnapshot value) { Current = value; Changed?.Invoke(); }
    public void Dispose() => active?.Dispose();
    private sealed class InlineProgress(Action<int> report) : IProgress<int> { public void Report(int value) => report(value); }
}
