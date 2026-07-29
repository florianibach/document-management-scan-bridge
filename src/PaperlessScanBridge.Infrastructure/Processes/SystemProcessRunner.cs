using System.ComponentModel;
using System.Diagnostics;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Infrastructure.Processes;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new()
            {
                FileName = request.FileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        foreach (var argument in request.Arguments) process.StartInfo.ArgumentList.Add(argument);

        try
        {
            process.Start();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            throw new ProcessExecutionException($"Unable to start '{Path.GetFileName(request.FileName)}'.");
        }

        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await process.WaitForExitAsync(combined.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            throw new ProcessTimeoutException(Path.GetFileName(request.FileName), request.Timeout);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new(process.ExitCode, await stdout, await stderr);
    }
}
