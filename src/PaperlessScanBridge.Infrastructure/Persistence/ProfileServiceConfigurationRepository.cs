using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class ProfileServiceConfigurationRepository(IDbContextFactory<BridgeDbContext> factory, IDataProtectionProvider protection) : IProfileServiceConfigurationRepository
{
    private readonly IDataProtector protector = protection.CreateProtector("PaperlessScanBridge.ProfilePaperlessToken.v1");
    public async Task<(string? BaseUrl, string? ApiToken, bool UseDeploymentToken, DateTimeOffset UpdatedAt)?> GetSecretAsync(string profileId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db.ProfileServiceConfigurations.AsNoTracking().SingleOrDefaultAsync(x => x.ProfileId == profileId, cancellationToken);
        if (row is null) return null;
        return (row.BaseUrl, row.ProtectedApiToken is null ? null : protector.Unprotect(row.ProtectedApiToken), row.UseDeploymentToken, row.UpdatedAt);
    }
    public async Task SaveAsync(string profileId, string? baseUrl, string? apiToken, bool preserveToken, bool useDeploymentToken, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var row = await db.ProfileServiceConfigurations.SingleOrDefaultAsync(x => x.ProfileId == profileId, cancellationToken);
        if (row is null) { row = new() { ProfileId = profileId }; db.ProfileServiceConfigurations.Add(row); }
        row.BaseUrl = baseUrl; row.UseDeploymentToken = useDeploymentToken; row.UpdatedAt = DateTimeOffset.UtcNow;
        if (!preserveToken) row.ProtectedApiToken = string.IsNullOrWhiteSpace(apiToken) ? null : protector.Protect(apiToken);
        await db.SaveChangesAsync(cancellationToken);
    }
    public async Task DeleteAsync(string profileId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await db.ProfileServiceConfigurations.Where(x => x.ProfileId == profileId).ExecuteDeleteAsync(cancellationToken);
    }
}
