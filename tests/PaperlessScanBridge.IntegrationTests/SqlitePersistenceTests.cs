using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Infrastructure.Persistence;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class SqlitePersistenceTests
{
    [Fact]
    public async Task MigrationCreatesFoundationSchema()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite(connection).Options;
        await using var context = new BridgeDbContext(options);

        await context.Database.MigrateAsync();

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Empty(await context.SchemaMarkers.ToListAsync());
        Assert.Empty(await context.SelectedScanners.ToListAsync());
    }

    [Fact]
    public async Task RepositoryKeepsMultiplePreviouslyValidatedScanners()
    {
        var file = Path.Combine(Path.GetTempPath(), "scanner-store-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite("Data Source=" + file).Options;
            await using (var context = new BridgeDbContext(options)) await context.Database.MigrateAsync();
            var repository = new SelectedScannerRepository(new TestFactory(options));
            await repository.SaveAsync(new("one", "Scanner One", "10.0.0.1", 80, "http", "http://10.0.0.1/eSCL"), DateTimeOffset.UtcNow.AddMinutes(-1), default);
            var second = await repository.SaveAsync(new("two", "Scanner Two", "10.0.0.2", 80, "http", "http://10.0.0.2/eSCL"), DateTimeOffset.UtcNow, default);
            Assert.Equal(2, (await repository.ListAsync(default)).Count);
            Assert.Equal("Scanner Two", (await repository.GetByIdAsync(second.Id, default))!.DisplayName);
            Assert.Equal("Scanner Two", (await repository.GetAsync(default))!.DisplayName);
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }

    private sealed class TestFactory(DbContextOptions<BridgeDbContext> options) : IDbContextFactory<BridgeDbContext>
    {
        public BridgeDbContext CreateDbContext() => new(options);
    }
}
