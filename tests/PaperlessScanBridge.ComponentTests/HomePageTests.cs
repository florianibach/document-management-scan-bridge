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
        await page.Find("button").ClickAsync(new());
        Assert.Contains("HP One", page.Markup); Assert.Contains("HP Two", page.Markup);
        Assert.True(page.FindAll("button")[1].HasAttribute("disabled"));
        await page.Find("input[value=two]").ChangeAsync(new ChangeEventArgs { Value = "two" });
        await page.FindAll("button")[1].ClickAsync(new());
        Assert.Contains("geprüft und gespeichert", page.Markup);
    }

    [Fact]
    public async Task ShowsControlledHttpFallbackAfterSelection()
    {
        var device = new DiscoveredScanner("one", "HP", "10.0.0.1", 443, "https", "https://10.0.0.1/eSCL");
        AddServices(new DiscoveryStub(new([device], []), "HTTP-eSCL-Endpunkt wird verwendet."));
        var page = Render<Home>();
        await page.Find("button").ClickAsync(new());
        await page.Find("input").ChangeAsync(new ChangeEventArgs { Value = "one" });
        await page.FindAll("button")[1].ClickAsync(new());
        Assert.Contains("HTTP-eSCL-Endpunkt", page.Find("[role=alert]").TextContent);
    }

    [Fact]
    public async Task StartsSimplexScanAndReportsCompletion()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        await page.Find("button.btn-primary.w-100.mt-4").ClickAsync(new());
        Assert.Contains("Abgeschlossen", page.Find("[aria-live=polite]").TextContent);
        Assert.Contains("1 Seite", page.Markup);
    }

    [Fact]
    public void ShowsSavedScannerAndExactSaneSources()
    {
        AddServices(new DiscoveryStub(new([], [])));
        var page = Render<Home>();
        Assert.Contains("HP Two (10.0.0.2)", page.Find("#saved-scanner").TextContent);
        Assert.Contains("Automatischer Einzug (ADF Simplex)", page.Find("#source").TextContent);
    }

    private void AddServices(DiscoveryStub discovery)
    { Services.AddSingleton<IScannerDiscoveryService>(discovery); Services.AddSingleton<IScanner>(new SaneStub()); Services.AddSingleton<ISimplexScanWorkflow>(new WorkflowStub()); }
    private sealed class DiscoveryStub(ScannerNetworkDiscoveryResult result, string? selectionDiagnostic = null) : IScannerDiscoveryService
    {
        private readonly SelectedScanner saved = new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow);
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(saved);
        public Task<IReadOnlyList<SelectedScanner>> GetSavedAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SelectedScanner>>([saved]);
        public Task<ScannerSelectionResult> ActivateSavedAsync(long scannerId, CancellationToken cancellationToken) => Task.FromResult(new ScannerSelectionResult(true, saved));
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken) =>
            Task.FromResult(new ScannerSelectionResult(true, new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow), selectionDiagnostic));
    }
    private sealed class SaneStub : IScanner
    { public Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(new ScannerDiscoveryResult([new("airscan:test", "HP Two")], new("airscan:test", "HP Two"), new(["Flatbed", "ADF Simplex"], ["Color", "Gray"], [300], ["A4"]))); }
    private sealed class WorkflowStub : ISimplexScanWorkflow
    {
        public ScanJobSnapshot? Current { get; private set; }
        public event Action? Changed;
        public Task<ScanJobSnapshot> StartAsync(SimplexScanSettings settings, CancellationToken cancellationToken = default)
        { Current = new(Guid.NewGuid(), ScanJobState.Completed, 1, "Scan abgeschlossen: 1 Seite(n).", DateTimeOffset.UtcNow); Changed?.Invoke(); return Task.FromResult(Current); }
        public Task CancelAsync() => Task.CompletedTask;
    }
}
