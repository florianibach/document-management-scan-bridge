using System.Text;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Infrastructure.Scanning;

public sealed class SaneAirscanConfigurationWriter(IOptions<ScannerDiscoveryOptions> options) : ISaneAirscanConfigurationWriter
{
    public async Task WriteAsync(SelectedScanner scanner, CancellationToken cancellationToken)
    {
        var directory = options.Value.SaneConfigurationDirectory;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "airscan.conf");
        var temporary = path + ".tmp";
        var safeName = scanner.DisplayName.Replace("\"", "'", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");
        var content = $"[devices]\n\"{safeName}\" = {scanner.EsclUrl}, eSCL\n\n[options]\ndiscovery = disable\n";
        await File.WriteAllTextAsync(temporary, content, new UTF8Encoding(false), cancellationToken);
        File.Move(temporary, path, true);
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.Combine(options.Value.SaneConfigurationDirectory, "airscan.conf");
        File.Delete(path);
        File.Delete(path + ".tmp");
        return Task.CompletedTask;
    }
}
