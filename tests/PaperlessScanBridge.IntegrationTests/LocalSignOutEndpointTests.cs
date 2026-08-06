using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Web;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class LocalSignOutEndpointTests
{
    [Fact]
    public async Task SignOutUsesProviderAfterClearingLocalCookie()
    {
        var auth = new RecordingAuthenticationService();
        var context = CreateContext(auth);

        var result = await LocalSignOutEndpoint.SignOutAsync(context,
            Options.Create(new ProfileOptions { Mode = ProfileMode.OpenIdConnect }),
            LoggerFactory.Create(_ => { }));

        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme], auth.SignedOutSchemes);
        Assert.Equal("EmptyHttpResult", result.GetType().Name);
    }


    [Fact]
    public async Task ConfiguredRemoteSignOutUrlIsPreferredOverMetadataSignOut()
    {
        var auth = new RecordingAuthenticationService { ThrowForScheme = OpenIdConnectDefaults.AuthenticationScheme };
        var context = CreateContext(auth);

        var result = await LocalSignOutEndpoint.SignOutAsync(context,
            Options.Create(new ProfileOptions
            {
                Mode = ProfileMode.OpenIdConnect,
                RemoteSignOutUrl = "https://identity.example.test/logout"
            }),
            LoggerFactory.Create(_ => { }));
        await result.ExecuteAsync(context);

        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], auth.SignedOutSchemes);
        Assert.Equal("https://identity.example.test/logout", context.Response.Headers.Location);
    }

    [Fact]
    public async Task SignOutFallsBackToLocalCompletionWhenProviderSignOutFails()
    {
        var auth = new RecordingAuthenticationService { ThrowForScheme = OpenIdConnectDefaults.AuthenticationScheme };
        var context = CreateContext(auth);

        var result = await LocalSignOutEndpoint.SignOutAsync(context,
            Options.Create(new ProfileOptions { Mode = ProfileMode.OpenIdConnect }),
            LoggerFactory.Create(_ => { }));
        await result.ExecuteAsync(context);

        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme], auth.SignedOutSchemes);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/signed-out?remote=unavailable", context.Response.Headers.Location);
    }

    [Fact]
    public async Task AnonymousModeOnlyClearsLocalCookie()
    {
        var auth = new RecordingAuthenticationService { ThrowForScheme = OpenIdConnectDefaults.AuthenticationScheme };
        var context = CreateContext(auth);

        var result = await LocalSignOutEndpoint.SignOutAsync(context,
            Options.Create(new ProfileOptions { Mode = ProfileMode.Anonymous }),
            LoggerFactory.Create(_ => { }));
        await result.ExecuteAsync(context);

        Assert.Equal([CookieAuthenticationDefaults.AuthenticationScheme], auth.SignedOutSchemes);
        Assert.Equal("/signed-out", context.Response.Headers.Location);
    }

    private static DefaultHttpContext CreateContext(IAuthenticationService auth)
    {
        var services = new ServiceCollection().AddLogging().AddSingleton(auth).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "subject")], CookieAuthenticationDefaults.AuthenticationScheme));
        return context;
    }

    private sealed class RecordingAuthenticationService : IAuthenticationService
    {
        public List<string?> SignedOutSchemes { get; } = [];
        public string? ThrowForScheme { get; init; }
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => Task.CompletedTask;
        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOutSchemes.Add(scheme);
            if (scheme == ThrowForScheme) throw new InvalidOperationException("Provider unavailable.");
            return Task.CompletedTask;
        }
    }
}
