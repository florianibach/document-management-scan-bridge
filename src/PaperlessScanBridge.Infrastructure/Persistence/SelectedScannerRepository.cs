using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Scanning;
using System.Text.Json;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class SelectedScannerRepository(IDbContextFactory<BridgeDbContext> factory) : ISelectedScannerRepository
{
    public async Task<SelectedScanner?> GetAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entities = await context.SelectedScanners.AsNoTracking().ToArrayAsync(cancellationToken);
        var entity = entities.MaxBy(value => value.ValidatedAt);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<SelectedScanner>> ListAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entities = await context.SelectedScanners.AsNoTracking().OrderBy(value => value.DisplayName).ToArrayAsync(cancellationToken);
        return entities.Select(Map).ToArray();
    }

    public async Task<SelectedScanner?> GetByIdAsync(long scannerId, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.SelectedScanners.AsNoTracking().SingleOrDefaultAsync(value => value.Id == scannerId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<SelectedScanner> SaveAsync(DiscoveredScanner scanner, DateTimeOffset validatedAt, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.SelectedScanners.SingleOrDefaultAsync(value => value.EsclUrl == scanner.EsclUrl, cancellationToken);
        if (entity is null)
        {
            entity = new() { DisplayName = scanner.DisplayName, IpAddress = scanner.IpAddress, Protocol = scanner.Protocol, EsclUrl = scanner.EsclUrl };
            context.SelectedScanners.Add(entity);
        }
        entity.DisplayName = scanner.DisplayName; entity.IpAddress = scanner.IpAddress; entity.Port = scanner.Port;
        entity.Protocol = scanner.Protocol; entity.EsclUrl = scanner.EsclUrl; entity.ValidatedAt = validatedAt;
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<SelectedScanner> SaveSaneProfileAsync(long scannerId, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.SelectedScanners.SingleAsync(value => value.Id == scannerId, cancellationToken);
        entity.SaneDeviceId = device.Identifier;
        entity.SourcesJson = JsonSerializer.Serialize(capabilities.Sources);
        entity.ResolutionsJson = JsonSerializer.Serialize(capabilities.Resolutions);
        await context.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ScannerRemoval?> RemoveAsync(long scannerId, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var entity = await context.SelectedScanners.SingleOrDefaultAsync(value => value.Id == scannerId, cancellationToken);
        if (entity is null) return null;
        var removed = Map(entity);
        var affectedDefaults = await context.ProfileDefaults.Where(value => value.ScannerId == scannerId).ToArrayAsync(cancellationToken);
        foreach (var defaults in affectedDefaults) { defaults.ScannerId = null; defaults.Source = null; defaults.UpdatedAt = DateTimeOffset.UtcNow; }
        context.SelectedScanners.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var remaining = await context.SelectedScanners.AsNoTracking().ToArrayAsync(cancellationToken);
        var replacementEntity = remaining.MaxBy(value => value.ValidatedAt);
        return new(removed, replacementEntity is null ? null : Map(replacementEntity), affectedDefaults.Length);
    }

    private static SelectedScanner Map(SelectedScannerEntity value) => new(value.Id, value.DisplayName, value.IpAddress,
        value.Port, value.Protocol, value.EsclUrl, value.ValidatedAt, value.SaneDeviceId,
        Deserialize<string>(value.SourcesJson), Deserialize<int>(value.ResolutionsJson));

    private static IReadOnlyList<T> Deserialize<T>(string? json) => json is null ? [] : JsonSerializer.Deserialize<T[]>(json) ?? [];
}
