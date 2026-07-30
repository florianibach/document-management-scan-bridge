using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Web.Components.Pages;
using PaperlessScanBridge.Web.Components.Layout;
using PaperlessScanBridge.Web;

namespace PaperlessScanBridge.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void ShowsBuildCommitInLayout()
    {
        Services.AddSingleton(new BuildInformation("abc1234"));
        var layout = Render<MainLayout>(parameters => parameters.Add(value => value.Body, _ => { }));
        Assert.Contains("abc1234", layout.Markup);
    }

    [Fact]
    public void ShowsInitialEmptyState()
    {
        AddServices(new DiscoveryStub(new([], [])));
        Assert.Contains("Noch keine Suche", Render<Home>().Markup);
    }

    [Fact]
    public async Task DisplaysAllDevicesAndRequiresExplicitSelection()
    {
        var devices = new[] { new DiscoveredScanner("one", "HP One", "10.0.0.1", 80, "http", "http://10.0.0.1/eSCL"),
            new DiscoveredScanner("two", "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL") };
        AddServices(new DiscoveryStub(new(devices, [])));
        var page = Render<Home>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner suchen")).ClickAsync(new());
        Assert.Contains("HP One", page.Markup); Assert.Contains("HP Two", page.Markup);
        Assert.True(page.FindAll("button").Single(button => button.TextContent.Contains("Scanner auswählen") && !button.TextContent.Contains("gespeichert", StringComparison.OrdinalIgnoreCase)).HasAttribute("disabled"));
        await page.Find("input[value=two]").ChangeAsync(new ChangeEventArgs { Value = "two" });
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner auswählen")).ClickAsync(new());
        Assert.Contains("geprüft und gespeichert", page.Markup);
    }

    [Fact]
    public async Task ShowsControlledHttpFallbackAfterSelection()
    {
        var device = new DiscoveredScanner("one", "HP", "10.0.0.1", 443, "https", "https://10.0.0.1/eSCL");
        AddServices(new DiscoveryStub(new([device], []), "HTTP-eSCL-Endpunkt wird verwendet."));
        var page = Render<Home>();
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner suchen")).ClickAsync(new());
        await page.Find("input").ChangeAsync(new ChangeEventArgs { Value = "one" });
        await page.FindAll("button").Single(button => button.TextContent.Contains("Scanner auswählen")).ClickAsync(new());
        Assert.Contains("HTTP-eSCL-Endpunkt", page.Find("[role=alert]").TextContent);
    }

    [Fact]
    public async Task StartsSimplexScanAndReportsCompletion()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("#saved-scanner").ChangeAsync("1");
        await page.Find("button.btn-primary.w-100.mt-4").ClickAsync(new());
        Assert.Contains("Abgeschlossen", page.Find("[aria-live=polite]").TextContent);
        Assert.Contains("1 Seite", page.Markup);
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

    private void AddServices(DiscoveryStub discovery, SaneStub? scanner = null, WorkflowStub? workflow = null, DuplexWorkflowStub? duplex = null)
    { Services.AddSingleton<IScannerDiscoveryService>(discovery); Services.AddSingleton<IScanner>(scanner ?? new SaneStub()); Services.AddSingleton<ISimplexScanWorkflow>(workflow ?? new WorkflowStub()); Services.AddSingleton<IManualDuplexWorkflow>(duplex ?? new DuplexWorkflowStub()); }
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
        public ScanJobSnapshot? Current { get; private set; }
        public event Action? Changed;
        public Task<ScanJobSnapshot> StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default)
        { Current = new(Guid.NewGuid(), ScanJobState.Completed, 1, "Scan abgeschlossen: 1 Seite(n).", DateTimeOffset.UtcNow); Changed?.Invoke(); return Task.FromResult(Current); }
        public Task CancelAsync() => Task.CompletedTask;
        public Task ContinueAsync() => Task.CompletedTask;
        public void SetState(ScanJobState state, string message)
        { Current = new(Guid.NewGuid(), state, 0, message, DateTimeOffset.UtcNow); Changed?.Invoke(); }
    }
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
}
