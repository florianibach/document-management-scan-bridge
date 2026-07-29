using PaperlessScanBridge.Application.Scanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace PaperlessScanBridge.UnitTests;

public sealed class ScannerDiscoveryServiceTests
{
    [Fact]
    public async Task ParsesEsclAdvertisementAndResolvesCapabilityUrl()
    {
        var service = CreateService(new Browser([new("Office", "printer.local", ["192.168.1.20"], 8080,
            new Dictionary<string, string> { ["ty"] = "HP OfficeJet", ["rs"] = "eSCL" })]));
        var result = await service.DiscoverAsync(default);
        var scanner = Assert.Single(result.Devices);
        Assert.Equal("HP OfficeJet", scanner.DisplayName);
        Assert.Equal("192.168.1.20", scanner.IpAddress);
        Assert.Equal("https://192.168.1.20:8080/eSCL", scanner.EsclUrl);
    }

    [Fact]
    public async Task DeduplicatesHttpAndHttpsAdvertisementsAndPrefersSecure()
    {
        var advertisement = new ZeroconfAdvertisement("Office", "printer.local", ["192.168.1.20"], 443,
            new Dictionary<string, string> { ["ty"] = "HP", ["rs"] = "eSCL" });
        var service = CreateService(new Browser([advertisement]));
        var result = await service.DiscoverAsync(default);
        Assert.Single(result.Devices);
        Assert.Equal("https", result.Devices[0].Protocol);
        Assert.Contains(result.Diagnostics, value => value.Contains("both HTTP and HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RejectsBrowserSuppliedUnknownSelectionAndValidationFailure()
    {
        var validator = new Validator(false);
        var service = CreateService(new Browser([new("Office", "host", ["10.0.0.2"], 80, new Dictionary<string, string>())]), validator);
        Assert.False((await service.SelectAsync("http://attacker/", default)).Succeeded);
        var discovered = Assert.Single((await service.DiscoverAsync(default)).Devices);
        var selected = await service.SelectAsync(discovered.DiscoveryId, default);
        Assert.False(selected.Succeeded);
        Assert.Equal(1, validator.Calls);
    }

    private static ScannerDiscoveryService CreateService(Browser browser, Validator? validator = null) =>
        new(browser, validator ?? new(true), new Repository(), new Writer(), new() { TimeoutSeconds = 1 }, NullLogger<ScannerDiscoveryService>.Instance);

    private sealed class Browser(IReadOnlyList<ZeroconfAdvertisement> advertisements) : IZeroconfBrowser
    {
        public Task<IReadOnlyList<ZeroconfAdvertisement>> ResolveAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ZeroconfAdvertisement>>(advertisements);
    }
    private sealed class Validator(bool succeeds) : IScannerEndpointValidator
    {
        public int Calls { get; private set; }
        public Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(new ScannerEndpointValidationResult(succeeds, succeeds ? null : "Capabilities invalid.")); }
    }
    private sealed class Repository : ISelectedScannerRepository
    {
        public Task<SelectedScanner?> GetAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(null);
        public Task<SelectedScanner> SaveAsync(DiscoveredScanner scanner, DateTimeOffset validatedAt, CancellationToken cancellationToken) =>
            Task.FromResult(new SelectedScanner(1, scanner.DisplayName, scanner.IpAddress, scanner.Port, scanner.Protocol, scanner.EsclUrl, validatedAt));
    }
    private sealed class Writer : ISaneAirscanConfigurationWriter
    { public Task WriteAsync(SelectedScanner scanner, CancellationToken cancellationToken) => Task.CompletedTask; }
}
