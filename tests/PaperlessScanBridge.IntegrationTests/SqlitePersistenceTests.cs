using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Infrastructure.Persistence;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Application.Profiles;

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
        Assert.Empty(await context.ProfileDefaults.ToListAsync());
    }

    [Fact]
    public async Task ProfileDefaultsSurviveRestartUpdateAndReset()
    {
        var file = Path.Combine(Path.GetTempPath(), "profile-store-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite("Data Source=" + file).Options;
            await using (var context = new BridgeDbContext(options)) await context.Database.MigrateAsync();
            var repository = new ProfileDefaultsRepository(new TestFactory(options));
            await repository.SaveAsync("profile-a", new(null,null,ScanColorMode.Grayscale,200," Rechnung ",4,5,[9,7],DateTimeOffset.UtcNow));
            repository = new ProfileDefaultsRepository(new TestFactory(options));
            var restarted = await repository.GetAsync("profile-a");
            Assert.Equal(ScanColorMode.Grayscale, restarted.ColorMode); Assert.Equal([7,9], restarted.TagIds);
            await repository.SaveAsync("profile-a", restarted with { ResolutionDpi = 300, Title = "Updated" });
            Assert.Equal("Updated", (await repository.GetAsync("profile-a")).Title);
            await repository.ResetAsync("profile-a");
            Assert.Equal(300, (await repository.GetAsync("profile-a")).ResolutionDpi);
            Assert.Null((await repository.GetAsync("profile-a")).Title);
        }
        finally { if(File.Exists(file)) File.Delete(file); }
    }

    [Fact]
    public async Task ProfileDefaultsAreIsolatedByProfileId()
    {
        var file = Path.Combine(Path.GetTempPath(), "profile-isolation-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite("Data Source=" + file).Options;
            await using (var context = new BridgeDbContext(options)) await context.Database.MigrateAsync();
            var repository = new ProfileDefaultsRepository(new TestFactory(options));

            await repository.SaveAsync("user-one", new(null, null, ScanColorMode.Grayscale, 200, "User One", null, null, [], DateTimeOffset.UtcNow));
            await repository.SaveAsync("user-two", new(null, null, ScanColorMode.Color, 300, "User Two", null, null, [], DateTimeOffset.UtcNow));

            Assert.Equal("User One", (await repository.GetAsync("user-one")).Title);
            Assert.Equal("User Two", (await repository.GetAsync("user-two")).Title);
        }
        finally { if(File.Exists(file)) File.Delete(file); }
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
            var profile = await repository.SaveSaneProfileAsync(second.Id, new("airscan:e0:two", "Scanner Two"),
                new(["Flatbed", "ADF"], ["Color"], [200, 300], ["A4"]), default);
            Assert.Equal("airscan:e0:two", profile.SaneDeviceId);
            Assert.Equal(["Flatbed", "ADF"], profile.Sources);
            Assert.Equal([200, 300], (await repository.GetByIdAsync(second.Id, default))!.Resolutions);
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }

    private sealed class TestFactory(DbContextOptions<BridgeDbContext> options) : IDbContextFactory<BridgeDbContext>
    {
        public BridgeDbContext CreateDbContext() => new(options);
    }
}
