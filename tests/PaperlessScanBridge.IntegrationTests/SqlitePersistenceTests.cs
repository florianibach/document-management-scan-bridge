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

    [Fact]
    public async Task ProfileServiceTokensAreEncryptedAndIsolatedAtRest()
    {
        var file = Path.Combine(Path.GetTempPath(), "profile-secret-" + Guid.NewGuid().ToString("N") + ".db");
        var keys = Path.Combine(Path.GetTempPath(), "profile-keys-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite("Data Source=" + file).Options;
            await using (var context = new BridgeDbContext(options)) await context.Database.MigrateAsync();
            var provider = Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create(new DirectoryInfo(keys));
            var repository = new ProfileServiceConfigurationRepository(new TestFactory(options), provider);
            await repository.SaveAsync("user-one", "https://one.test", "token-one", false, false);
            await repository.SaveAsync("user-two", "https://two.test", "token-two", false, false);
            await using (var context = new BridgeDbContext(options))
            {
                var raw = await context.ProfileServiceConfigurations.AsNoTracking().ToListAsync();
                Assert.DoesNotContain(raw, x => x.ProtectedApiToken is "token-one" or "token-two");
            }
            Assert.Equal("token-one", (await repository.GetSecretAsync("user-one"))!.Value.ApiToken);
            Assert.Equal("token-two", (await repository.GetSecretAsync("user-two"))!.Value.ApiToken);
            await repository.DeleteAsync("user-one"); Assert.Null(await repository.GetSecretAsync("user-one"));
            Assert.Equal("token-two", (await repository.GetSecretAsync("user-two"))!.Value.ApiToken);
        }
        finally { if(File.Exists(file)) File.Delete(file); if(Directory.Exists(keys)) Directory.Delete(keys,true); }
    }

    [Fact]
    public async Task ScanSessionsCannotBeClaimedReadOrInferredByAnotherProfile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BridgeDbContext>().UseSqlite(connection).Options;
        await using (var context = new BridgeDbContext(options)) await context.Database.MigrateAsync();
        var repository = new ScanSessionOwnerRepository(new TestFactory(options)); var session = Guid.NewGuid();
        await repository.ClaimAsync(session, "user-one");
        Assert.True(await repository.IsOwnedByAsync(session, "user-one"));
        Assert.False(await repository.IsOwnedByAsync(session, "user-two"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.ClaimAsync(session, "user-two"));
        Assert.False(await repository.IsOwnedByAsync(Guid.NewGuid(), "user-two"));
    }

    private sealed class TestFactory(DbContextOptions<BridgeDbContext> options) : IDbContextFactory<BridgeDbContext>
    {
        public BridgeDbContext CreateDbContext() => new(options);
    }
}
