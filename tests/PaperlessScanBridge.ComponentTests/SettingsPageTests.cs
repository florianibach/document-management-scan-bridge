using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Web.Components.Pages;

namespace PaperlessScanBridge.ComponentTests;

public sealed class SettingsPageTests : BunitContext
{
    [Fact]
    public void AnonymousPaperlessEnvironmentValuesAreReadOnlyAndTokenIsNotExposed()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AnonymousConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());

        var page = Render<Settings>();

        var url = page.Find("#paperless-url");
        Assert.True(url.HasAttribute("readonly"));
        Assert.Equal("https://paperless.environment.test", url.GetAttribute("value"));
        Assert.True(page.Find("#paperless-token-status").HasAttribute("readonly"));
        Assert.Contains("PAPERLESS_URL", page.Markup);
        Assert.Contains("PAPERLESS_TOKEN", page.Markup);
        Assert.DoesNotContain("environment-secret", page.Markup);
        Assert.DoesNotContain(page.FindAll("input"), input => input.Id == "replace-token");
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("aktivieren"));
    }

    private sealed class AnonymousConfigurationStub : IProfileServiceConfigurationService
    {
        public Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken=default) => Task.FromResult(new ProfileServiceConfiguration("https://paperless.environment.test", true, true, false, DateTimeOffset.MinValue, true));
        public Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken=default) => Task.FromResult(new EffectivePaperlessConfiguration("https://paperless.environment.test", "environment-secret", PaperlessConfigurationSource.Deployment, PaperlessConfigurationSource.Deployment));
        public Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input,CancellationToken cancellationToken=default) => throw new NotSupportedException();
        public Task DeleteAsync(CancellationToken cancellationToken=default) => throw new NotSupportedException();
    }
    private sealed class DefaultsStub : IProfileDefaultsService
    {
        private static ProfileDefaults Empty => new(null,null,ScanColorMode.Color,300,null,null,null,[],DateTimeOffset.MinValue);
        public Task<ProfileDefaults> GetAsync(CancellationToken cancellationToken=default)=>Task.FromResult(Empty);
        public Task<ProfileValidation> ValidateAsync(ProfileDefaults defaults,CancellationToken cancellationToken=default)=>Task.FromResult(new ProfileValidation(true,[],defaults));
        public Task<ProfileValidation> SaveAsync(ProfileDefaults defaults,CancellationToken cancellationToken=default)=>ValidateAsync(defaults,cancellationToken);
        public Task ResetAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
    }
    private sealed class DiscoveryStub : IScannerDiscoveryService
    {
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<SelectedScanner?>(null);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<SelectedScanner>>([]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<SelectedScanner> SaveSaneProfileAsync(long scannerId,ScannerDevice device,ScannerCapabilities capabilities,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private sealed class PaperlessStub : IPaperlessClient
    {
        public Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<(PaperlessResult Result,PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request,IProgress<int>? progress=null,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
}
