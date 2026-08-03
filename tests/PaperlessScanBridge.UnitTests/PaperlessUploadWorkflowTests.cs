using PaperlessScanBridge.Application.Paperless;

namespace PaperlessScanBridge.UnitTests;

public sealed class PaperlessUploadWorkflowTests
{
    [Fact]
    public async Task AcceptedSessionCannotBeSilentlyUploadedTwice()
    {
        var client = new ClientStub(); using var workflow = new PaperlessUploadWorkflow(client);
        var request = new PaperlessUploadRequest(Guid.NewGuid(), "Title", null, null, []);
        await workflow.UploadAsync(request); await workflow.UploadAsync(request);
        Assert.Equal(1, client.Calls); Assert.Equal(PaperlessUploadState.Accepted, workflow.Current!.State);
    }

    private sealed class ClientStub : IPaperlessClient
    {
        public int Calls { get; private set; }
        public Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(new PaperlessResult(true, "accepted", TaskId: "42")); }
        public Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
