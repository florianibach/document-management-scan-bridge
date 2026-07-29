using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class ProcessResultTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void SucceededReflectsExitCode(int exitCode, bool expected) =>
        Assert.Equal(expected, new ProcessResult(exitCode, "", "").Succeeded);
}
