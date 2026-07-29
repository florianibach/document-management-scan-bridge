using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Infrastructure.Persistence;

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
}
