using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Web.Components.Pages;

namespace PaperlessScanBridge.ComponentTests;

public sealed class SettingsPageTests : BunitContext
{
    private readonly BunitJSModuleInterop notifications;
    public SettingsPageTests()
    {
        Services.AddSingleton(new PaperlessOptions());
        notifications = JSInterop.SetupModule("./scanNotifications.js");
        notifications.Setup<string>("getState").SetResult("disabled");
        notifications.Setup<string>("enable").SetResult("enabled");
        notifications.Setup<string>("disable").SetResult("disabled");
    }
    [Fact]
    public void AnonymousPaperlessEnvironmentValuesAreReadOnlyAndTokenIsNotExposed()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AnonymousConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());

        var page = Render<Settings>();

        var url = page.Find("#paperless-url-value");
        Assert.Equal("DD", url.TagName);
        Assert.Contains("bg-body-secondary", url.ClassList);
        Assert.Equal("https://paperless.environment.test", url.TextContent.Trim());
        Assert.Equal("DD", page.Find("#paperless-token-status").TagName);
        Assert.Contains("••••••••••••", page.Find("#paperless-token-status").TextContent);
        Assert.Contains("PAPERLESS_URL", page.Markup);
        Assert.Contains("PAPERLESS_TOKEN", page.Markup);
        Assert.DoesNotContain("environment-secret", page.Markup);
        Assert.DoesNotContain(page.FindAll("input"), input => input.Id == "replace-token");
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("Validate and activate connection"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HttpWarningIsShownForReadOnlyAndEditablePaperlessConfiguration(bool readOnly)
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new HttpConfigurationStub(readOnly));
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());

        var page = Render<Settings>();

        var warning = page.Find("#paperless-http-warning");
        Assert.Equal("alert", warning.GetAttribute("role"));
        Assert.Contains("Unencrypted Paperless connection", warning.TextContent);
        Assert.Contains("API token, metadata, or documents", warning.TextContent);
        Assert.Contains("Use HTTPS whenever possible", warning.TextContent);
    }

    [Fact]
    public void HttpsConfigurationDoesNotShowHttpWarning()
    {
        AddAnonymousServices();
        var page = Render<Settings>();
        Assert.Empty(page.FindAll("#paperless-http-warning"));
    }

    [Fact]
    public void DeploymentCanSuppressHttpWarningWithoutChangingHttpConfiguration()
    {
        Services.AddSingleton(new PaperlessOptions { ShowHttpWarning=false });
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new HttpConfigurationStub(true));
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());

        var page = Render<Settings>();

        Assert.Empty(page.FindAll("#paperless-http-warning"));
        Assert.Equal("http://paperless.lan:8000", page.Find("#paperless-url-value").TextContent.Trim());
    }

    [Fact]
    public async Task NotificationPermissionIsManagedOnlyFromSettingsAfterExplicitAction()
    {
        AddAnonymousServices();
        var page = Render<Settings>();
        Assert.DoesNotContain(notifications.Invocations, invocation => invocation.Identifier == "enable");
        await page.FindAll("button").Single(button => button.TextContent.Contains("Enable notifications")).ClickAsync(new());
        Assert.Single(notifications.Invocations, invocation => invocation.Identifier == "enable");
        Assert.Contains("Disable notifications", page.Markup);
        Assert.Contains("open tab is in the background", page.Markup);
    }

    [Theory]
    [InlineData("denied", "Notifications are blocked by the browser")]
    [InlineData("unsupported", "This browser does not support notifications")]
    public void UnavailableNotificationsKeepAnActionableVisibleState(string state, string diagnostic)
    {
        notifications.Setup<string>("getState").SetResult(state);
        AddAnonymousServices();

        var page = Render<Settings>();

        page.WaitForAssertion(() => Assert.Contains(diagnostic, page.Markup));
        Assert.True(page.FindAll("button").Single(button => button.TextContent.Contains("Enable notifications")).HasAttribute("disabled"));
        Assert.DoesNotContain(notifications.Invocations, invocation => invocation.Identifier == "enable");
    }

    [Fact]
    public async Task AuthenticatedUserCanRevealOnlyNewTokenBeforeSaving()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AuthenticatedConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());
        var page = Render<Settings>();
        var token = page.Find("#paperless-token");
        Assert.Equal("password", token.GetAttribute("type"));
        await token.ChangeAsync("new-visible-token");
        Assert.DoesNotContain("stored-secret-never-rendered", page.Markup);
        await page.Find("#toggle-paperless-token").ClickAsync(new());
        Assert.Equal("text", page.Find("#paperless-token").GetAttribute("type"));
        Assert.Equal("new-visible-token", page.Find("#paperless-token").GetAttribute("value"));
        Assert.Contains("cannot be displayed again", page.Markup);
    }

    [Fact]
    public async Task SavedScannerRequiresConfirmationAndReportsActiveScanConflict()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new SavedScannerDiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AnonymousConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());
        var page = Render<Settings>();

        await page.FindAll("button").Single(button => button.TextContent.Contains("Forget scanner")).ClickAsync(new());
        Assert.Contains("repairs every profile default", page.Markup);
        Assert.Contains("unrelated system configuration are not changed", page.Markup);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Confirm forget")).ClickAsync(new());
        Assert.Contains("A scan is active", page.Markup);
        Assert.Contains("Finish or cancel it", page.Markup);
    }

    [Fact]
    public async Task NewlyAddedScannerImmediatelyPopulatesScanDefaults()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new AddScannerDiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AnonymousConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());
        var page = Render<Settings>();

        await page.FindAll("button").Single(button => button.TextContent.Contains("Search for scanners")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Select and validate")).ClickAsync(new());

        Assert.Equal("7", page.Find("#default-scanner").GetAttribute("value"));
        Assert.Contains("ADF Simplex", page.Find("#default-source").TextContent);
        Assert.Equal("Flatbed", page.Find("#default-source").GetAttribute("value"));
    }

    private void AddAnonymousServices()
    {
        Services.AddSingleton<IProfileDefaultsService>(new DefaultsStub());
        Services.AddSingleton<IScannerDiscoveryService>(new DiscoveryStub());
        Services.AddSingleton<IScanner>(new ScannerStub());
        Services.AddSingleton<IProfileServiceConfigurationService>(new AnonymousConfigurationStub());
        Services.AddSingleton<IPaperlessClient>(new PaperlessStub());
    }

    private sealed class ScannerStub : IScanner
    {
        public Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ScannerDiscoveryResult([new("airscan:test", "Test scanner")], new("airscan:test", "Test scanner"), new(["Flatbed", "ADF Simplex"], ["Color"], [300], ["A4"])));
    }

    private sealed class AuthenticatedConfigurationStub : IProfileServiceConfigurationService
    {
        public Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken=default) => Task.FromResult(new ProfileServiceConfiguration("https://profile.test", true, false, true, DateTimeOffset.UtcNow));
        public Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken=default) => Task.FromResult(new EffectivePaperlessConfiguration("https://profile.test", "stored-secret-never-rendered", PaperlessConfigurationSource.Profile, PaperlessConfigurationSource.Profile));
        public Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input,CancellationToken cancellationToken=default) => throw new NotSupportedException();
        public Task DeleteAsync(CancellationToken cancellationToken=default) => throw new NotSupportedException();
    }

    private sealed class AnonymousConfigurationStub : IProfileServiceConfigurationService
    {
        public Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken=default) => Task.FromResult(new ProfileServiceConfiguration("https://paperless.environment.test", true, true, false, DateTimeOffset.MinValue, true));
        public Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken=default) => Task.FromResult(new EffectivePaperlessConfiguration("https://paperless.environment.test", "environment-secret", PaperlessConfigurationSource.Deployment, PaperlessConfigurationSource.Deployment));
        public Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input,CancellationToken cancellationToken=default) => throw new NotSupportedException();
        public Task DeleteAsync(CancellationToken cancellationToken=default) => throw new NotSupportedException();
    }
    private sealed class HttpConfigurationStub(bool readOnly) : IProfileServiceConfigurationService
    {
        public Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken=default) => Task.FromResult(new ProfileServiceConfiguration("http://paperless.lan:8000", true, false, !readOnly, DateTimeOffset.MinValue, readOnly));
        public Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken=default) => Task.FromResult(new EffectivePaperlessConfiguration("http://paperless.lan:8000", "environment-secret", readOnly ? PaperlessConfigurationSource.Deployment : PaperlessConfigurationSource.Profile, PaperlessConfigurationSource.Deployment));
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
    private sealed class SavedScannerDiscoveryStub : IScannerDiscoveryService
    {
        private readonly SelectedScanner scanner = new(3,"Office scanner","192.0.2.10",443,"https","https://192.0.2.10/eSCL",DateTimeOffset.UtcNow,"airscan:office",["ADF"],[300]);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<SelectedScanner>>([scanner]);
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<SelectedScanner?>(scanner);
        public Task<ForgetScannerResult> ForgetAsync(long scannerId,CancellationToken cancellationToken=default)=>Task.FromResult(new ForgetScannerResult(false,scanner,"A scan is active on this scanner. Finish or cancel it, then try again.",true));
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId,CancellationToken cancellationToken)=>throw new NotSupportedException();
        public Task<SelectedScanner> SaveSaneProfileAsync(long scannerId,ScannerDevice device,ScannerCapabilities capabilities,CancellationToken cancellationToken)=>throw new NotSupportedException();
    }
    private sealed class AddScannerDiscoveryStub : IScannerDiscoveryService
    {
        private static readonly SelectedScanner Selected = new(7,"Test scanner","192.0.2.7",443,"https","https://192.0.2.7/eSCL",DateTimeOffset.UtcNow);
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken=default) => Task.FromResult(new ScannerNetworkDiscoveryResult([new("test","Test scanner","192.0.2.7",443,"https","https://192.0.2.7/eSCL")],[]));
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId,CancellationToken cancellationToken=default) => Task.FromResult(new ScannerSelectionResult(true,Selected));
        public Task<SelectedScanner> SaveSaneProfileAsync(long scannerId,ScannerDevice device,ScannerCapabilities capabilities,CancellationToken cancellationToken=default) => Task.FromResult(Selected with { SaneDeviceId=device.Identifier, Sources=capabilities.Sources, Resolutions=capabilities.Resolutions });
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<SelectedScanner?>(null);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<SelectedScanner>>([]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
    private sealed class PaperlessStub : IPaperlessClient
    {
        public Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<(PaperlessResult Result,PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken=default)=>throw new NotSupportedException();
        public Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request,IProgress<int>? progress=null,CancellationToken cancellationToken=default)=>throw new NotSupportedException();
    }
}
