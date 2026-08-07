using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.Application.Profiles;

public sealed record ProfileDefaults(
    long? ScannerId,
    string? Source,
    ScanColorMode ColorMode,
    int ResolutionDpi,
    string? Title,
    int? CorrespondentId,
    int? DocumentTypeId,
    IReadOnlyList<int> TagIds,
    DateTimeOffset UpdatedAt);

public sealed record ProfileValidation(
    bool IsValid,
    IReadOnlyList<string> Errors,
    ProfileDefaults Defaults);

public interface IProfileDefaultsRepository
{
    Task<ProfileDefaults> GetAsync(string profileId, CancellationToken cancellationToken = default);
    Task SaveAsync(string profileId, ProfileDefaults defaults, CancellationToken cancellationToken = default);
    Task ResetAsync(string profileId, CancellationToken cancellationToken = default);
}

public interface IProfileDefaultsService
{
    Task<ProfileDefaults> GetAsync(CancellationToken cancellationToken = default);
    Task<ProfileValidation> ValidateAsync(ProfileDefaults defaults, CancellationToken cancellationToken = default);
    Task<ProfileValidation> SaveAsync(ProfileDefaults defaults, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}

public sealed class ProfileDefaultsService(
    IProfileDefaultsRepository repository,
    ISelectedScannerRepository scanners,
    ICurrentProfileAccessor currentProfile) : IProfileDefaultsService
{
    public async Task<ProfileDefaults> GetAsync(CancellationToken cancellationToken = default) =>
        await repository.GetAsync((await currentProfile.GetRequiredAsync(cancellationToken)).Id, cancellationToken);

    public async Task<ProfileValidation> ValidateAsync(ProfileDefaults defaults, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (defaults.ResolutionDpi is not (100 or 200 or 300 or 600))
            errors.Add("The selected resolution is not supported.");

        if (defaults.ScannerId is { } scannerId)
        {
            var scanner = await scanners.GetByIdAsync(scannerId, cancellationToken);
            if (scanner is null) errors.Add("The saved default scanner is no longer available.");
            else
            {
                if (string.IsNullOrWhiteSpace(defaults.Source) || scanner.Sources?.Contains(defaults.Source) != true)
                    errors.Add("The saved scan source is no longer supported by this scanner.");
                if (scanner.Resolutions?.Contains(defaults.ResolutionDpi) != true)
                    errors.Add("The saved resolution is no longer supported by this scanner.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(defaults.Source))
            errors.Add("Eine Scanquelle kann nur zusammen mit einem Standardscanner gespeichert werden.");

        return new(errors.Count == 0, errors, defaults);
    }

    public async Task<ProfileValidation> SaveAsync(ProfileDefaults defaults, CancellationToken cancellationToken = default)
    {
        var normalized = defaults with { Title = string.IsNullOrWhiteSpace(defaults.Title) ? null : defaults.Title.Trim(), UpdatedAt = DateTimeOffset.UtcNow };
        var validation = await ValidateAsync(normalized, cancellationToken);
        if (validation.IsValid) await repository.SaveAsync((await currentProfile.GetRequiredAsync(cancellationToken)).Id, normalized, cancellationToken);
        return validation;
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default) =>
        await repository.ResetAsync((await currentProfile.GetRequiredAsync(cancellationToken)).Id, cancellationToken);
}
