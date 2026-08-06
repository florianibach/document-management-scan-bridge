using System.ComponentModel.DataAnnotations;

namespace PaperlessScanBridge.Application.Profiles;

public enum ProfileMode { Anonymous, OpenIdConnect }
public enum LegacyDefaultsMigrationMode { MoveToAnonymous, Reset }
public enum ProfileSignOutMode { ProviderWithLocalFallback, LocalOnly }

public sealed class ProfileOptions
{
    public const string SectionName = "Profiles";
    [Required] public ProfileMode Mode { get; init; } = ProfileMode.Anonymous;
    [Required, MinLength(12)] public string AnonymousSubject { get; init; } = "scan-bridge-local-anonymous-profile";
    public LegacyDefaultsMigrationMode LegacyDefaultsMigration { get; init; } = LegacyDefaultsMigrationMode.MoveToAnonymous;
    public ProfileSignOutMode SignOutMode { get; init; } = ProfileSignOutMode.ProviderWithLocalFallback;
    public string? RemoteSignOutUrl { get; init; }
}

public sealed record UserProfile(string Id, string Issuer, string Subject, string DisplayName, DateTimeOffset CreatedAt, DateTimeOffset LastSeenAt);

public interface ICurrentProfileAccessor
{
    Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default);
}

public interface IUserProfileRepository
{
    Task<UserProfile> GetOrCreateAsync(string issuer, string subject, string displayName, CancellationToken cancellationToken = default);
    Task RemoveAsync(string issuer, string subject, CancellationToken cancellationToken = default);
}
