using PaperlessScanBridge.Application.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PaperlessScanBridge.Application.Scanning;

public sealed class SaneScanner(IProcessRunner processRunner, ScannerOptions settings, ILogger<SaneScanner>? suppliedLogger = null) : IScanner
{
    private readonly ILogger<SaneScanner> logger = suppliedLogger ?? NullLogger<SaneScanner>.Instance;
    public async Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting SANE scanner discovery with command {Command} -L", Path.GetFileName(settings.Command));
            var timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
            var discovery = await processRunner.RunAsync(new(settings.Command, ["-L"], timeout), cancellationToken);
            if (!discovery.Succeeded)
                return Failure($"Scanner discovery failed (exit {discovery.ExitCode}). Check SANE configuration and network access.");

            var devices = SaneOutputParser.ParseDevices(discovery.StandardOutput);
            logger.LogInformation("SANE scanner discovery returned {DeviceCount} device(s)", devices.Count);
            if (devices.Count == 0)
                return new([], null, null, "No scanners were discovered. Check that the scanner is online and mDNS/UDP discovery reaches the container.");

            ScannerDevice? selected = null;
            if (!string.IsNullOrWhiteSpace(settings.DeviceId))
            {
                selected = devices.FirstOrDefault(device => device.Identifier == settings.DeviceId);
                if (selected is null)
                    return new(devices, null, null, "The configured scanner was not discovered. Verify Scanner:DeviceId or select a reported device.");
            }
            else if (devices.Count == 1) selected = devices[0];
            else return new(devices, null, null, "Multiple scanners were discovered. Configure Scanner:DeviceId to select one.");

            logger.LogInformation("Inspecting SANE capabilities for device {DeviceIdentifier}", selected.Identifier);
            var inspection = await processRunner.RunAsync(new(settings.Command, ["--help", "--device-name", selected.Identifier], timeout), cancellationToken);
            if (!inspection.Succeeded)
                return new(devices, selected, null, $"Scanner option inspection failed (exit {inspection.ExitCode}). Verify device access and SANE permissions.");

            return new(devices, selected, SaneOutputParser.ParseCapabilities(inspection.StandardOutput));
        }
        catch (ProcessTimeoutException)
        {
            return Failure($"Scanner discovery timed out after {settings.TimeoutSeconds} seconds. Check network routing and scanner availability.");
        }
        catch (ProcessExecutionException)
        {
            return Failure($"Scanner command '{Path.GetFileName(settings.Command)}' could not be started. Install SANE and verify Scanner:Command.");
        }
    }

    private static ScannerDiscoveryResult Failure(string diagnostic) => new([], null, null, diagnostic);
}
