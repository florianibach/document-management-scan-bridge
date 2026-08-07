using Microsoft.EntityFrameworkCore;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class BridgeDbContext(DbContextOptions<BridgeDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();
    public DbSet<SelectedScannerEntity> SelectedScanners => Set<SelectedScannerEntity>();
    public DbSet<ProfileDefaultsEntity> ProfileDefaults => Set<ProfileDefaultsEntity>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<ProfileServiceConfigurationEntity> ProfileServiceConfigurations => Set<ProfileServiceConfigurationEntity>();
    public DbSet<ScanSessionOwnerEntity> ScanSessionOwners => Set<ScanSessionOwnerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProfileDefaultsEntity>().HasIndex(value => value.ProfileId).IsUnique();
        modelBuilder.Entity<UserProfileEntity>().HasIndex(value => new { value.Issuer, value.Subject }).IsUnique();
        modelBuilder.Entity<ProfileServiceConfigurationEntity>().HasIndex(value => value.ProfileId).IsUnique();
        modelBuilder.Entity<ScanSessionOwnerEntity>().HasKey(value => value.SessionId);
    }
}

public sealed class ProfileDefaultsEntity
{
    public int Id { get; set; }
    public string ProfileId { get; set; } = "anonymous";
    public long? ScannerId { get; set; }
    public string? Source { get; set; }
    public int ColorMode { get; set; }
    public int ResolutionDpi { get; set; }
    public string? Title { get; set; }
    public int? CorrespondentId { get; set; }
    public int? DocumentTypeId { get; set; }
    public string TagIdsJson { get; set; } = "[]";
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SelectedScannerEntity
{
    public long Id { get; set; }
    public required string DisplayName { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; }
    public required string Protocol { get; set; }
    public required string EsclUrl { get; set; }
    public DateTimeOffset ValidatedAt { get; set; }
    public string? SaneDeviceId { get; set; }
    public string? SourcesJson { get; set; }
    public string? ResolutionsJson { get; set; }
}

public sealed class SchemaMarker
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UserProfileEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public required string Issuer { get; set; }
    public required string Subject { get; set; }
    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}

public sealed class ProfileServiceConfigurationEntity
{
    public int Id { get; set; }
    public required string ProfileId { get; set; }
    public string? BaseUrl { get; set; }
    public string? ProtectedApiToken { get; set; }
    public bool UseDeploymentToken { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ScanSessionOwnerEntity
{
    public Guid SessionId { get; set; }
    public required string ProfileId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
