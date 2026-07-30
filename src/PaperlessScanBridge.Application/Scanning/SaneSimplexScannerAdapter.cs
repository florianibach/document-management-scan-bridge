using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Application.Scanning;

public sealed class SaneSimplexScannerAdapter(IProcessRunner processRunner, ScannerOptions options) : ISimplexScannerAdapter
{
    public async Task<ScanCaptureResult> CaptureAsync(string sessionDirectory, SimplexScanSettings settings, CancellationToken cancellationToken)
    {
        var pattern = Path.Combine(sessionDirectory, "page-%04d.png");
        var arguments = new List<string>
        {
            "--device-name", settings.DeviceId,
            "--source", settings.Source,
            "--mode", settings.ColorMode switch { ScanColorMode.Color => "Color", ScanColorMode.Grayscale => "Gray", _ => "Lineart" },
            "--resolution", settings.ResolutionDpi.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--format", "png",
            "--batch=" + pattern
        };
        if (!IsFeeder(settings.Source)) arguments.AddRange(["--batch-count", "1"]);

        var result = await processRunner.RunAsync(new(options.Command, arguments, TimeSpan.FromSeconds(options.TimeoutSeconds)), cancellationToken);
        if (!result.Succeeded) throw new InvalidOperationException($"scanimage exited with code {result.ExitCode}.");
        return new(Directory.GetFiles(sessionDirectory, "page-*.png").Order(StringComparer.Ordinal).ToArray());
    }

    private static bool IsFeeder(string source) => source.Contains("ADF", StringComparison.OrdinalIgnoreCase)
        || source.Contains("Feeder", StringComparison.OrdinalIgnoreCase);
}
