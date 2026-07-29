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

    private void AddServices(DiscoveryStub discovery)
    { Services.AddSingleton<IScannerDiscoveryService>(discovery); Services.AddSingleton<IScanner>(new SaneStub()); }
    private sealed class DiscoveryStub(ScannerNetworkDiscoveryResult result) : IScannerDiscoveryService
    {
        public Task<ScannerNetworkDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<SelectedScanner?> GetSelectedAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(null);
        public Task<ScannerSelectionResult> SelectAsync(string discoveryId, CancellationToken cancellationToken) =>
            Task.FromResult(new ScannerSelectionResult(true, new(1, "HP Two", "10.0.0.2", 443, "https", "https://10.0.0.2/eSCL", DateTimeOffset.UtcNow)));
    }
    private sealed class SaneStub : IScanner
    { public Task<ScannerDiscoveryResult> DiscoverAsync(CancellationToken cancellationToken) => Task.FromResult(new ScannerDiscoveryResult([], null, null, "Not available in test")); }
}
