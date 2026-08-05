using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PaperlessScanBridge.Web;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class LocalSignOutEndpointTests
{
    [Fact]
    public async Task SignOutClearsOnlyLocalCookieSchemeWithoutContactingOpenIdProvider()
    {
        var auth = new RecordingAuthenticationService();
        var services = new ServiceCollection().AddLogging().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject")], CookieAuthenticationDefaults.AuthenticationScheme));

        var result = await LocalSignOutEndpoint.SignOutAsync(context);
        await result.ExecuteAsync(context);

        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], auth.SignedOutSchemes);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/signed-out", context.Response.Headers.Location);
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public List<string?> SignedOutSchemes { get; } = [];
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) { SignedOutSchemes.Add(scheme); return Task.CompletedTask; }
    }
}
