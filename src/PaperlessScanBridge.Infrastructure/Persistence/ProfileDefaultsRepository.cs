using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class ProfileDefaultsRepository(IDbContextFactory<BridgeDbContext> factory) : IProfileDefaultsRepository
{
    private static ProfileDefaults Empty => new(null, null, ScanColorMode.Color, 300, null, null, null, [], DateTimeOffset.MinValue);

    public async Task<ProfileDefaults> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ProfileDefaults.AsNoTracking().SingleOrDefaultAsync(value => value.Id == 1, cancellationToken);
        return entity is null ? Empty : new(entity.ScannerId, entity.Source, (ScanColorMode)entity.ColorMode,
            entity.ResolutionDpi, entity.Title, entity.CorrespondentId, entity.DocumentTypeId,
            JsonSerializer.Deserialize<int[]>(entity.TagIdsJson) ?? [], entity.UpdatedAt);
    }

    public async Task SaveAsync(ProfileDefaults defaults, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await context.ProfileDefaults.SingleOrDefaultAsync(value => value.Id == 1, cancellationToken);
        if (entity is null) { entity = new() { Id = 1 }; context.ProfileDefaults.Add(entity); }
        entity.ScannerId = defaults.ScannerId; entity.Source = defaults.Source; entity.ColorMode = (int)defaults.ColorMode;
        entity.ResolutionDpi = defaults.ResolutionDpi; entity.Title = defaults.Title; entity.CorrespondentId = defaults.CorrespondentId;
        entity.DocumentTypeId = defaults.DocumentTypeId; entity.TagIdsJson = JsonSerializer.Serialize(defaults.TagIds.Distinct().Order().ToArray());
        entity.UpdatedAt = defaults.UpdatedAt;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.ProfileDefaults.Where(value => value.Id == 1).ExecuteDeleteAsync(cancellationToken);
    }
}
