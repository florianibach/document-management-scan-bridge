using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Application.Scanning;

public sealed class SaneSimplexScannerAdapter(IProcessRunner processRunner, ScannerOptions options) : ISimplexScannerAdapter
{
    public async Task<ScanCaptureResult> CaptureAsync(string sessionDirectory, SimplexScanSettings settings, CancellationToken cancellationToken)
    {
        var pattern = Path.Combine(sessionDirectory, "page-%04d.pnm");
        var arguments = new List<string>
        {
            "--source", settings.Source == ScanSource.Adf ? "ADF" : "Flatbed",
            "--mode", settings.ColorMode switch { ScanColorMode.Color => "Color", ScanColorMode.Grayscale => "Gray", _ => "Lineart" },
            "--resolution", settings.ResolutionDpi.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--format", "pnm",
            "--batch=" + pattern
        };
        if (!string.IsNullOrWhiteSpace(options.DeviceId)) arguments.InsertRange(0, ["--device-name", options.DeviceId]);
        if (settings.Source == ScanSource.Flatbed) arguments.AddRange(["--batch-count", "1"]);

        var result = await processRunner.RunAsync(new(options.Command, arguments, TimeSpan.FromSeconds(options.TimeoutSeconds)), cancellationToken);
        if (!result.Succeeded) throw new InvalidOperationException($"scanimage exited with code {result.ExitCode}.");
        return new(Directory.GetFiles(sessionDirectory, "page-*.pnm").Order(StringComparer.Ordinal).ToArray());
    }
}
