using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class SaneSimplexScannerAdapterTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "sane-adapter-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task BuildsSafeAdfArgumentsAndReturnsOrderedPages()
    {
        Directory.CreateDirectory(directory);
        var runner = new WritingRunner();
        var adapter = new SaneSimplexScannerAdapter(runner, new ScannerOptions { DeviceId = "airscan:e0:HP", TimeoutSeconds = 17 });
        var result = await adapter.CaptureAsync(directory, new("airscan:e0:HP", "ADF Simplex", ScanColorMode.Grayscale, 300), default);
        Assert.Equal(2, result.PageFiles.Count);
        Assert.Equal("--device-name", runner.Request!.Arguments[0]);
        Assert.Contains("airscan:e0:HP", runner.Request.Arguments);
        Assert.Contains("--batch=" + Path.Combine(directory, "page-%04d.png"), runner.Request.Arguments);
        Assert.DoesNotContain("--batch-count", runner.Request.Arguments);
        Assert.Equal(TimeSpan.FromSeconds(17), runner.Request.Timeout);
    }

    [Fact]
    public async Task LimitsFlatbedToOnePageAndMapsLineart()
    {
        Directory.CreateDirectory(directory);
        var runner = new WritingRunner();
        await new SaneSimplexScannerAdapter(runner, new()).CaptureAsync(directory, new("device", "Flatbed", ScanColorMode.BlackAndWhite, 100), default);
        Assert.Contains("--batch-count", runner.Request!.Arguments);
        Assert.Contains("Lineart", runner.Request.Arguments);
    }

    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    private sealed class WritingRunner : IProcessRunner
    {
        public ProcessRequest? Request { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            var pattern = request.Arguments.Single(value => value.StartsWith("--batch=", StringComparison.Ordinal))[8..];
            File.WriteAllText(pattern.Replace("%04d", "0002"), "two");
            File.WriteAllText(pattern.Replace("%04d", "0001"), "one");
            return Task.FromResult(new ProcessResult(0, "", ""));
        }
    }
}
