using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class ScanBatchWorkflowTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SimplexAndDuplexPagesCanBeSplitWithoutGaps(bool manualDuplex)
    {
        var pages = Pages(4); var store = new Store(); var workflow = new ScanBatchWorkflow(store, new Processor());
        await workflow.LoadAsync(new(Guid.NewGuid(), pages), manualDuplex ? "duplex-profile" : "simplex-profile");
        await workflow.ToggleSplitAfterAsync(pages[1].Id);
        Assert.Equal([1, 2], workflow.Current!.Documents.Select(document => document.Number));
        Assert.Equal(pages.Select(page => page.Id), workflow.Current.Documents.SelectMany(document => document.Pages).Select(page => page.Id));
        Assert.All(workflow.Current.Documents, document => Assert.NotEmpty(document.Pages));
    }

    [Fact]
    public async Task BoundariesCanBeAddedMovedAndRemovedAndEditsStayWithPages()
    {
        var pages = Pages(4); pages[2] = pages[2] with { RotationDegrees = 90 };
        var workflow = new ScanBatchWorkflow(new Store(), new Processor()); await workflow.LoadAsync(new(Guid.NewGuid(), pages), "a");
        await workflow.ToggleSplitAfterAsync(pages[0].Id); await workflow.ToggleSplitAfterAsync(pages[2].Id);
        await workflow.ToggleSplitAfterAsync(pages[0].Id); await workflow.ToggleSplitAfterAsync(pages[1].Id);
        Assert.Equal([2, 3], workflow.Current!.SplitPoints);
        Assert.Equal(90, workflow.Current.Documents[1].Pages.Single().RotationDegrees);
    }

    [Fact]
    public async Task ReloadAfterPageRemovalDropsInvalidBoundaryAndPreservesCoverage()
    {
        var store = new Store(); var pages = Pages(3); var session = Guid.NewGuid();
        var first = new ScanBatchWorkflow(store, new Processor()); await first.LoadAsync(new(session, pages), "owner"); await first.ToggleSplitAfterAsync(pages[1].Id);
        var second = new ScanBatchWorkflow(store, new Processor()); await second.LoadAsync(new(session, [pages[0], pages[2]]), "owner");
        Assert.Empty(second.Current!.SplitPoints);
        Assert.Equal(2, second.Current.PageCount);
    }

    [Fact]
    public async Task MetadataAndPartialSuccessAreIndependentAndSuccessfulUploadIsIdempotent()
    {
        var processor = new Processor(failDocumentNumber: 2); var workflow = new ScanBatchWorkflow(new Store(), processor); var pages = Pages(2);
        await workflow.LoadAsync(new(Guid.NewGuid(), pages), "owner"); await workflow.ToggleSplitAfterAsync(pages[0].Id);
        var first = workflow.Current!.Documents[0]; var second = workflow.Current.Documents[1];
        await workflow.SetMetadataAsync(first.Id, new("Invoice", 1, 2, [3, 3]));
        await workflow.SetMetadataAsync(second.Id, new("Letter", 4, 5, [6]));
        await workflow.UploadAsync(first.Id); await workflow.UploadAsync(second.Id); await workflow.UploadAsync(first.Id);
        Assert.Equal(BatchDocumentState.Uploaded, workflow.Current.Documents[0].State);
        Assert.Equal(BatchDocumentState.Failed, workflow.Current.Documents[1].State);
        Assert.Equal("Invoice", workflow.Current.Documents[0].Metadata.Title);
        Assert.Equal("Letter", workflow.Current.Documents[1].Metadata.Title);
        Assert.Equal(2, processor.UploadCalls);
    }

    [Fact]
    public async Task StoreSeparatesProfilesDuringRecovery()
    {
        var store = new Store(); var session = Guid.NewGuid(); var pages = Pages(2);
        var alice = new ScanBatchWorkflow(store, new Processor()); await alice.LoadAsync(new(session, pages), "alice"); await alice.ToggleSplitAfterAsync(pages[0].Id);
        var bob = new ScanBatchWorkflow(store, new Processor()); await bob.LoadAsync(new(session, pages), "bob");
        Assert.Single(bob.Current!.Documents);
        var recovered = new ScanBatchWorkflow(store, new Processor()); await recovered.LoadAsync(new(session, pages), "alice");
        Assert.Equal(2, recovered.Current!.Documents.Count);
    }

    private static EditablePage[] Pages(int count) => Enumerable.Range(1, count).Select(i => new EditablePage(Guid.NewGuid(), i, $"page-{i}.png", 0, true, null)).ToArray();
    private sealed class Store : IScanBatchStore
    {
        private readonly Dictionary<(Guid, string), ScanBatchSnapshot> values = [];
        public Task<ScanBatchSnapshot?> LoadAsync(Guid id, string profile, CancellationToken token = default) => Task.FromResult(values.GetValueOrDefault((id, profile)));
        public Task SaveAsync(ScanBatchSnapshot batch, string profile, CancellationToken token = default) { values[(batch.SessionId, profile)] = batch; return Task.CompletedTask; }
    }
    private sealed class Processor(int? failDocumentNumber = null) : IScanBatchProcessor
    {
        public int UploadCalls { get; private set; }
        public Task CreatePdfAsync(Guid sessionId, BatchDocument document, CancellationToken token) => Task.CompletedTask;
        public Task<PaperlessResult> UploadAsync(Guid sessionId, BatchDocument document, CancellationToken token) { UploadCalls++; return Task.FromResult(document.Number == failDocumentNumber ? new PaperlessResult(false, "retry") : new PaperlessResult(true, "accepted", TaskId: $"task-{document.Number}")); }
    }
}
