using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class SaneScannerTests
{
    [Fact]
    public async Task InspectsTheOnlyDiscoveredDevice()
    {
        var runner = new StubRunner(new ProcessResult(0, "device `airscan:e0:HP' is a HP scanner", ""), new ProcessResult(0, "--source [ADF|Flatbed]", ""));
        var result = await new SaneScanner(runner, new() { TimeoutSeconds = 5 }).DiscoverAsync(default);
        Assert.True(result.Succeeded);
        Assert.Equal("airscan:e0:HP", result.SelectedDevice?.Identifier);
        Assert.Equal(["--help", "--device-name", "airscan:e0:HP"], runner.Requests[1].Arguments);
    }

    [Fact]
    public async Task RequiresConfigurationForMultipleDevices()
    {
        var result = await new SaneScanner(new StubRunner(new ProcessResult(0, "device `one' is a First scanner\ndevice `two' is a Second scanner", "")), new()).DiscoverAsync(default);
        Assert.Contains("Multiple", result.Diagnostic);
    }

    [Theory]
    [InlineData(2, "exit 2")]
    [InlineData(0, "No scanners")]
    public async Task ReportsDiscoveryFailures(int exitCode, string diagnostic)
    {
        var result = await new SaneScanner(new StubRunner(new ProcessResult(exitCode, "", "private backend detail")), new()).DiscoverAsync(default);
        Assert.Contains(diagnostic, result.Diagnostic);
        Assert.DoesNotContain("private", result.Diagnostic);
    }

    private sealed class StubRunner(params ProcessResult[] results) : IProcessRunner
    {
        private int index;
        public List<ProcessRequest> Requests { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
        { Requests.Add(request); return Task.FromResult(results[index++]); }
    }
}
