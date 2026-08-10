using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.UnitTests;

public sealed class ProfileServiceConfigurationTests
{
    [Fact]
    public async Task ProfileTokenAndUrlOverrideDeploymentAndTokenIsNotReturned()
    {
        var repository = new Repository(); var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test", ApiToken="fallback" });
        var result = await service.ValidateAndSaveAsync(new("https://profile.test", "personal", true, false, false));
        Assert.True(result.Succeeded); Assert.True(result.Configuration.HasToken);
        Assert.DoesNotContain("personal", result.Configuration.ToString());
        var effective = await service.GetEffectiveAsync();
        Assert.Equal("personal", effective.ApiToken); Assert.Equal(PaperlessConfigurationSource.Profile, effective.TokenSource); Assert.Equal("https://profile.test", effective.BaseUrl);
    }

    [Fact]
    public async Task InvalidOrUnreachableConfigurationIsNotActivated()
    {
        var repository = new Repository(); var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test" }, false);
        Assert.False((await service.ValidateAndSaveAsync(new("http://remote.test", "secret", true, false, false))).Succeeded);
        Assert.Null(await repository.GetSecretAsync("profile-a"));
        Assert.False((await service.ValidateAndSaveAsync(new("https://remote.test", "secret", true, false, false))).Succeeded);
        Assert.Null(await repository.GetSecretAsync("profile-a"));
    }

    [Fact]
    public async Task AnonymousDeploymentTokenIsFallbackWithoutPersistence()
    {
        var repository = new Repository(); var deployment = new PaperlessOptions { BaseUrl="https://deployment.test", ApiToken="fallback" };
        var service = Create(repository, deployment, profiles: new ProfileOptions { Mode=ProfileMode.Anonymous });
        var effective = await service.GetEffectiveAsync();
        Assert.Equal("fallback", effective.ApiToken); Assert.Equal(PaperlessConfigurationSource.Deployment, effective.TokenSource);
        var view = await service.GetAsync();
        Assert.True(view.IsReadOnly); Assert.Equal("https://deployment.test", view.BaseUrl); Assert.True(view.HasToken);
        var save = await service.ValidateAndSaveAsync(new("https://override.test", "other", true, false, false));
        Assert.False(save.Succeeded); Assert.Contains("anonymous mode", save.Errors.Single());
        Assert.Null(await repository.GetSecretAsync("profile-a"));
    }

    private static ProfileServiceConfigurationService Create(Repository repository, PaperlessOptions deployment, bool succeeds=true, ProfileOptions? profiles=null) =>
        new(repository, new Current(), deployment, profiles ?? new() { Mode=ProfileMode.OpenIdConnect }, new(), new Tester(succeeds));
    private sealed class Current : ICurrentProfileAccessor { public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken=default) => Task.FromResult(new UserProfile("profile-a","issuer","subject","A",DateTimeOffset.UtcNow,DateTimeOffset.UtcNow)); }
    private sealed class Tester(bool succeeds) : IPaperlessConnectionTester { public Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> ValidateAsync(string url,string token,CancellationToken cancellationToken=default) => Task.FromResult<(PaperlessResult,PaperlessMetadata?)>((new(succeeds,succeeds?"ok":"unreachable",succeeds?PaperlessFailure.None:PaperlessFailure.Network), succeeds?new([],[],[]):null)); }
    private sealed class Repository : IProfileServiceConfigurationRepository
    {
        private (string? BaseUrl,string? ApiToken,bool UseDeploymentToken,DateTimeOffset UpdatedAt)? value;
        public Task<(string? BaseUrl,string? ApiToken,bool UseDeploymentToken,DateTimeOffset UpdatedAt)?> GetSecretAsync(string profileId,CancellationToken cancellationToken=default)=>Task.FromResult(value);
        public Task SaveAsync(string profileId,string? baseUrl,string? apiToken,bool preserveToken,bool useDeploymentToken,CancellationToken cancellationToken=default){ value=(baseUrl,preserveToken?value?.ApiToken:apiToken,useDeploymentToken,DateTimeOffset.UtcNow); return Task.CompletedTask; }
        public Task DeleteAsync(string profileId,CancellationToken cancellationToken=default){value=null;return Task.CompletedTask;}
    }
}
