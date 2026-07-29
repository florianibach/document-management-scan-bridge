using PaperlessScanBridge.Application.Scanning;
using Zeroconf;
using Microsoft.Extensions.Logging;

namespace PaperlessScanBridge.Infrastructure.Scanning;

public sealed class ZeroconfBrowser(ILogger<ZeroconfBrowser> logger) : IZeroconfBrowser
{
    public async Task<IReadOnlyList<ZeroconfAdvertisement>> ResolveAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken)
    {
        logger.LogDebug("Calling ZeroconfResolver.ResolveAsync for {ServiceType} with timeout {Timeout}", serviceType, timeout);
        var hosts = await ZeroconfResolver.ResolveAsync(serviceType, timeout, 1, 200, cancellationToken: cancellationToken);
        logger.LogDebug("ZeroconfResolver returned {HostCount} host(s) for {ServiceType}", hosts.Count, serviceType);
        return hosts.SelectMany(host => host.Services.Values.Select(service => new ZeroconfAdvertisement(
            service.Name,
            host.DisplayName,
            [host.IPAddress],
            service.Port,
            service.Properties.SelectMany(value => value).GroupBy(value => value.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase))))
            .ToArray();
    }
}
