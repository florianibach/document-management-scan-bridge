using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;
using System.ComponentModel.DataAnnotations;

namespace PaperlessScanBridge.UnitTests;

public sealed class ProfileServiceConfigurationTests
{
    [Fact]
    public async Task ProfileTokenAndUrlOverrideDeploymentAndTokenIsReturnedOnlyInOwnerView()
    {
        var repository = new Repository(); var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test", ApiToken="fallback" });
        var result = await service.ValidateAndSaveAsync(new("https://profile.test", "personal", true, false, false));
        Assert.True(result.Succeeded); Assert.True(result.Configuration.HasToken);
        Assert.DoesNotContain("personal", result.Configuration.ToString());
        Assert.Equal("personal", (await service.GetAsync()).ApiToken);
        var effective = await service.GetEffectiveAsync();
        Assert.Equal("personal", effective.ApiToken); Assert.Equal(PaperlessConfigurationSource.Profile, effective.TokenSource); Assert.Equal("https://profile.test", effective.BaseUrl);
    }

    [Fact]
    public async Task InvalidOrUnreachableConfigurationIsNotActivated()
    {
        var repository = new Repository(); var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test" }, false);
        Assert.False((await service.ValidateAndSaveAsync(new("ftp://remote.test", "secret", true, false, false))).Succeeded);
        Assert.Null(await repository.GetSecretAsync("profile-a"));
        Assert.False((await service.ValidateAndSaveAsync(new("https://remote.test", "secret", true, false, false))).Succeeded);
        Assert.Null(await repository.GetSecretAsync("profile-a"));
    }

    [Fact]
    public async Task ValidConfigurationCanBeSavedWhilePaperlessIsOffline()
    {
        var tester=new Tester(false); var repository=new Repository();
        var service=Create(repository,new PaperlessOptions { BaseUrl="https://deployment.test" },tester:tester);

        var result=await service.ValidateAndSaveAsync(new("https://offline.test","secret",true,false,false,ValidateConnection:false));

        Assert.True(result.Succeeded);
        Assert.Null(tester.LastUrl);
        Assert.Equal("https://offline.test",(await service.GetEffectiveAsync()).BaseUrl);
    }

    [Theory]
    [InlineData("http://paperless.lan:8000")]
    [InlineData("https://paperless.example.test")]
    public async Task HttpAndHttpsProfileOverridesUseTheAcceptedUrlForValidation(string url)
    {
        var tester = new Tester(true); var repository = new Repository();
        var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test" }, tester: tester);

        var result = await service.ValidateAndSaveAsync(new(url, "secret", true, false, false));

        Assert.True(result.Succeeded); Assert.Equal(url, tester.LastUrl);
        Assert.Equal(url, (await service.GetEffectiveAsync()).BaseUrl);
    }

    [Theory]
    [InlineData("paperless.lan")]
    [InlineData("/paperless")]
    [InlineData("ftp://paperless.lan")]
    [InlineData("https://user:password@paperless.lan")]
    [InlineData("http://")]
    public async Task InvalidProfileUrlsAreRejected(string url)
    {
        var repository = new Repository(); var service = Create(repository, new PaperlessOptions { BaseUrl="https://deployment.test" });
        var result = await service.ValidateAndSaveAsync(new(url, "secret", true, false, false));
        Assert.False(result.Succeeded); Assert.Contains(PaperlessUrlPolicy.ValidationMessage, result.Errors); Assert.Null(await repository.GetSecretAsync("profile-a"));
    }

    [Theory]
    [InlineData("http://paperless.lan:8000", true)]
    [InlineData("https://paperless.example.test", true)]
    [InlineData("https://token@paperless.example.test", false)]
    [InlineData("file:///tmp/paperless", false)]
    [InlineData("paperless.example.test", false)]
    public void DeploymentUrlUsesTheSamePolicy(string url, bool valid)
    {
        var options = new PaperlessOptions { BaseUrl=url };
        var results = new List<ValidationResult>();
        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), results, true));
        if (!valid) Assert.Contains(results, result => result.ErrorMessage == PaperlessUrlPolicy.ValidationMessage);
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

    [Theory]
    [InlineData(false, null, PaperlessConfigurationSource.None)]
    [InlineData(true, "fallback", PaperlessConfigurationSource.Deployment)]
    public async Task AuthenticatedDeploymentFallbackIsAdministratorControlled(bool enabled, string? expectedToken, PaperlessConfigurationSource source)
    {
        var service = Create(new Repository(), new PaperlessOptions { BaseUrl="https://deployment.test", ApiToken="fallback" },
            serviceOptions: new ProfileServiceOptions { AllowDeploymentTokenFallback=enabled });
        var effective = await service.GetEffectiveAsync();
        Assert.Equal(expectedToken, effective.ApiToken);
        Assert.Equal(source, effective.TokenSource);
        var view = await service.GetAsync();
        Assert.True(view.DeploymentTokenAvailable);
        Assert.Equal(enabled, view.DeploymentTokenFallbackEnabled);
        Assert.Null(view.ApiToken);
    }

    private static ProfileServiceConfigurationService Create(Repository repository, PaperlessOptions deployment, bool succeeds=true, ProfileOptions? profiles=null, Tester? tester=null, ProfileServiceOptions? serviceOptions=null) =>
        new(repository, new Current(), deployment, profiles ?? new() { Mode=ProfileMode.OpenIdConnect }, serviceOptions ?? new(), tester ?? new Tester(succeeds));
    private sealed class Current : ICurrentProfileAccessor { public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken=default) => Task.FromResult(new UserProfile("profile-a","issuer","subject","A",DateTimeOffset.UtcNow,DateTimeOffset.UtcNow)); }
    private sealed class Tester(bool succeeds) : IPaperlessConnectionTester { public string? LastUrl { get; private set; } public Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> ValidateAsync(string url,string token,CancellationToken cancellationToken=default) { LastUrl=url; return Task.FromResult<(PaperlessResult,PaperlessMetadata?)>((new(succeeds,succeeds?"ok":"unreachable",succeeds?PaperlessFailure.None:PaperlessFailure.Network), succeeds?new([],[],[]):null)); } }
    private sealed class Repository : IProfileServiceConfigurationRepository
    {
        private (string? BaseUrl,string? ApiToken,bool UseDeploymentToken,DateTimeOffset UpdatedAt)? value;
        public Task<(string? BaseUrl,string? ApiToken,bool UseDeploymentToken,DateTimeOffset UpdatedAt)?> GetSecretAsync(string profileId,CancellationToken cancellationToken=default)=>Task.FromResult(value);
        public Task SaveAsync(string profileId,string? baseUrl,string? apiToken,bool preserveToken,bool useDeploymentToken,CancellationToken cancellationToken=default){ value=(baseUrl,preserveToken?value?.ApiToken:apiToken,useDeploymentToken,DateTimeOffset.UtcNow); return Task.CompletedTask; }
        public Task DeleteAsync(string profileId,CancellationToken cancellationToken=default){value=null;return Task.CompletedTask;}
    }
}
