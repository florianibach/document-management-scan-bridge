using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Profiles;

namespace PaperlessScanBridge.Web;

public static class LocalSignOutEndpoint
{
    public static async Task<IResult> SignOutAsync(
        HttpContext context,
        IOptions<ProfileOptions> options,
        ILoggerFactory loggerFactory)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (options.Value.Mode != ProfileMode.OpenIdConnect)
        {
            return Results.Redirect("/signed-out");
        }

        if (!string.IsNullOrWhiteSpace(options.Value.RemoteSignOutUrl))
        {
            return Results.Redirect(options.Value.RemoteSignOutUrl);
        }

        try
        {
            await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = "/signed-out" });
            return Results.Empty;
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            loggerFactory.CreateLogger("PaperlessScanBridge.Authentication")
                .LogWarning(exception, "Remote OpenID Connect sign-out failed after clearing the local authentication cookie; continuing with local sign-out.");
            return Results.Redirect("/signed-out?remote=unavailable");
        }
    }
}
