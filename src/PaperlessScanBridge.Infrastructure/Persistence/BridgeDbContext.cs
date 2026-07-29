using Microsoft.EntityFrameworkCore;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class BridgeDbContext(DbContextOptions<BridgeDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();
    public DbSet<SelectedScannerEntity> SelectedScanners => Set<SelectedScannerEntity>();
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
}

public sealed class SchemaMarker
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
