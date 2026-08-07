using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Profiles;
namespace PaperlessScanBridge.Infrastructure.Persistence;
public sealed class ScanSessionOwnerRepository(IDbContextFactory<BridgeDbContext> factory) : IScanSessionOwnerRepository
{
    public async Task ClaimAsync(Guid sessionId,string profileId,CancellationToken cancellationToken=default)
    {
        await using var db=await factory.CreateDbContextAsync(cancellationToken);
        var existing=await db.ScanSessionOwners.SingleOrDefaultAsync(x=>x.SessionId==sessionId,cancellationToken);
        if(existing is null){db.ScanSessionOwners.Add(new(){SessionId=sessionId,ProfileId=profileId,CreatedAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync(cancellationToken);}
        else if(existing.ProfileId!=profileId) throw new UnauthorizedAccessException("Scan session belongs to another profile.");
    }
    public async Task<bool> IsOwnedByAsync(Guid sessionId,string profileId,CancellationToken cancellationToken=default)
    { await using var db=await factory.CreateDbContextAsync(cancellationToken); return await db.ScanSessionOwners.AnyAsync(x=>x.SessionId==sessionId&&x.ProfileId==profileId,cancellationToken); }
}
