using Microsoft.Extensions.Logging.Abstractions;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class ScannerDiscoveryServiceTests
{
    private static readonly ZeroconfAdvertisement Http = new("Office", "printer.local", ["192.168.1.20"], 80,
        new Dictionary<string, string> { ["ty"] = "HP OfficeJet", ["rs"] = "eSCL" });
    private static readonly ZeroconfAdvertisement Https = Http with { Port = 443 };

    [Fact]
    public async Task ParsesAndDeduplicatesAdvertisementsButRetainsSecurePreference()
    {
        var service = CreateService(new Browser([Http], [Https]));
        var result = await service.DiscoverAsync(default);
        var scanner = Assert.Single(result.Devices);
        Assert.Equal("HP OfficeJet", scanner.DisplayName);
        Assert.Equal("https", scanner.Protocol);
        Assert.Contains(result.Diagnostics, value => value.Contains("HTTPS will be validated first", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SuccessfulHttpsDoesNotValidateHttp()
    {
        var validator = new Validator(scanner => new(true));
        var repository = new Repository();
        var service = CreateService(new Browser([Http], [Https]), validator, repository);
        var device = Assert.Single((await service.DiscoverAsync(default)).Devices);
        var result = await service.SelectAsync(device.DiscoveryId, default);
        Assert.True(result.Succeeded);
        Assert.Equal(["https"], validator.Protocols);
        Assert.Equal("https", repository.Saved?.Protocol);
    }

    [Fact]
    public async Task CertificateFailureFallsBackToMatchingAdvertisedHttp()
    {
        var validator = new Validator(scanner => scanner.Protocol == "https"
            ? new(false, ScannerEndpointFailure.TlsCertificate, "certificate") : new(true));
        var repository = new Repository();
        var service = CreateService(new Browser([Http], [Https]), validator, repository);
        var device = Assert.Single((await service.DiscoverAsync(default)).Devices);
        var result = await service.SelectAsync(device.DiscoveryId, default);
        Assert.True(result.Succeeded);
        Assert.Equal(["https", "http"], validator.Protocols);
        Assert.Equal("http://192.168.1.20:80/eSCL", repository.Saved?.EsclUrl);
        Assert.Contains("HTTP-eSCL-Endpunkt", result.Diagnostic);
    }

    [Theory]
    [InlineData(ScannerEndpointFailure.Timeout)]
    [InlineData(ScannerEndpointFailure.InvalidCapabilities)]
    public async Task NonCertificateFailureDoesNotFallBack(ScannerEndpointFailure failure)
    {
        var validator = new Validator(_ => new(false, failure, "failed"));
        var service = CreateService(new Browser([Http], [Https]), validator);
        var device = Assert.Single((await service.DiscoverAsync(default)).Devices);
        Assert.False((await service.SelectAsync(device.DiscoveryId, default)).Succeeded);
        Assert.Equal(["https"], validator.Protocols);
    }

    [Fact]
    public async Task CertificateFailureWithoutMatchingHttpDoesNotSave()
    {
        var repository = new Repository();
        var validator = new Validator(_ => new(false, ScannerEndpointFailure.TlsCertificate, "certificate"));
        var service = CreateService(new Browser([], [Https]), validator, repository);
        var device = Assert.Single((await service.DiscoverAsync(default)).Devices);
        Assert.False((await service.SelectAsync(device.DiscoveryId, default)).Succeeded);
        Assert.Null(repository.Saved);
    }

    [Fact]
    public async Task RejectsBrowserSuppliedUrlAndDoesNotMixDifferentDevices()
    {
        var otherHttp = Http with { Addresses = ["192.168.1.21"] };
        var validator = new Validator(_ => new(false, ScannerEndpointFailure.TlsCertificate, "certificate"));
        var service = CreateService(new Browser([otherHttp], [Https]), validator);
        Assert.False((await service.SelectAsync("http://attacker/", default)).Succeeded);
        var devices = (await service.DiscoverAsync(default)).Devices;
        Assert.Equal(2, devices.Count);
        var secure = devices.Single(device => device.Protocol == "https");
        Assert.False((await service.SelectAsync(secure.DiscoveryId, default)).Succeeded);
        Assert.Equal(["https"], validator.Protocols);
    }

    private static ScannerDiscoveryService CreateService(Browser browser, Validator? validator = null, Repository? repository = null) =>
        new(browser, validator ?? new(_ => new(true)), repository ?? new(), new Writer(), new() { TimeoutSeconds = 1 }, NullLogger<ScannerDiscoveryService>.Instance);

    private sealed class Browser(IReadOnlyList<ZeroconfAdvertisement> http, IReadOnlyList<ZeroconfAdvertisement> https) : IZeroconfBrowser
    {
        public Task<IReadOnlyList<ZeroconfAdvertisement>> ResolveAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken) =>
            Task.FromResult(serviceType.StartsWith("_uscans", StringComparison.Ordinal) ? https : http);
    }
    private sealed class Validator(Func<DiscoveredScanner, ScannerEndpointValidationResult> result) : IScannerEndpointValidator
    {
        public List<string> Protocols { get; } = [];
        public Task<ScannerEndpointValidationResult> ValidateAsync(DiscoveredScanner scanner, CancellationToken cancellationToken)
        { Protocols.Add(scanner.Protocol); return Task.FromResult(result(scanner)); }
    }
    private sealed class Repository : ISelectedScannerRepository
    {
        public DiscoveredScanner? Saved { get; private set; }
        public Task<SelectedScanner?> GetAsync(CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(null);
        public Task<IReadOnlyList<SelectedScanner>> ListAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SelectedScanner>>([]);
        public Task<SelectedScanner?> GetByIdAsync(long scannerId, CancellationToken cancellationToken) => Task.FromResult<SelectedScanner?>(null);
        public Task<SelectedScanner> SaveAsync(DiscoveredScanner scanner, DateTimeOffset validatedAt, CancellationToken cancellationToken)
        { Saved = scanner; return Task.FromResult(new SelectedScanner(1, scanner.DisplayName, scanner.IpAddress, scanner.Port, scanner.Protocol, scanner.EsclUrl, validatedAt)); }
    }
    private sealed class Writer : ISaneAirscanConfigurationWriter
    { public Task WriteAsync(SelectedScanner scanner, CancellationToken cancellationToken) => Task.CompletedTask; }
}
