using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;

namespace PaperlessScanBridge.Web;

public sealed class DeploymentReadinessHealthCheck(
    IOptions<PersistenceOptions> persistence,
    IOptions<TemporaryStorageOptions> temporaryStorage,
    IOptions<DataProtectionStorageOptions> dataProtectionStorage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var diagnostics = new Dictionary<string, object>();

        try
        {
            var dataSource = new SqliteConnectionStringBuilder(persistence.Value.ConnectionString).DataSource;
            var directory = Path.GetDirectoryName(dataSource);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

            await using var connection = new SqliteConnection(persistence.Value.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            diagnostics["sqlite"] = "reachable";
        }
        catch (Exception exception) when (exception is SqliteException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            diagnostics["sqlite"] = exception.GetType().Name;
            return HealthCheckResult.Unhealthy("SQLite persistence is not ready.", exception, diagnostics);
        }

        var storageCheck = await CheckWritableDirectoryAsync("temporaryStorage", temporaryStorage.Value.Path, cancellationToken);
        diagnostics["temporaryStorage"] = storageCheck.Status;
        if (!storageCheck.IsHealthy)
        {
            return HealthCheckResult.Unhealthy("Temporary storage is not writable.", storageCheck.Exception, diagnostics);
        }

        var dataProtectionCheck = await CheckWritableDirectoryAsync("dataProtectionStorage", dataProtectionStorage.Value.Path, cancellationToken);
        diagnostics["dataProtectionStorage"] = dataProtectionCheck.Status;
        if (!dataProtectionCheck.IsHealthy)
        {
            return HealthCheckResult.Unhealthy("Data-protection key storage is not writable.", dataProtectionCheck.Exception, diagnostics);
        }

        return HealthCheckResult.Healthy("Application dependencies required for startup are ready.", diagnostics);
    }

    private static async Task<DirectoryCheckResult> CheckWritableDirectoryAsync(string prefix, string path, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".{prefix}-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probe, "ready", cancellationToken);
            File.Delete(probe);
            return new("writable");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            return new(exception.GetType().Name, exception);
        }
    }

    private sealed record DirectoryCheckResult(string Status, Exception? Exception = null)
    {
        public bool IsHealthy => Exception is null;
    }
}
