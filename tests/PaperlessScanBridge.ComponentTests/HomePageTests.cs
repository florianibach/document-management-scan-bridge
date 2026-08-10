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
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

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
        Services.AddSingleton(new BuildInformation("abc1234")); Services.AddSingleton<ICurrentProfileAccessor>(new CurrentProfileStub()); Services.AddSingleton<IScanSessionAccessService>(new SessionAccessStub()); Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProfileOptions())); Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuthenticationStateProvider());
        var layout = Render<MainLayout>(parameters => parameters.Add(value => value.Body, _ => { }));
        Assert.Contains("Scan", layout.Markup);
        Assert.Contains("Documents", layout.Markup);
        Assert.Contains("Settings", layout.Markup);
        Assert.Contains("Status", layout.Markup);
        Assert.Contains("Open account menu for anonymous household profile", layout.Markup);
    }

    private sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    [Fact]
    public void ShowsInitialEmptyState()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        Assert.Contains("Prepare", page.Markup);
        Assert.DoesNotContain("mDNS", page.Markup);
        Assert.DoesNotContain("Täglicher Ablauf", page.Markup);
    }

    [Fact]
    public void MissingScannerShowsProminentSetupPathInsteadOfScanAction()
    {
        AddServices(new EmptyDiscoveryStub());
        var page = Render<Home>();
        Assert.Contains("No scanner selected yet", page.Markup);
        Assert.Equal("/scanner-setup", page.Find("a[href='/scanner-setup']").GetAttribute("href"));
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("Start simplex scan"));
    }

    [Fact]
    public async Task DisplaysAllDevicesAndRequiresExplicitSelection()
    {
        var devices = new[] { new DiscoveredScanner("one", "HP One", "10.0.0.1", 80, "http", "http://10.0.0.1/eSCL"),
            new DiscoveredScanner("two", "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL") };
        var discovery = new DiscoveryStub(new(devices, []));
        var scanner = new SaneStub();
        AddServices(discovery, scanner);
        var page = Render<ScannerSetup>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Search for scanners")).ClickAsync(new());
        Assert.Contains("HP One", page.Markup); Assert.Contains("HP Two", page.Markup);
        Assert.Equal(2, page.FindAll("button").Count(button => button.TextContent.Contains("Select and validate")));
        await page.FindAll("button").Last(button => button.TextContent.Contains("Select and validate")).ClickAsync(new());
        Assert.Contains("passed capability validation", page.Markup);
        Assert.Equal(1, scanner.Calls);
        Assert.Equal(1, discovery.SaveSaneProfileCalls);
    }

    [Fact]
    public async Task ShowsControlledHttpFallbackAfterSelection()
    {
        var device = new DiscoveredScanner("one", "HP", "10.0.0.1", 443, "https", "https://10.0.0.1/eSCL");
        AddServices(new DiscoveryStub(new([device], []), "HTTP eSCL endpoint is being used."));
        var page = Render<ScannerSetup>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Search for scanners")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Select and validate")).ClickAsync(new());
        Assert.Contains("HTTP eSCL endpoint", page.Markup);
    }

    [Fact]
    public async Task PreviewRequiresDeleteConfirmationAndRenumbersPages()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "page-0001.png", 0, true, null), new(Guid.NewGuid(), 2, "page-0002.png", 0, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();

        Assert.Equal(2, page.FindAll(".preview-page").Count);
        await page.FindAll("button").First(button => button.TextContent.Contains("Remove page")).ClickAsync(new());
        Assert.Equal(2, page.FindAll(".preview-page").Count);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Confirm removal")).ClickAsync(new());

        Assert.Single(page.FindAll(".preview-page"));
        Assert.Contains("Page 1", page.Find(".preview-page").TextContent);
    }

    [Fact]
    public async Task ReviewCanSplitBatchAndShowsDocumentAndPageProgress()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "page-1.png", 0, true, null), new(Guid.NewGuid(), 2, "page-2.png", 0, true, null), new(Guid.NewGuid(), 3, "page-3.png", 0, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();

        Assert.Contains("1 documents from 3 pages", page.Markup);
        await page.FindAll(".split-control").First().ClickAsync(new());

        page.WaitForAssertion(() => Assert.Contains("2 documents from 3 pages", page.Markup));
        Assert.Equal(2, page.FindAll(".batch-document").Count);
        Assert.Equal("Remove document boundary after page 1", page.Find(".split-control").GetAttribute("aria-label"));
        Assert.Contains("Continue with document 1", page.Markup);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue with document 1")).ClickAsync(new());
        Assert.Contains("Review document 1 of 2", page.Markup);
        Assert.Contains("1/2", page.Find(".workflow-stepper").TextContent);
        Assert.Contains("document-layout", page.Find(".preview-grid").ClassList);
        Assert.DoesNotContain("split-layout", page.Find(".preview-grid").ClassList);
    }

    [Fact]
    public async Task SplitDocumentsEachFollowReviewPdfAndSendBeforeAdvancing()
    {
        var paperless = new PaperlessStub();
        var editor = new PageEditorStub(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "page-1.png", 0, true, null), new(Guid.NewGuid(), 2, "page-2.png", 0, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor, paperless: paperless);
        var page = Render<Home>();
        await page.Find(".split-control").ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue with document 1")).ClickAsync(new());
        Assert.Contains("Review document 1 of 2", page.Markup);

        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue to PDF")).ClickAsync(new());
        Assert.Contains("PDF · document 1 of 2", page.Markup);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue to Send")).ClickAsync(new());
        await page.Find("button.btn-outline-primary").ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Send to Paperless")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Review next document")).ClickAsync(new());

        Assert.Contains("Review document 2 of 2", page.Markup);
        Assert.Contains("2/2", page.Find(".workflow-stepper").TextContent);
        Assert.Equal(1, paperless.UploadCalls);
    }

    [Fact]
    public async Task FinalDocumentReviewUsesRegularPageColumnsWithoutSplitSlots()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(),
            [new(Guid.NewGuid(), 1, "page-1.png", 0, true, null), new(Guid.NewGuid(), 2, "page-2.png", 0, true, null), new(Guid.NewGuid(), 3, "page-3.png", 0, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();
        await page.FindAll(".split-control")[1].ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue with document 1")).ClickAsync(new());

        Assert.Equal(2, page.FindAll(".preview-page").Count);
        Assert.Contains("document-layout", page.Find(".preview-grid").ClassList);
        Assert.Empty(page.FindAll(".split-control"));
    }

    [Fact]
    public async Task ReviewedPagesCanCreateAndDownloadPdf()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(), [new(Guid.NewGuid(), 1, "page-0001.png", 90, true, null)]));
        AddServices(new DiscoveryStub(new([], [])), editor: editor);
        var page = Render<Home>();

        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue with document")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue to PDF")).ClickAsync(new());

        Assert.Contains("Download PDF", page.Markup);
        Assert.Contains("/documents/", page.Find("a[href*='/documents/']").GetAttribute("href"));
    }

    [Fact]
    public async Task CompletedPdfLoadsMetadataAndUploadsExactlyOnce()
    {
        var editor = new PageEditorStub(new(Guid.NewGuid(), [new(Guid.NewGuid(), 1, "page.png", 0, true, null)]));
        var paperless = new PaperlessStub();
        AddServices(new DiscoveryStub(new([], [])), editor: editor, paperless: paperless);
        var page = Render<Home>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue with document")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue to PDF")).ClickAsync(new());
        await page.FindAll("button").Single(button => button.TextContent.Contains("Continue to Send")).ClickAsync(new());
        Assert.DoesNotContain("Scan a document", page.Markup);
        Assert.DoesNotContain("Beidseitiges Scan a document", page.Markup);
        Assert.Contains("Back to review", page.Markup);
        Assert.DoesNotContain("is ready with saved scanner capabilities", page.Markup);
        await page.Find("button.btn-outline-primary").ClickAsync(new());
        Assert.Contains("Example GmbH", page.Markup);
        await page.Find("input[id^='batch-title-']").ChangeAsync("August invoice");
        await page.FindAll("button").Single(button => button.TextContent.Contains("Send to Paperless")).ClickAsync(new());
        page.WaitForAssertion(() => Assert.Contains("Document submitted", page.Markup));
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Contains("Send to Paperless"));
        Assert.Equal(1, paperless.UploadCalls);
        await page.FindAll("button").Single(button => button.TextContent.Contains("Finish batch")).ClickAsync(new());
        Assert.Contains("Start simplex scan", page.Markup);
        Assert.DoesNotContain("Document submitted", page.Markup);
    }

    [Fact]
    public async Task StartsSimplexScanAndReportsCompletion()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.Find("button.btn-primary.w-100.mt-4").ClickAsync(new());
        Assert.Contains("Review", page.Markup);
        Assert.DoesNotContain("Start simplex scan", page.Markup);
        Assert.DoesNotContain("Start manual duplex scan", page.Markup);
        page.WaitForAssertion(() => Assert.Contains(notifications.Invocations, invocation => invocation.Identifier == "show"
            && invocation.Arguments[0]?.ToString() == "Scan completed"));
    }

    [Fact]
    public async Task InitialVisibleSourceIsForwardedWithoutAChangeEvent()
    {
        var workflow = new WorkflowStub();
        AddServices(new DiscoveryStub(new([], [])), workflow: workflow);
        var page = Render<Home>();

        var selectedSource = page.Find("#source option[selected]");
        Assert.Equal("ADF Simplex", selectedSource.GetAttribute("value"));
        Assert.Contains("Automatic feeder", selectedSource.TextContent);
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
        Assert.Contains("Automatic feeder (ADF Simplex)", page.Find("#source").TextContent);
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
    public async Task TimeoutDecisionOffersContinueAndAbort()
    {
        var workflow = new WorkflowStub();
        AddServices(new DiscoveryStub(new([], [])), workflow: workflow);
        var page = Render<Home>();
        workflow.SetState(ScanJobState.AwaitingUserDecision, "Scanner arbeitet möglicherweise noch.");
        page.WaitForAssertion(() => Assert.Contains("keep waiting", page.Markup));
        Assert.Contains("Cancel scan now", page.Markup);
        var previousNotifications = notifications.Invocations.Count();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Cancel scan now")).ClickAsync(new());
        page.WaitForAssertion(() => Assert.Contains(notifications.Invocations.Skip(previousNotifications), invocation => invocation.Identifier == "show" && invocation.Arguments[0]?.ToString() == "Scan cancelled"));
        Assert.DoesNotContain(notifications.Invocations.Skip(previousNotifications), invocation => invocation.Identifier == "show" && invocation.Arguments[0]?.ToString() == "Scan needs a decision");
    }

    [Fact]
    public async Task ManualDuplexWaitsForExplicitMobileFriendlyFlipConfirmation()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.FindAll("button").Single(button => button.TextContent.Contains("Start manual duplex scan")).ClickAsync(new());
        Assert.Contains("Flip the stack now", page.Markup);
        Assert.Contains("keep its order", page.Markup);
        Assert.Contains("Scan back sides", page.Markup);
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

        var start = page.FindAll("button").Single(button => button.TextContent.Contains("Start manual duplex scan"));
        Assert.False(start.HasAttribute("disabled"));
        await start.ClickAsync(new());

        Assert.Equal("ADF Simplex", duplex.ReceivedSettings!.Source);
        Assert.Equal(ScanColorMode.Grayscale, duplex.ReceivedSettings.ColorMode);
        Assert.Equal(200, duplex.ReceivedSettings.ResolutionDpi);
    }

    private void AddServices(IScannerDiscoveryService discovery, SaneStub? scanner = null, WorkflowStub? workflow = null, DuplexWorkflowStub? duplex = null, PageEditorStub? editor = null, PaperlessStub? paperless = null)
    { Services.AddSingleton<IScannerDiscoveryService>(discovery); Services.AddSingleton<IScanner>(scanner ?? new SaneStub()); Services.AddSingleton<ISimplexScanWorkflow>(workflow ?? new WorkflowStub()); Services.AddSingleton<IManualDuplexWorkflow>(duplex ?? new DuplexWorkflowStub()); Services.AddSingleton<IPageEditingSession>(editor ?? new PageEditorStub()); Services.AddSingleton<IPdfCreationWorkflow>(new PdfWorkflowStub()); var client = paperless ?? new PaperlessStub(); Services.AddSingleton<IPaperlessClient>(client); Services.AddSingleton<IPaperlessUploadWorkflow>(new PaperlessUploadWorkflow(client)); Services.AddSingleton<IScanBatchWorkflow>(new ScanBatchWorkflow(new BatchStoreStub(), new BatchProcessorStub(client))); Services.AddSingleton<IProfileDefaultsService>(new ProfileStub()); Services.AddSingleton<IProfileServiceConfigurationService>(new ServiceConfigurationStub()); Services.AddSingleton<ICurrentProfileAccessor>(new CurrentProfileStub()); Services.AddSingleton<IScanSessionAccessService>(new SessionAccessStub()); Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProfileOptions())); }
    private sealed class BatchStoreStub : IScanBatchStore
    { public Task<ScanBatchSnapshot?> LoadAsync(Guid id,string profile,CancellationToken token=default)=>Task.FromResult<ScanBatchSnapshot?>(null); public Task SaveAsync(ScanBatchSnapshot batch,string profile,CancellationToken token=default)=>Task.CompletedTask; }
    private sealed class BatchProcessorStub(IPaperlessClient client) : IScanBatchProcessor
    { public Task CreatePdfAsync(Guid session,BatchDocument document,CancellationToken token)=>Task.CompletedTask; public Task<PaperlessResult> UploadAsync(Guid session,BatchDocument document,CancellationToken token)=>client.UploadAsync(new(document.Id, document.Metadata.Title, document.Metadata.CorrespondentId, document.Metadata.DocumentTypeId, document.Metadata.Tags), cancellationToken: token); }
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
    private sealed class EmptyDiscoveryStub : IScannerDiscoveryService
    {
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken token) => Task.FromResult(new ScannerNetworkDiscoveryResult([], []));
        public Task<ScannerSelectionResult> SelectAsync(string id, CancellationToken token) => throw new NotSupportedException();
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken token) => Task.FromResult<SelectedScanner?>(null);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken token) => Task.FromResult<IReadOnlyList<SelectedScanner>>([]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long id, CancellationToken token) => throw new NotSupportedException();
        public Task<SelectedScanner> SaveSaneProfileAsync(long id, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken token) => throw new NotSupportedException();
    }
    private sealed class DiscoveryStub(ScannerNetworkDiscoveryResult result, string? selectionDiagnostic = null) : IScannerDiscoveryService
    {
        private readonly SelectedScanner saved = new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow, "airscan:test", ["Flatbed", "ADF Simplex"], [300]);
        public int SaveSaneProfileCalls { get; private set; }
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(saved);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SelectedScanner>>([saved]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId, CancellationToken cancellationToken) => Task.FromResult(new ScannerSelectionResult(true, saved));
        public Task<SelectedScanner> SaveSaneProfileAsync(long scannerId, ScannerDevice device, ScannerCapabilities capabilities, CancellationToken cancellationToken)
        { SaveSaneProfileCalls++; return Task.FromResult(saved); }
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
        { ReceivedSettings = settings; Current = new(Guid.NewGuid(), ScanJobState.Completed, 1, "Scan completed: 1 Seite(n).", DateTimeOffset.UtcNow); Changed?.Invoke(); return Task.FromResult(Current); }
        public Task CancelAsync() { if (Current is not null) { Current = Current with { State = ScanJobState.Cancelled, Message = "Scan wurde abgebrochen." }; Changed?.Invoke(); } return Task.CompletedTask; }
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
