namespace PaperlessScanBridge.Application.Profiles;
public interface IScanSessionAccessService
{
    Task ClaimAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
public interface IScanSessionOwnerRepository
{
    Task ClaimAsync(Guid sessionId, string profileId, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByAsync(Guid sessionId, string profileId, CancellationToken cancellationToken = default);
}
public sealed class ScanSessionAccessService(IScanSessionOwnerRepository repository, ICurrentProfileAccessor profile) : IScanSessionAccessService
{
    public async Task ClaimAsync(Guid sessionId, CancellationToken cancellationToken = default) => await repository.ClaimAsync(sessionId, (await profile.GetRequiredAsync(cancellationToken)).Id, cancellationToken);
    public async Task<bool> CanAccessAsync(Guid sessionId, CancellationToken cancellationToken = default) => await repository.IsOwnedByAsync(sessionId, (await profile.GetRequiredAsync(cancellationToken)).Id, cancellationToken);
}
