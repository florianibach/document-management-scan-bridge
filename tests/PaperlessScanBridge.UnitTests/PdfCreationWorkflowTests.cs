using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class PdfCreationWorkflowTests
{
    [Fact]
    public async Task ForwardsCurrentOrderAndRotations()
    {
        var writer = new WriterStub();
        using var workflow = new PdfCreationWorkflow(writer);
        var sessionId = Guid.NewGuid();
        var snapshot = new PageEditingSnapshot(sessionId,
        [
            new(Guid.NewGuid(), 1, "second.png", 90, true, null),
            new(Guid.NewGuid(), 2, "first.png", 270, true, null)
        ]);

        await workflow.CreateAsync(snapshot);

        var pages = Assert.IsAssignableFrom<IReadOnlyList<PdfPageInput>>(writer.Pages);
        Assert.Equal(["second.png", "first.png"], pages.Select(page => page.FileName));
        Assert.Equal([90, 270], pages.Select(page => page.RotationDegrees));
        Assert.Equal(PdfCreationState.Completed, workflow.Current!.State);
    }

    [Fact]
    public async Task RejectsEmptyOrCorruptReviewedStateWithoutCallingWriter()
    {
        var writer = new WriterStub();
        using var workflow = new PdfCreationWorkflow(writer);
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.CreateAsync(new(Guid.NewGuid(), [])));
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.CreateAsync(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "bad.png", 0, false, "corrupt")])));
        Assert.Null(writer.Pages);
    }

    [Fact]
    public async Task FailureRetainsActionableRetryState()
    {
        using var workflow = new PdfCreationWorkflow(new WriterStub(new InvalidDataException()));
        await workflow.CreateAsync(new(Guid.NewGuid(), [new(Guid.NewGuid(), 1, "page.png", 0, true, null)]));
        Assert.Equal(PdfCreationState.Failed, workflow.Current!.State);
        Assert.Contains("Sitzung bleibt erhalten", workflow.Current.Message);
    }

    private sealed class WriterStub(Exception? failure = null) : IPdfDocumentWriter
    {
        public IReadOnlyList<PdfPageInput>? Pages { get; private set; }
        public Task<string> WriteAsync(Guid sessionId, IReadOnlyList<PdfPageInput> pages, CancellationToken cancellationToken)
        { Pages = pages; return failure is null ? Task.FromResult("document.pdf") : Task.FromException<string>(failure); }
    }
}
