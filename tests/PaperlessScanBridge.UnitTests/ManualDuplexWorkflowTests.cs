using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class ManualDuplexWorkflowTests : IDisposable
{
    private readonly string storage = Path.Combine(Path.GetTempPath(), "duplex-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void OrdersEvenAndOddReversedPasses()
    {
        Assert.Equal(["1", "2", "3", "4"], ManualDuplexWorkflow.MergeReadingOrder(["1", "3"], ["4", "2"]));
        Assert.Equal(["1", "2", "3", "4", "5"], ManualDuplexWorkflow.MergeReadingOrder(["1", "3", "5"], ["4", "2"]));
    }

    [Fact]
    public async Task RequiresConfirmationAndCompletesTwoPasses()
    {
        var adapter = new PassAdapter(2, 2);
        var workflow = Create(adapter);
        await workflow.StartAsync(Settings());
        await WaitFor(workflow, ManualDuplexState.AwaitingFlipConfirmation);
        Assert.Equal(1, adapter.Calls);
        await workflow.ConfirmFlipAsync(false);
        await WaitFor(workflow, ManualDuplexState.Completed);
        Assert.Equal(2, adapter.Calls);
        Assert.Equal(4, workflow.Current!.PageCount);
        Assert.Equal(4, Directory.GetFiles(Path.Combine(storage, workflow.Current.SessionId.ToString("N"), "ordered")).Length);
    }

    [Fact]
    public async Task RemovesReportedFinalBlankWithoutLosingRealPage()
    {
        var workflow = Create(new PassAdapter(3, 3));
        await workflow.StartAsync(Settings());
        await WaitFor(workflow, ManualDuplexState.AwaitingFlipConfirmation);
        await workflow.ConfirmFlipAsync(true);
        await WaitFor(workflow, ManualDuplexState.Completed);
        Assert.Equal(5, workflow.Current!.PageCount);
    }

    [Fact]
    public async Task MismatchRequiresRestartInsteadOfGuessing()
    {
        var workflow = Create(new PassAdapter(3, 1));
        await workflow.StartAsync(Settings());
        await WaitFor(workflow, ManualDuplexState.AwaitingFlipConfirmation);
        await workflow.ConfirmFlipAsync(false);
        await WaitFor(workflow, ManualDuplexState.PageCountMismatch);
        Assert.Contains("neu starten", workflow.Current!.Message);
        await workflow.RestartAsync();
        Assert.Null(workflow.Current);
    }

    [Fact]
    public async Task CancellationCleansSessionAndPreventsSecondPass()
    {
        var workflow = Create(new BlockingAdapter());
        await workflow.StartAsync(Settings());
        var id = workflow.Current!.SessionId;
        await workflow.CancelAsync();
        Assert.Equal(ManualDuplexState.Cancelled, workflow.Current!.State);
        Assert.False(Directory.Exists(Path.Combine(storage, id.ToString("N"))));
    }

    [Fact]
    public async Task CancellationWhileWaitingForFlipCompletesImmediatelyAndCleansSession()
    {
        var adapter = new PassAdapter(2);
        var workflow = Create(adapter);
        await workflow.StartAsync(Settings());
        await WaitFor(workflow, ManualDuplexState.AwaitingFlipConfirmation);
        var id = workflow.Current!.SessionId;

        await workflow.CancelAsync().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(ManualDuplexState.Cancelled, workflow.Current!.State);
        Assert.Equal(1, adapter.Calls);
        Assert.False(Directory.Exists(Path.Combine(storage, id.ToString("N"))));
    }

    private ManualDuplexWorkflow Create(ISimplexScannerAdapter adapter) => new(adapter, new TemporaryStorageOptions { Path = storage });
    private static SimplexScanSettings Settings() => new("device", "ADF Simplex", ScanColorMode.Color, 300);
    private static async Task WaitFor(IManualDuplexWorkflow workflow, ManualDuplexState state)
    { while (workflow.Current?.State != state) await Task.Delay(10); }
    public void Dispose() { if (Directory.Exists(storage)) Directory.Delete(storage, true); }

    private sealed class PassAdapter(params int[] counts) : ISimplexScannerAdapter
    {
        public int Calls { get; private set; }
        public Task<ScanCaptureResult> CaptureAsync(string directory, SimplexScanSettings settings, CancellationToken token)
        {
            var count = counts[Calls++];
            var pages = Enumerable.Range(1, count).Select(index => Path.Combine(directory, $"capture-{index}.png")).ToArray();
            foreach (var page in pages) File.WriteAllText(page, page);
            return Task.FromResult(new ScanCaptureResult(pages));
        }
    }

    private sealed class BlockingAdapter : ISimplexScannerAdapter
    {
        public async Task<ScanCaptureResult> CaptureAsync(string directory, SimplexScanSettings settings, CancellationToken token)
        { await Task.Delay(Timeout.Infinite, token); return new([]); }
    }
}
