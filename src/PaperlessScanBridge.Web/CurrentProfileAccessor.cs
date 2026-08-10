using System.Security.Claims;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Web;

public sealed class CurrentProfileAccessor(IHttpContextAccessor http, IUserProfileRepository users, IOptions<ProfileOptions> options) : ICurrentProfileAccessor
{
    public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        if (options.Value.Mode == ProfileMode.Anonymous)
            return users.GetOrCreateAsync("scan-bridge", options.Value.AnonymousSubject, "Shared anonymous profile", cancellationToken);

        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException("Sign-in is required.");
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var issuer = principal.FindFirst("iss")?.Value ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid") ?? "oidc";
        if (string.IsNullOrWhiteSpace(subject)) throw new UnauthorizedAccessException("The identity provider did not provide a stable subject.");
        var displayName = principal.Identity.Name ?? principal.FindFirstValue("name") ?? "Signed-in user";
        return users.GetOrCreateAsync(issuer, subject, displayName, cancellationToken);
    }
}
