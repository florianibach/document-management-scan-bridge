using Microsoft.EntityFrameworkCore;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class BridgeDbContext(DbContextOptions<BridgeDbContext> options) : DbContext(options)
{
    public DbSet<SchemaMarker> SchemaMarkers => Set<SchemaMarker>();
}

public sealed class SchemaMarker
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
