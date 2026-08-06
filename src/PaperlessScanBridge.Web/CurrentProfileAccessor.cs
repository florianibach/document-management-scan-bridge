using System.Security.Claims;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Web;

public sealed class CurrentProfileAccessor(IHttpContextAccessor http, IUserProfileRepository users, IOptions<ProfileOptions> options) : ICurrentProfileAccessor
{
    public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        if (options.Value.Mode == ProfileMode.Anonymous)
            return users.GetOrCreateAsync("scan-bridge", options.Value.AnonymousSubject, "Anonymes gemeinsames Profil", cancellationToken);

        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException("Eine Anmeldung ist erforderlich.");
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var issuer = principal.FindFirst("iss")?.Value ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid") ?? "oidc";
        if (string.IsNullOrWhiteSpace(subject)) throw new UnauthorizedAccessException("Der Identitätsanbieter hat kein stabiles Subject geliefert.");
        var displayName = principal.Identity.Name ?? principal.FindFirstValue("name") ?? "Angemeldeter Benutzer";
        return users.GetOrCreateAsync(issuer, subject, displayName, cancellationToken);
    }
}
