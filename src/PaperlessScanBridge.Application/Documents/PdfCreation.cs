using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Application.Documents;

public enum PdfCreationState { Idle, Creating, Completed, Cancelled, Failed }

public sealed record PdfPageInput(string FileName, int RotationDegrees);
public sealed record PdfCreationSnapshot(Guid SessionId, PdfCreationState State, string Message, string? FileName = null)
{
    public bool IsActive => State == PdfCreationState.Creating;
}

public interface IPdfDocumentWriter
{
    Task<string> WriteAsync(Guid sessionId, IReadOnlyList<PdfPageInput> pages, CancellationToken cancellationToken);
}

public interface IPdfCreationWorkflow
{
    PdfCreationSnapshot? Current { get; }
    event Action? Changed;
    Task CreateAsync(PageEditingSnapshot session, CancellationToken cancellationToken = default);
    Task CancelAsync();
}

public sealed class PdfCreationWorkflow(
    IPdfDocumentWriter writer,
    ILogger<PdfCreationWorkflow>? suppliedLogger = null) : IPdfCreationWorkflow, IDisposable
{
    private readonly ILogger<PdfCreationWorkflow> logger = suppliedLogger ?? NullLogger<PdfCreationWorkflow>.Instance;
    private readonly object gate = new();
    private CancellationTokenSource? cancellation;

    public PdfCreationSnapshot? Current { get; private set; }
    public event Action? Changed;

    public async Task CreateAsync(PageEditingSnapshot session, CancellationToken cancellationToken = default)
    {
        if (session.Pages.Count == 0)
            throw new InvalidOperationException("Mindestens eine Seite ist für die PDF-Erstellung erforderlich.");
        if (session.Pages.Any(page => !page.IsAvailable))
            throw new InvalidOperationException("Beschädigte oder nicht lesbare Seiten müssen vor der PDF-Erstellung entfernt oder erneut gescannt werden.");

        CancellationTokenSource source;
        lock (gate)
        {
            if (Current?.IsActive == true) throw new InvalidOperationException("Eine PDF-Erstellung läuft bereits.");
            cancellation?.Dispose();
            source = cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Current = new(session.SessionId, PdfCreationState.Creating, "PDF wird aus den geprüften Seiten erstellt …");
        }
        Changed?.Invoke();

        try
        {
            var pages = session.Pages.Select(page => new PdfPageInput(page.FileName, page.RotationDegrees)).ToArray();
            var path = await writer.WriteAsync(session.SessionId, pages, source.Token);
            Update(new(session.SessionId, PdfCreationState.Completed,
                $"PDF mit {pages.Length} Seite(n) wurde vollständig erstellt.", Path.GetFileName(path)));
            logger.LogInformation("PDF creation for session {SessionId} completed with {PageCount} page(s)", session.SessionId, pages.Length);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            Update(new(session.SessionId, PdfCreationState.Cancelled,
                "PDF-Erstellung abgebrochen. Die geprüften Seiten bleiben für einen neuen Versuch erhalten."));
            logger.LogInformation("PDF creation for session {SessionId} was cancelled", session.SessionId);
        }
        catch (Exception exception)
        {
            Update(new(session.SessionId, PdfCreationState.Failed,
                "PDF konnte nicht erstellt werden. Beschädigte Seiten entfernen oder den Scan wiederholen; die Sitzung bleibt erhalten."));
            logger.LogWarning("PDF creation for session {SessionId} failed: {FailureType}", session.SessionId, exception.GetType().Name);
        }
        finally
        {
            lock (gate)
            {
                if (ReferenceEquals(cancellation, source)) cancellation = null;
                source.Dispose();
            }
        }
    }

    public Task CancelAsync()
    {
        lock (gate) cancellation?.Cancel();
        return Task.CompletedTask;
    }

    private void Update(PdfCreationSnapshot snapshot)
    {
        lock (gate) Current = snapshot;
        Changed?.Invoke();
    }

    public void Dispose() => cancellation?.Cancel();
}
