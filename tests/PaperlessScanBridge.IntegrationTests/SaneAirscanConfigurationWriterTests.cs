using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Infrastructure.Scanning;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class SaneAirscanConfigurationWriterTests
{
    [Fact]
    public async Task WritesValidatedSelectionAtomically()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var writer = new SaneAirscanConfigurationWriter(Options.Create(new ScannerDiscoveryOptions { SaneConfigurationDirectory = directory }));
            await writer.WriteAsync(new SelectedScanner(1, "HP", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL", DateTimeOffset.UtcNow), default);
            Assert.Contains("http://10.0.0.2/eSCL, eSCL", await File.ReadAllTextAsync(Path.Combine(directory, "airscan.conf")));
            Assert.Contains("[options]\ndiscovery = disable", await File.ReadAllTextAsync(Path.Combine(directory, "airscan.conf")));
            Assert.False(File.Exists(Path.Combine(directory, "airscan.conf.tmp")));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task ClearRemovesOnlyGeneratedConfigurationFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "dll.conf"), "unrelated");
            var writer = new SaneAirscanConfigurationWriter(Options.Create(new ScannerDiscoveryOptions { SaneConfigurationDirectory = directory }));
            await writer.WriteAsync(new SelectedScanner(1,"HP","10.0.0.2",80,"http","http://10.0.0.2/eSCL",DateTimeOffset.UtcNow),default);
            await writer.ClearAsync(default);
            Assert.False(File.Exists(Path.Combine(directory,"airscan.conf")));
            Assert.Equal("unrelated", await File.ReadAllTextAsync(Path.Combine(directory,"dll.conf")));
        }
        finally { Directory.Delete(directory,true); }
    }
}
