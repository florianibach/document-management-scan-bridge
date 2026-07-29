using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Web.Components.Pages;

namespace PaperlessScanBridge.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void ShowsInitialEmptyState()
    {
        Services.AddSingleton<IScanner>(new StubScanner(new([], null, null)));
        Assert.Contains("Noch keine Suche", Render<Home>().Markup);
    }

    [Fact]
    public async Task ShowsCapabilitiesAfterDiscovery()
    {
        var result = new ScannerDiscoveryResult([new("airscan:test", "HP Test")], new("airscan:test", "HP Test"), new(["ADF"], ["Color"], [300], ["A4"]));
        Services.AddSingleton<IScanner>(new StubScanner(result));
        var page = Render<Home>();
        await page.Find("button").ClickAsync(new());
        Assert.Contains("HP Test", page.Markup);
        Assert.Contains("300 dpi", page.Markup);
        Assert.Contains("A4", page.Markup);
    }

    [Fact]
    public async Task ShowsActionableFailure()
    {
        Services.AddSingleton<IScanner>(new StubScanner(new([], null, null, "No scanners were discovered.")));
        var page = Render<Home>();
        await page.Find("button").ClickAsync(new());
        Assert.Contains("No scanners", page.Find("[role=alert]").TextContent);
    }

    private sealed class StubScanner(ScannerDiscoveryResult result) : IScanner
    { public Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(result); }
}
