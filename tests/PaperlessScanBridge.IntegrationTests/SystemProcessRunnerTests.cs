using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Infrastructure.Processes;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class SystemProcessRunnerTests
{
    [Fact]
    public async Task CapturesOutputAndExitCode()
    {
        var result = await new SystemProcessRunner().RunAsync(new("/bin/sh", ["-c", "printf output; printf error >&2; exit 7"], TimeSpan.FromSeconds(5)), default);
        Assert.Equal(7, result.ExitCode);
        Assert.Equal("output", result.StandardOutput);
        Assert.Equal("error", result.StandardError);
    }

    [Fact]
    public async Task MissingExecutableIsMapped() => await Assert.ThrowsAsync<ProcessExecutionException>(() =>
        new SystemProcessRunner().RunAsync(new("/definitely/not/a/program", [], TimeSpan.FromSeconds(5)), default));

    [Fact]
    public async Task TimeoutKillsProcess() => await Assert.ThrowsAsync<ProcessTimeoutException>(() =>
        new SystemProcessRunner().RunAsync(new("/bin/sh", ["-c", "sleep 10"], TimeSpan.FromMilliseconds(50)), default));
}
