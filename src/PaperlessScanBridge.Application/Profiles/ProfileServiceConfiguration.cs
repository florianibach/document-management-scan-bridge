using System.ComponentModel.DataAnnotations;
using PaperlessScanBridge.Application.Paperless;

namespace PaperlessScanBridge.Application.Profiles;

public enum PaperlessConfigurationSource { None, Deployment, Profile }
public sealed record ProfileServiceConfiguration(string? BaseUrl, bool HasToken, bool UseDeploymentToken, bool AllowProfileUrlOverride, DateTimeOffset UpdatedAt, bool IsReadOnly = false);
public sealed record EffectivePaperlessConfiguration(string? BaseUrl, string? ApiToken, PaperlessConfigurationSource UrlSource, PaperlessConfigurationSource TokenSource)
{
    public bool IsConfigured => Uri.TryCreate(BaseUrl, UriKind.Absolute, out _) && !string.IsNullOrWhiteSpace(ApiToken);
}
public sealed record ProfileServiceConfigurationInput(string? BaseUrl, string? ApiToken, bool ReplaceToken, bool DeleteToken, bool UseDeploymentToken);
public sealed record ProfileServiceConfigurationResult(bool Succeeded, IReadOnlyList<string> Errors, ProfileServiceConfiguration Configuration, PaperlessMetadata? Metadata = null);

public sealed class ProfileServiceOptions
{
    public const string SectionName = "ProfileServices";
    public bool AllowProfileUrlOverride { get; init; } = true;
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
        return new(stored?.BaseUrl, !string.IsNullOrWhiteSpace(stored?.ApiToken), stored?.UseDeploymentToken ?? false, options.AllowProfileUrlOverride, stored?.UpdatedAt ?? DateTimeOffset.MinValue);
    }

    public async Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken = default)
    {
        var profile = await currentProfile.GetRequiredAsync(cancellationToken);
        if (profiles.Mode == ProfileMode.Anonymous)
            return new(deployment.BaseUrl, deployment.ApiToken, PaperlessConfigurationSource.Deployment,
                string.IsNullOrWhiteSpace(deployment.ApiToken) ? PaperlessConfigurationSource.None : PaperlessConfigurationSource.Deployment);
        var stored = await repository.GetSecretAsync(profile.Id, cancellationToken);
        var url = options.AllowProfileUrlOverride && stored is { } storedUrl && !string.IsNullOrWhiteSpace(storedUrl.BaseUrl) ? storedUrl.BaseUrl : deployment.BaseUrl;
        var useDeployment = stored?.UseDeploymentToken ?? false;
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
            return new(false, ["Im anonymen Modus wird die Paperless-Konfiguration ausschließlich über PAPERLESS_URL und PAPERLESS_TOKEN bereitgestellt."], readOnly);
        }
        var existing = await repository.GetSecretAsync(profile.Id, cancellationToken);
        var errors = new List<string>();
        var baseUrl = string.IsNullOrWhiteSpace(input.BaseUrl) ? null : input.BaseUrl.Trim().TrimEnd('/');
        if (baseUrl is not null && (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            errors.Add("Die Paperless-URL muss HTTPS verwenden; HTTP ist nur für localhost zulässig.");
        if (!options.AllowProfileUrlOverride && baseUrl is not null) errors.Add("Profilbezogene URL-Änderungen sind durch die Bereitstellung gesperrt.");
        var token = input.DeleteToken ? null : input.ReplaceToken ? input.ApiToken?.Trim() : existing?.ApiToken;
        var effectiveUrl = baseUrl ?? deployment.BaseUrl;
        var effectiveToken = token ?? (input.UseDeploymentToken ? deployment.ApiToken : null);
        if (string.IsNullOrWhiteSpace(effectiveToken)) errors.Add("Ein API-Token ist erforderlich oder der Bereitstellungs-Token muss aktiviert werden.");
        PaperlessMetadata? metadata = null;
        if (errors.Count == 0)
        {
            var check = await tester.ValidateAsync(effectiveUrl, effectiveToken!, cancellationToken);
            if (!check.Result.Succeeded) errors.Add(check.Result.Message); else metadata = check.Metadata;
        }
        if (errors.Count == 0)
            await repository.SaveAsync(profile.Id, baseUrl, input.DeleteToken ? null : input.ReplaceToken ? input.ApiToken?.Trim() : null, !input.ReplaceToken && !input.DeleteToken, input.UseDeploymentToken, cancellationToken);
        var view = new ProfileServiceConfiguration(baseUrl, !string.IsNullOrWhiteSpace(token), input.UseDeploymentToken, options.AllowProfileUrlOverride, errors.Count == 0 ? DateTimeOffset.UtcNow : existing?.UpdatedAt ?? DateTimeOffset.MinValue);
        return new(errors.Count == 0, errors, view, metadata);
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (profiles.Mode == ProfileMode.Anonymous) throw new InvalidOperationException("Anonymous service configuration is deployment-controlled.");
        await repository.DeleteAsync((await currentProfile.GetRequiredAsync(cancellationToken)).Id, cancellationToken);
    }
}
