using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Infrastructure.Persistence;

public sealed class UserProfileRepository(IDbContextFactory<BridgeDbContext> factory) : IUserProfileRepository
{
    public async Task<UserProfile> GetOrCreateAsync(string issuer, string subject, string displayName, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var existing = await context.UserProfiles.SingleOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            existing = new() { Id = Guid.NewGuid().ToString("N"), Issuer = issuer, Subject = subject, DisplayName = displayName, CreatedAt = now };
            context.UserProfiles.Add(existing);
        }
        existing.DisplayName = displayName;
        existing.LastSeenAt = now;
        await context.SaveChangesAsync(cancellationToken);
        return new(existing.Id, existing.Issuer, existing.Subject, existing.DisplayName, existing.CreatedAt, existing.LastSeenAt);
    }

    public async Task RemoveAsync(string issuer, string subject, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        await context.UserProfiles.Where(x => x.Issuer == issuer && x.Subject == subject).ExecuteDeleteAsync(cancellationToken);
    }
}
