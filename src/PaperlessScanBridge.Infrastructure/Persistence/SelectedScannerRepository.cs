using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Scanning;

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
        return await context.SelectedScanners.AsNoTracking().OrderBy(value => value.DisplayName)
            .Select(value => new SelectedScanner(value.Id, value.DisplayName, value.IpAddress, value.Port, value.Protocol, value.EsclUrl, value.ValidatedAt))
            .ToArrayAsync(cancellationToken);
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

    private static SelectedScanner Map(SelectedScannerEntity value) => new(value.Id, value.DisplayName, value.IpAddress,
        value.Port, value.Protocol, value.EsclUrl, value.ValidatedAt);
}
