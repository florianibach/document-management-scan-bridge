using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Web;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class DeploymentReadinessHealthCheckTests
{
    [Fact]
    public async Task ReportsHealthyWhenDatabaseAndStorageAreWritable()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var check = new DeploymentReadinessHealthCheck(
                Options.Create(new PersistenceOptions { ConnectionString = $"Data Source={Path.Combine(root, "bridge.db")}" }),
                Options.Create(new TemporaryStorageOptions { Path = Path.Combine(root, "temp") }),
                Options.Create(new DataProtectionStorageOptions { Path = Path.Combine(root, "keys") }));

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Healthy, result.Status);
            Assert.Equal("reachable", result.Data["sqlite"]);
            Assert.Equal("writable", result.Data["temporaryStorage"]);
            Assert.Equal("writable", result.Data["dataProtectionStorage"]);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReportsUnhealthyWhenSqliteCannotOpen()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var check = new DeploymentReadinessHealthCheck(
                Options.Create(new PersistenceOptions { ConnectionString = $"Data Source={root}" }),
                Options.Create(new TemporaryStorageOptions { Path = Path.Combine(root, "temp") }),
                Options.Create(new DataProtectionStorageOptions { Path = Path.Combine(root, "keys") }));

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Equal("SqliteException", result.Data["sqlite"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
