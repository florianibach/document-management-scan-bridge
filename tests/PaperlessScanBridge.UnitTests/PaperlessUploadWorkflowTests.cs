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

    [Theory]
    [InlineData(PaperlessFailure.Configuration)]
    [InlineData(PaperlessFailure.Network)]
    [InlineData(PaperlessFailure.Timeout)]
    [InlineData(PaperlessFailure.Authentication)]
    [InlineData(PaperlessFailure.Authorization)]
    [InlineData(PaperlessFailure.InvalidResponse)]
    [InlineData(PaperlessFailure.Server)]
    [InlineData(PaperlessFailure.Unknown)]
    public async Task EveryFailureRemainsRetryableAndKeepsDiagnosticContext(PaperlessFailure failure)
    {
        var client = new ClientStub(new(false, "Safe failure", failure, DiagnosticId: "D123"));
        using var workflow = new PaperlessUploadWorkflow(client); var request = new PaperlessUploadRequest(Guid.NewGuid(), null, null, null, []);
        await workflow.UploadAsync(request);
        Assert.Equal(PaperlessUploadState.Failed, workflow.Current!.State); Assert.Equal(failure, workflow.Current.Failure); Assert.Equal("D123", workflow.Current.DiagnosticId);
        client.Result = new(true, "accepted", TaskId: "task"); await workflow.UploadAsync(request);
        Assert.Equal(PaperlessUploadState.Accepted, workflow.Current.State); Assert.Equal(2, client.Calls);
    }

    [Fact]
    public async Task FailedErrorCanBeDeliberatelyDismissed()
    {
        var client = new ClientStub(new(false, "Safe failure", PaperlessFailure.Network, DiagnosticId: "D123")); using var workflow = new PaperlessUploadWorkflow(client);
        await workflow.UploadAsync(new(Guid.NewGuid(), null, null, null, [])); workflow.DismissError(); Assert.Null(workflow.Current);
    }

    private sealed class ClientStub(PaperlessResult? result = null) : IPaperlessClient
    {
        public int Calls { get; private set; }
        public PaperlessResult Result { get; set; } = result ?? new(true, "accepted", TaskId: "42");
        public Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult(Result); }
        public Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
