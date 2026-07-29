namespace PaperlessScanBridge.Application.Scanning;

public interface IScanner
{
    Task<IReadOnlyList<ScannerDevice>> DiscoverAsync(CancellationToken cancellationToken);
}

public sealed record ScannerDevice(string Identifier, string DisplayName);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);
}

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments);
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
