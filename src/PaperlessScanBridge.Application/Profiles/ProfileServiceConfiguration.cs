using System.ComponentModel.DataAnnotations;
using PaperlessScanBridge.Application.Paperless;

namespace PaperlessScanBridge.Application.Profiles;

public enum PaperlessConfigurationSource { None, Deployment, Profile }
public sealed record ProfileServiceConfiguration(string? BaseUrl, bool HasToken, bool UseDeploymentToken, bool AllowProfileUrlOverride, DateTimeOffset UpdatedAt, bool IsReadOnly = false, string? ApiToken = null, bool DeploymentTokenAvailable = false, bool DeploymentTokenFallbackEnabled = false)
{
    public override string ToString() => $"ProfileServiceConfiguration {{ BaseUrl = {BaseUrl}, HasToken = {HasToken}, AllowProfileUrlOverride = {AllowProfileUrlOverride}, UpdatedAt = {UpdatedAt}, IsReadOnly = {IsReadOnly}, ApiToken = [REDACTED], DeploymentTokenAvailable = {DeploymentTokenAvailable}, DeploymentTokenFallbackEnabled = {DeploymentTokenFallbackEnabled} }}";
}
public sealed record EffectivePaperlessConfiguration(string? BaseUrl, string? ApiToken, PaperlessConfigurationSource UrlSource, PaperlessConfigurationSource TokenSource)
{
    public bool IsConfigured => PaperlessScanBridge.Application.Configuration.PaperlessUrlPolicy.TryParse(BaseUrl, out _) && !string.IsNullOrWhiteSpace(ApiToken);
}
public sealed record ProfileServiceConfigurationInput(string? BaseUrl, string? ApiToken, bool ReplaceToken, bool DeleteToken, bool UseDeploymentToken, bool ValidateConnection = true);
public sealed record ProfileServiceConfigurationResult(bool Succeeded, IReadOnlyList<string> Errors, ProfileServiceConfiguration Configuration, PaperlessMetadata? Metadata = null);

public sealed class ProfileServiceOptions
{
    public const string SectionName = "ProfileServices";
    public bool AllowProfileUrlOverride { get; init; } = true;
    public bool AllowDeploymentTokenFallback { get; init; }
}

public interface IProfileServiceConfigurationRepository
{
    Task<(string? BaseUrl, string? ApiToken, bool UseDeploymentToken, DateTimeOffset UpdatedAt)?> GetSecretAsync(string profileId, CancellationToken cancellationToken = default);
    Task SaveAsync(string profileId, string? baseUrl, string? apiToken, bool preserveToken, bool useDeploymentToken, CancellationToken cancellationToken = default);
    Task DeleteAsync(string profileId, CancellationToken cancellationToken = default);
}
public interface IPaperlessConnectionTester
{
    Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> ValidateAsync(string baseUrl, string apiToken, CancellationToken cancellationToken = default);
}
public interface IProfileServiceConfigurationService
{
    Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken = default);
    Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken = default);
    Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input, CancellationToken cancellationToken = default);
    Task DeleteAsync(CancellationToken cancellationToken = default);
}

