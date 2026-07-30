using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class SimplexScanWorkflowTests : IDisposable
{
    private readonly string storage = Path.Combine(Path.GetTempPath(), "scan-workflow-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CompletesWithPagesInAnIsolatedSession()
    {
        var workflow = CreateWorkflow(new WritingAdapter());
        var queued = await workflow.StartAsync(new("device", "Flatbed", ScanColorMode.Color, 300));
        await WaitUntilDone(workflow);
        Assert.Equal(ScanJobState.Completed, workflow.Current!.State);
        Assert.Equal(2, workflow.Current.PageCount);
        Assert.True(Directory.Exists(Path.Combine(storage, queued.SessionId.ToString("N"))));
    }

    [Fact]
    public async Task PreventsDuplicatesAndCancellationRemovesPartialOutput()
    {
        var workflow = CreateWorkflow(new BlockingAdapter());
        var queued = await workflow.StartAsync(new("device", "ADF Simplex", ScanColorMode.Grayscale, 200));
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.StartAsync(new("device", "ADF Simplex", ScanColorMode.Grayscale, 200)));
        await workflow.CancelAsync();
        Assert.Equal(ScanJobState.Cancelled, workflow.Current!.State);
        Assert.False(Directory.Exists(Path.Combine(storage, queued.SessionId.ToString("N"))));
    }

    [Fact]
    public async Task FailureRemovesPartialOutputAndUsesSafeDiagnostic()
    {
        var workflow = CreateWorkflow(new FailingAdapter());
        var queued = await workflow.StartAsync(new("device", "Flatbed", ScanColorMode.BlackAndWhite, 100));
        await WaitUntilDone(workflow);
        Assert.Equal(ScanJobState.Failed, workflow.Current!.State);
        Assert.DoesNotContain("secret", workflow.Current.Message);
        Assert.False(Directory.Exists(Path.Combine(storage, queued.SessionId.ToString("N"))));
    }

    [Fact]
    public async Task TimeoutAsksBeforeAbortAndContinueResetsTheWindow()
    {
        var workflow = CreateWorkflow(new BlockingAdapter());
        await workflow.StartAsync(new("device", "ADF Simplex", ScanColorMode.Color, 300));
        await WaitForState(workflow, ScanJobState.AwaitingUserDecision);
        Assert.Contains("weiter", workflow.Current!.Message, StringComparison.OrdinalIgnoreCase);
        await workflow.ContinueAsync();
        await WaitForState(workflow, ScanJobState.Running);
        await workflow.CancelAsync();
        Assert.Equal(ScanJobState.Cancelled, workflow.Current!.State);
    }

    private SimplexScanWorkflow CreateWorkflow(ISimplexScannerAdapter adapter) =>
        new(adapter, new TemporaryStorageOptions { Path = storage }, new ScannerOptions { ScanTimeoutSeconds = 1 });

    private static async Task WaitForState(ISimplexScanWorkflow workflow, ScanJobState state)
    { while (workflow.Current?.State != state) await Task.Delay(10); }

    private static async Task WaitUntilDone(ISimplexScanWorkflow workflow)
    { while (workflow.Current?.IsActive == true) await Task.Delay(10); }

    public void Dispose() { if (Directory.Exists(storage)) Directory.Delete(storage, true); }
    private sealed class WritingAdapter : ISimplexScannerAdapter
    {
        public Task<ScanCaptureResult> CaptureAsync(string directory, SimplexScanSettings settings, CancellationToken token)
        { var a = Path.Combine(directory, "page-0001.png"); var b = Path.Combine(directory, "page-0002.png"); File.WriteAllText(a, "one"); File.WriteAllText(b, "two"); return Task.FromResult<ScanCaptureResult>(new([a, b])); }
    }
    private sealed class BlockingAdapter : ISimplexScannerAdapter
    {
        public async Task<ScanCaptureResult> CaptureAsync(string directory, SimplexScanSettings settings, CancellationToken token)
        { File.WriteAllText(Path.Combine(directory, "partial"), "partial"); await Task.Delay(Timeout.Infinite, token); return new([]); }
    }
    private sealed class FailingAdapter : ISimplexScannerAdapter
    {
        public Task<ScanCaptureResult> CaptureAsync(string directory, SimplexScanSettings settings, CancellationToken token)
        { File.WriteAllText(Path.Combine(directory, "partial"), "secret"); throw new InvalidOperationException("secret scanner output"); }
    }
}
