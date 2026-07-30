using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Infrastructure.Processes;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class SaneSimplexScannerAdapterIntegrationTests
{
    [Fact]
    public async Task CapturesPagesAcrossTheRealProcessBoundary()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "simplex-process-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var command = Path.Combine(root, "fake-scanimage");
        await File.WriteAllTextAsync(command, "#!/bin/sh\nfor arg in \"$@\"; do case $arg in --batch=*) pattern=${arg#--batch=};; esac; done\nprintf page > \"$(printf \"$pattern\" 1)\"\nprintf page > \"$(printf \"$pattern\" 2)\"\n");
        File.SetUnixFileMode(command, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            var adapter = new SaneSimplexScannerAdapter(new SystemProcessRunner(), new ScannerOptions { Command = command, ScanTimeoutSeconds = 60 });
            var result = await adapter.CaptureAsync(root, new("device", "ADF Simplex", ScanColorMode.Color, 200), default);
            Assert.Equal(["page-0001.png", "page-0002.png"], result.PageFiles.Select(Path.GetFileName));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