public sealed class ProfileServiceConfigurationService(
    IProfileServiceConfigurationRepository repository, ICurrentProfileAccessor currentProfile,
    PaperlessScanBridge.Application.Configuration.PaperlessOptions deployment, ProfileOptions profiles,
    ProfileServiceOptions options, IPaperlessConnectionTester tester) : IProfileServiceConfigurationService
{
    public async Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await currentProfile.GetRequiredAsync(cancellationToken);
        if (profiles.Mode == ProfileMode.Anonymous)
            return new(deployment.BaseUrl, !string.IsNullOrWhiteSpace(deployment.ApiToken), true, false, DateTimeOffset.MinValue, true);
        var stored = await repository.GetSecretAsync(profile.Id, cancellationToken);
        return new(stored?.BaseUrl ?? deployment.BaseUrl, !string.IsNullOrWhiteSpace(stored?.ApiToken), false, options.AllowProfileUrlOverride,
            stored?.UpdatedAt ?? DateTimeOffset.MinValue, false, stored?.ApiToken, !string.IsNullOrWhiteSpace(deployment.ApiToken), options.AllowDeploymentTokenFallback);
    }

    public async Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken = default)
    {
        var profile = await currentProfile.GetRequiredAsync(cancellationToken);
        if (profiles.Mode == ProfileMode.Anonymous)
            return new(deployment.BaseUrl, deployment.ApiToken, PaperlessConfigurationSource.Deployment,
                string.IsNullOrWhiteSpace(deployment.ApiToken) ? PaperlessConfigurationSource.None : PaperlessConfigurationSource.Deployment);
        var stored = await repository.GetSecretAsync(profile.Id, cancellationToken);
        var url = options.AllowProfileUrlOverride && stored is { } storedUrl && !string.IsNullOrWhiteSpace(storedUrl.BaseUrl) ? storedUrl.BaseUrl : deployment.BaseUrl;
        var useDeployment = options.AllowDeploymentTokenFallback;
        var token = stored is { } storedToken && !string.IsNullOrWhiteSpace(storedToken.ApiToken) ? storedToken.ApiToken : useDeployment ? deployment.ApiToken : null;
        return new(url, token,
            options.AllowProfileUrlOverride && !string.IsNullOrWhiteSpace(stored?.BaseUrl) ? PaperlessConfigurationSource.Profile : PaperlessConfigurationSource.Deployment,
            !string.IsNullOrWhiteSpace(stored?.ApiToken) ? PaperlessConfigurationSource.Profile : useDeployment && !string.IsNullOrWhiteSpace(deployment.ApiToken) ? PaperlessConfigurationSource.Deployment : PaperlessConfigurationSource.None);
    }

    public async Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input, CancellationToken cancellationToken = default)
    {
        var profile = await currentProfile.GetRequiredAsync(cancellationToken);
        if (profiles.Mode == ProfileMode.Anonymous)
        {
            var readOnly = new ProfileServiceConfiguration(deployment.BaseUrl, !string.IsNullOrWhiteSpace(deployment.ApiToken), true, false, DateTimeOffset.MinValue, true);
            return new(false, ["In anonymous mode, Paperless configuration is provided exclusively through PAPERLESS_URL and PAPERLESS_TOKEN."], readOnly);
        }
        var existing = await repository.GetSecretAsync(profile.Id, cancellationToken);
        var errors = new List<string>();
        var baseUrl = string.IsNullOrWhiteSpace(input.BaseUrl) ? deployment.BaseUrl : input.BaseUrl.Trim().TrimEnd('/');
        if (baseUrl is not null && !PaperlessScanBridge.Application.Configuration.PaperlessUrlPolicy.TryParse(baseUrl, out _))
            errors.Add(PaperlessScanBridge.Application.Configuration.PaperlessUrlPolicy.ValidationMessage);
        if (!options.AllowProfileUrlOverride && !string.Equals(baseUrl, deployment.BaseUrl, StringComparison.Ordinal)) errors.Add("Profile URL changes are disabled by the deployment.");
        var token = input.DeleteToken ? null : input.ReplaceToken ? input.ApiToken?.Trim() : existing?.ApiToken;
        var effectiveUrl = baseUrl ?? deployment.BaseUrl;
        var effectiveToken = token ?? (options.AllowDeploymentTokenFallback ? deployment.ApiToken : null);
        if (string.IsNullOrWhiteSpace(effectiveToken)) errors.Add("Enter a profile API token. An administrator-managed fallback is not available.");
        PaperlessMetadata? metadata = null;
        if (errors.Count == 0 && input.ValidateConnection)
        {
            var check = await tester.ValidateAsync(effectiveUrl, effectiveToken!, cancellationToken);
            if (!check.Result.Succeeded) errors.Add(check.Result.Message); else metadata = check.Metadata;
        }
        if (errors.Count == 0)
            await repository.SaveAsync(profile.Id, options.AllowProfileUrlOverride ? baseUrl : null, input.DeleteToken ? null : input.ReplaceToken ? input.ApiToken?.Trim() : null, !input.ReplaceToken && !input.DeleteToken, false, cancellationToken);
        var view = new ProfileServiceConfiguration(baseUrl, !string.IsNullOrWhiteSpace(token), false, options.AllowProfileUrlOverride, errors.Count == 0 ? DateTimeOffset.UtcNow : existing?.UpdatedAt ?? DateTimeOffset.MinValue,
            false, token, !string.IsNullOrWhiteSpace(deployment.ApiToken), options.AllowDeploymentTokenFallback);
        return new(errors.Count == 0, errors, view, metadata);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (profiles.Mode == ProfileMode.Anonymous) throw new InvalidOperationException("Anonymous service configuration is deployment-controlled.");
        var profile = await currentProfile.GetRequiredAsync(cancellationToken);
        var existing = await repository.GetSecretAsync(profile.Id, cancellationToken);
        await repository.SaveAsync(profile.Id, existing?.BaseUrl, null, false, false, cancellationToken);
    }
}
