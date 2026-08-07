using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Web.Components.Pages;
using PaperlessScanBridge.Web.Components;
using PaperlessScanBridge.Web.Components.Layout;
using PaperlessScanBridge.Web;
using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    private readonly BunitJSModuleInterop notifications;

    public HomePageTests()
    {
        notifications = JSInterop.SetupModule("./scanNotifications.js");
        notifications.Setup<string>("getState").SetResult("disabled");
        notifications.Setup<string>("enable").SetResult("enabled");
        notifications.Setup<string>("disable").SetResult("disabled");
        notifications.SetupVoid("show", _ => true);
    }

    [Fact]
    public void ShowsBuildCommitInLayout()
    {
        Services.AddSingleton(new BuildInformation("abc1234")); Services.AddSingleton<ICurrentProfileAccessor>(new CurrentProfileStub()); Services.AddSingleton<IScanSessionAccessService>(new SessionAccessStub()); Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProfileOptions()));
        var layout = Render<MainLayout>(parameters => parameters.Add(value => value.Body, _ => { }));
        Assert.Contains("Scan", layout.Markup);
        Assert.Contains("Dokumente", layout.Markup);
        Assert.Contains("Einstellungen", layout.Markup);
        Assert.Contains("Status", layout.Markup);
        Assert.Contains("Profil:", layout.Markup);
    }

    [Fact]
    public void ShowsInitialEmptyState()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        Assert.Contains("Vorbereiten", page.Markup);
        Assert.DoesNotContain("mDNS", page.Markup);
    }

    [Fact]
    public async Task DisplaysAllDevicesAndRequiresExplicitSelection()
    {
        var devices = new[] { new DiscoveredScanner("one", "HP One", "10.0.0.1", 80, "http", "http://10.0.0.1/eSCL"),
            new DiscoveredScanner("two", "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL") };
        AddServices(new DiscoveryStub(new(devices, [])));
        var page = Render<ScannerSetup>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner im Netzwerk suchen")).ClickAsync(new());
        Assert.Contains("HP One", page.Markup); Assert.Contains("HP Two", page.Markup);
        Assert.True(page.FindAll("button").Single(button => button.TextContent.Contains("Auswählen und prüfen")).HasAttribute("disabled"));
        await page.Find("input[value=two]").ChangeAsync(new ChangeEventArgs { Value = "two" });
        await page.FindAll("button").Single(button => button.TextContent.Contains("Auswählen und prüfen")).ClickAsync(new());
        Assert.Contains("geprüft und gespeichert", page.Markup);
    }

    [Fact]
    public async Task ShowsControlledHttpFallbackAfterSelection()
    {
        var device = new DiscoveredScanner("one", "HP", "10.0.0.1", 443, "https", "https://10.0.0.1/eSCL");
        AddServices(new DiscoveryStub(new([device], []), "HTTP-eSCL-Endpunkt wird verwendet."));
        var page = Render<ScannerSetup>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner im Netzwerk suchen")).ClickAsync(new());
        await page.Find("input").ChangeAsync(new ChangeEventArgs { Value = "one" });
        await page.FindAll("button").Single(button => button.TextContent.Contains("Auswählen und prüfen")).ClickAsync(new());
        Assert.Contains("HTTP-eSCL-Endpunkt", page.Find("[role=alert]").TextContent);
    }

    [Fact]
    public async Task PreviewRequiresDeleteConfirmationAndRenumbersPages()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "page-0001.png", 0, true, null), new(Guid.NewGuid(), 2, "page-0002.png", 0, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();

        Assert.Equal(2, page.FindAll(".preview-page").Count);
        await page.FindAll("button").First(button => button.TextContent.Contains("Seite löschen")).ClickAsync(new());
        Assert.Equal(2, page.FindAll(".preview-page").Count);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Löschen bestätigen")).ClickAsync(new());

        Assert.Single(page.FindAll(".preview-page"));
        Assert.Contains("Seite 1", page.Find(".preview-page").TextContent);
    }

    [Fact]
    public async Task ReviewedPagesCanCreateAndDownloadPdf()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(), [new(Guid.NewGuid(), 1, "page-0001.png", 90, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();

        await page.FindAll("button").Single(button => button.TextContent.Contains("PDF erstellen")).ClickAsync(new());

        Assert.Contains("PDF herunterladen", page.Markup);
        Assert.Contains("/document", page.Find("a[href$='/document']").GetAttribute("href"));
    }

    [Fact]
    public async Task CompletedPdfLoadsMetadataAndUploadsExactlyOnce()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(), [new(Guid.NewGuid(), 1, "page.png", 0, true, null)]));
        var paperless = new PaperlessStub();
        AddServices(new DiscoveryStub(new([], [])), editor: editor, paperless: paperless);
        var page = Render<Home>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("PDF erstellen")).ClickAsync(new());
        Assert.DoesNotContain("Dokument scannen", page.Markup);
        Assert.DoesNotContain("Beidseitiges Dokument scannen", page.Markup);
        Assert.Contains("Zurück zur Vorschau", page.Markup);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Metadaten laden")).ClickAsync(new());
        Assert.Contains("Example GmbH", page.Markup);
        await page.Find("#paperless-title").ChangeAsync("Rechnung August");
        await page.FindAll("button").Single(button => button.TextContent.Contains("An Paperless senden")).ClickAsync(new());
        page.WaitForAssertion(() => Assert.Contains("Dokument wurde übergeben", page.Markup));
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("An Paperless senden"));
        Assert.Equal("https://paperless.example.test", page.FindAll("a").Single(link => link.TextContent.Contains("Paperless öffnen")).GetAttribute("href"));
        Assert.Equal("_blank", page.FindAll("a").Single(link => link.TextContent.Contains("Paperless öffnen")).GetAttribute("target"));
        Assert.Equal(1, paperless.UploadCalls);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Zur Startseite")).ClickAsync(new());
        Assert.Contains("Simplex-Scan starten", page.Markup);
        Assert.DoesNotContain("Dokument wurde übergeben", page.Markup);
    }

    [Fact]
    public async Task StartsSimplexScanAndReportsCompletion()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.Find("button.btn-primary.w-100.mt-4").ClickAsync(new());
        Assert.Contains("Prüfen", page.Markup);
        Assert.DoesNotContain("Simplex-Scan starten", page.Markup);
        Assert.DoesNotContain("Manuellen Duplex-Scan starten", page.Markup);
        Assert.Contains(notifications.Invocations, invocation => invocation.Identifier == "show"
            && invocation.Arguments[0]?.ToString() == "Scan abgeschlossen");
    }

    [Fact]
    public async Task InitialVisibleSourceIsForwardedWithoutAChangeEvent()
    {
        var workflow = new WorkflowStub();
        AddServices(new DiscoveryStub(new([], [])), workflow: workflow);
        var page = Render<Home>();

        var selectedSource = page.Find("#source option[selected]");
        Assert.Equal("ADF Simplex", selectedSource.GetAttribute("value"));
        Assert.Contains("Automatischer Einzug", selectedSource.TextContent);
        Assert.Equal("Color", page.Find("#color option[selected]").GetAttribute("value"));
        Assert.Equal("300", page.Find("#resolution option[selected]").GetAttribute("value"));
        await page.Find("button.btn-primary.w-100.mt-4").ClickAsync(new());

        Assert.Equal("ADF Simplex", workflow.ReceivedSettings!.Source);
        Assert.Equal(ScanColorMode.Color, workflow.ReceivedSettings.ColorMode);
        Assert.Equal(300, workflow.ReceivedSettings.ResolutionDpi);
    }

    [Fact]
    public async Task ShowsSavedScannerAndExactSaneSourcesAfterExplicitSelection()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        Assert.Contains("HP Two (10.0.0.2)", page.Find("#saved-scanner").TextContent);
        Assert.Contains("ADF Simplex", page.Markup);
        await page.Find("#saved-scanner").ChangeAsync("1");
        Assert.Contains("Automatischer Einzug (ADF Simplex)", page.Find("#source").TextContent);
    }

    [Fact]
    public void InitialRenderDoesNotRunSaneDiscovery()
    {
        var scanner = new SaneStub();
        AddServices(new DiscoveryStub(new([], [])), scanner);
        Render<Home>();
        Assert.Equal(0, scanner.Calls);
    }

    [Fact]
    public async Task RefreshButtonRunsLiveSaneDiscoveryOnDemand()
    {
        var scanner = new SaneStub();
        AddServices(new DiscoveryStub(new([], [])), scanner);
        var page = Render<Home>();
        await page.Find("button.btn-outline-secondary").ClickAsync(new());
        Assert.Equal(1, scanner.Calls);
    }

    [Fact]
    public void TimeoutDecisionOffersContinueAndAbort()
    {
        var workflow = new WorkflowStub();
        AddServices(new DiscoveryStub(new([], [])), workflow: workflow);
        var page = Render<Home>();
        workflow.SetState(ScanJobState.AwaitingUserDecision, "Scanner arbeitet möglicherweise noch.");
        page.WaitForAssertion(() => Assert.Contains("weiter warten", page.Markup));
        Assert.Contains("Scan jetzt abbrechen", page.Markup);
    }

    [Fact]
    public async Task ManualDuplexWaitsForExplicitMobileFriendlyFlipConfirmation()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.FindAll("button").Single(button => button.TextContent.Contains("Manuellen Duplex-Scan starten")).ClickAsync(new());
        Assert.Contains("Stapel jetzt wenden", page.Markup);
        Assert.Contains("Reihenfolge nicht verändern", page.Markup);
        Assert.Contains("Rückseiten scannen", page.Markup);
    }

    [Fact]
    public async Task ManualDuplexAlwaysUsesAdfAndSelectedColorSettings()
    {
        var duplex = new DuplexWorkflowStub();
        AddServices(new DiscoveryStub(new([], [])), duplex: duplex);
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.Find("#source").ChangeAsync("Flatbed");
        await page.Find("#color").ChangeAsync("Grayscale");
        await page.Find("#resolution").ChangeAsync("200");

        var start = page.FindAll("button").Single(button => button.TextContent.Contains("Manuellen Duplex-Scan starten"));
        Assert.False(start.HasAttribute("disabled"));
        await start.ClickAsync(new());

        Assert.Equal("ADF Simplex", duplex.ReceivedSettings!.Source);
        Assert.Equal(ScanColorMode.Grayscale, duplex.ReceivedSettings.ColorMode);
        Assert.Equal(200, duplex.ReceivedSettings.ResolutionDpi);
    }

    private void AddServices(DiscoveryStub discovery, SaneStub? scanner = null, WorkflowStub? workflow = null, DuplexWorkflowStub? duplex = null, PageEditorStub? editor = null, PaperlessStub? paperless = null)
    { Services.AddSingleton<IScannerDiscoveryService>(discovery); Services.AddSingleton<IScanner>(scanner ?? new SaneStub()); Services.AddSingleton<ISimplexScanWorkflow>(workflow ?? new WorkflowStub()); Services.AddSingleton<IManualDuplexWorkflow>(duplex ?? new DuplexWorkflowStub()); Services.AddSingleton<IPageEditingSession>(editor ?? new PageEditorStub()); Services.AddSingleton<IPdfCreationWorkflow>(new PdfWorkflowStub()); var client = paperless ?? new PaperlessStub(); Services.AddSingleton<IPaperlessClient>(client); Services.AddSingleton<IPaperlessUploadWorkflow>(new PaperlessUploadWorkflow(client)); Services.AddSingleton<IProfileDefaultsService>(new ProfileStub()); Services.AddSingleton<IProfileServiceConfigurationService>(new ServiceConfigurationStub()); Services.AddSingleton<ICurrentProfileAccessor>(new CurrentProfileStub()); Services.AddSingleton<IScanSessionAccessService>(new SessionAccessStub()); Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProfileOptions())); }
    private sealed class ServiceConfigurationStub : IProfileServiceConfigurationService
    {
        public Task<ProfileServiceConfiguration> GetAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EffectivePaperlessConfiguration> GetEffectiveAsync(CancellationToken cancellationToken = default) => Task.FromResult(new EffectivePaperlessConfiguration("https://paperless.example.test", "secret", PaperlessConfigurationSource.Profile, PaperlessConfigurationSource.Profile));
        public Task<ProfileServiceConfigurationResult> ValidateAndSaveAsync(ProfileServiceConfigurationInput input, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
    private sealed class CurrentProfileStub : ICurrentProfileAccessor
    {
        public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default) => Task.FromResult(new UserProfile("test-profile", "test", "subject", "Test", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    private sealed class ProfileStub : IProfileDefaultsService
    {
        private static readonly ProfileDefaults Empty = new(null,null,ScanColorMode.Color,300,null,null,null,[],DateTimeOffset.MinValue);
        public Task<ProfileDefaults> GetAsync(CancellationToken cancellationToken=default)=>Task.FromResult(Empty);
        public Task<ProfileValidation> ValidateAsync(ProfileDefaults value,CancellationToken cancellationToken=default)=>Task.FromResult(new ProfileValidation(true,[],value));
        public Task<ProfileValidation> SaveAsync(ProfileDefaults value,CancellationToken cancellationToken=default)=>Task.FromResult(new ProfileValidation(true,[],value));
        public Task ResetAsync(CancellationToken cancellationToken=default)=>Task.CompletedTask;
    }
    private sealed class DiscoveryStub(ScannerNetworkDiscoveryResult result, string? selectionDiagnostic = null) : IScannerDiscoveryService
    {
        private readonly SelectedScanner saved = new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow, "airscan:test", ["Flatbed", "ADF Simplex"], [300]);
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(saved);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SelectedScanner>>([saved]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId, CancellationToken cancellationToken) => Task.FromResult(new ScannerSelectionResult(true, saved));
        public Task<SelectedScanner> SaveSaneProfileAsync(long scannerId, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken cancellationToken) => Task.FromResult(saved);
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken) =>
            Task.FromResult(new ScannerSelectionResult(true, new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow), selectionDiagnostic));
    }
    private sealed class SaneStub : IScanner
    {
        public int Calls { get; private set; }
        public Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(new ScannerDiscoveryResult([new("airscan:test", "HP Two")], new("airscan:test", "HP Two"), new(["Flatbed", "ADF Simplex"], ["Color", "Gray"], [300], ["A4"]))); }
    }
    private sealed class WorkflowStub : ISimplexScanWorkflow
    {
        public SimplexScanSettings? ReceivedSettings { get; private set; }
        public ScanJobSnapshot? Current { get; private set; }
        public event Action? Changed;
        public Task<ScanJobSnapshot> StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default)
        { ReceivedSettings = settings; Current = new(Guid.NewGuid(), ScanJobState.Completed, 1, "Scan abgeschlossen: 1 Seite(n).", DateTimeOffset.UtcNow); Changed?.Invoke(); return Task.FromResult(Current); }
        public Task CancelAsync() => Task.CompletedTask;
        public Task ContinueAsync() => Task.CompletedTask;
        public void SetState(ScanJobState state, string message)
        { Current = new(Guid.NewGuid(), state, 0, message, DateTimeOffset.UtcNow); Changed?.Invoke(); }
    }

    private sealed class PageEditorStub(PageEditingSnapshot? initial = null) : IPageEditingSession
    {
        public PageEditingSnapshot? Current { get; private set; } = initial;
        public event Action? Changed;
        public Task LoadAsync(Guid sessionId, bool manualDuplex, CancellationToken cancellationToken = default) { Current = new(sessionId, []); Changed?.Invoke(); return Task.CompletedTask; }
        public void Rotate(Guid pageId) { }
        public void Delete(Guid pageId)
        {
            var remaining = Current!.Pages.Where(page => page.Id != pageId).Select((page, index) => page with { Number = index + 1 }).ToArray();
            Current = Current with { Pages = remaining }; Changed?.Invoke();
        }
    }
    private sealed class SessionAccessStub : IScanSessionAccessService { public Task ClaimAsync(Guid sessionId,CancellationToken cancellationToken=default)=>Task.CompletedTask; public Task<bool> CanAccessAsync(Guid sessionId,CancellationToken cancellationToken=default)=>Task.FromResult(true); }
    private sealed class DuplexWorkflowStub : IManualDuplexWorkflow
    {
        public SimplexScanSettings? ReceivedSettings { get; private set; }
        public ManualDuplexSnapshot? Current { get; private set; }
        public event Action? Changed;
        public Task StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default) { ReceivedSettings = settings; Current = new(Guid.NewGuid(), ManualDuplexState.AwaitingFlipConfirmation, 2, 0, 0, "Stapel wenden.", DateTimeOffset.UtcNow); Changed?.Invoke(); return Task.CompletedTask; }
        public Task ConfirmFlipAsync(bool finalBackIsBlank) => Task.CompletedTask;
        public Task CancelAsync() => Task.CompletedTask;
        public Task RestartAsync() { Current = null; Changed?.Invoke(); return Task.CompletedTask; }
    }
    private sealed class PdfWorkflowStub : IPdfCreationWorkflow
    {
        public PdfCreationSnapshot? Current { get; private set; }
        public event Action? Changed;
        public Task CreateAsync(PageEditingSnapshot session, CancellationToken cancellationToken = default)
        { Current = new(session.SessionId, PdfCreationState.Completed, "PDF vollständig erstellt.", "document.pdf"); Changed?.Invoke(); return Task.CompletedTask; }
        public Task CancelAsync() => Task.CompletedTask;
    }
    private sealed class PaperlessStub : IPaperlessClient
    {
        public int UploadCalls { get; private set; }
        public Task<PaperlessResult> CheckConnectivityAsync(CancellationToken cancellationToken = default) => Task.FromResult(new PaperlessResult(true, "Verbindung gültig."));
        public Task<(PaperlessResult Result, PaperlessMetadata? Metadata)> GetMetadataAsync(CancellationToken cancellationToken = default) => Task.FromResult<(PaperlessResult, PaperlessMetadata?)>((new(true, "Metadaten wurden geladen."), new([new(1, "Example GmbH")], [new(2, "Rechnung")], [new(3, "Eingang")])));
        public Task<PaperlessResult> UploadAsync(PaperlessUploadRequest request, IProgress<int>? progress = null, CancellationToken cancellationToken = default) { UploadCalls++; progress?.Report(100); return Task.FromResult(new PaperlessResult(true, "Paperless hat das Dokument angenommen.", TaskId: "task-1")); }
    }
}
