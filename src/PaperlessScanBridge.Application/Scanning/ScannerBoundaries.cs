namespace PaperlessScanBridge.Application.Scanning;

public interface IScanner
{
    Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken);
}

public sealed record ScannerDevice(string Identifier, string DisplayName);

public sealed record ScannerCapabilities(
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Formats,
    IReadOnlyList<int> Resolutions,
    IReadOnlyList<string> PaperSizes);

public sealed record ScannerDiscoveryResult(
    IReadOnlyList<ScannerDevice> Devices,
    ScannerDevice? SelectedDevice,
    ScannerCapabilities? Capabilities,
    string? Diagnostic = null)
{
    public bool Succeeded => Diagnostic is null;
}

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
}

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, TimeSpan Timeout);
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

public sealed class ProcessExecutionException(string message) : Exception(message);
public sealed class ProcessTimeoutException(string executable, TimeSpan timeout)
    : TimeoutException($"'{executable}' did not finish within {timeout.TotalSeconds:0} seconds.");
