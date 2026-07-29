using PaperlessScanBridge.Application.Scanning;
using Zeroconf;

namespace PaperlessScanBridge.Infrastructure.Scanning;

public sealed class ZeroconfBrowser : IZeroconfBrowser
{
    public async Task<IReadOnlyList<ZeroconfAdvertisement>> ResolveAsync(string serviceType, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var hosts = await ZeroconfResolver.ResolveAsync(serviceType, timeout, 1, 200, cancellationToken: cancellationToken);
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
