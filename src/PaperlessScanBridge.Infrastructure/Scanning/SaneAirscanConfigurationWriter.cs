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
        if (!string.IsNullOrWhiteSpace(options.Value.SaneDefaultsDirectory) && Directory.Exists(options.Value.SaneDefaultsDirectory))
            foreach (var source in Directory.EnumerateFiles(options.Value.SaneDefaultsDirectory))
            {
                var destination = Path.Combine(directory, Path.GetFileName(source));
                if (!File.Exists(destination)) File.Copy(source, destination);
            }
        var path = Path.Combine(directory, "airscan.conf");
        var temporary = path + ".tmp";
        var safeName = scanner.DisplayName.Replace("\"", "'", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");
        var content = $"[devices]\n\"{safeName}\" = {scanner.EsclUrl}, eSCL\n";
        await File.WriteAllTextAsync(temporary, content, new UTF8Encoding(false), cancellationToken);
        File.Move(temporary, path, true);
    }
}
