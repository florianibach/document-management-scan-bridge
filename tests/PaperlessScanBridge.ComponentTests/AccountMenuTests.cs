using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PaperlessScanBridge.Application.Profiles;
using PaperlessScanBridge.Web.Components;

namespace PaperlessScanBridge.ComponentTests;

public sealed class AccountMenuTests : BunitContext
{
    [Fact]
    public async Task AnonymousMenuExplainsSharedHouseholdProfile()
    {
        Configure(ProfileMode.Anonymous, new ClaimsPrincipal(new ClaimsIdentity()));
        var component = Render<AccountMenu>();

        var trigger = component.Find("button.account-trigger");
        Assert.Contains("anonymes Haushaltsprofil", trigger.GetAttribute("aria-label"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));

        await trigger.ClickAsync(new());

        Assert.Equal("true", trigger.GetAttribute("aria-expanded"));
        Assert.Contains("von allen Personen in diesem Haushalt gemeinsam verwendet", component.Markup);
        Assert.DoesNotContain("Abmelden", component.Markup);
    }

    [Fact]
    public async Task AuthenticatedMenuShowsSafePictureNameProviderAndSignOutOnly()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "private-subject"),
            new Claim(ClaimTypes.Name, "Ada Lovelace"),
            new Claim("iss", "https://identity.example.test/realms/home"),
            new Claim("picture", "https://images.example.test/ada.png"),
            new Claim("access_token", "private-token")
        };
        Configure(ProfileMode.OpenIdConnect, new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc", ClaimTypes.Name, ClaimTypes.Role)), "Ada Lovelace");
        var component = Render<AccountMenu>();

        Assert.Equal("https://images.example.test/ada.png", component.Find("img").GetAttribute("src"));
        await component.Find("button.account-trigger").ClickAsync(new());

        Assert.Contains("Ada Lovelace", component.Markup);
        Assert.Contains("identity.example.test", component.Markup);
        Assert.Contains("action=\"/signout\"", component.Markup);
        Assert.DoesNotContain("private-subject", component.Markup);
        Assert.DoesNotContain("private-token", component.Markup);
    }

    [Fact]
    public async Task UnsafePictureFallsBackToInitialsAndAuthenticationChangesClearSignedInState()
    {
        var provider = Configure(ProfileMode.OpenIdConnect, Authenticated("Casey Jones", "javascript:alert(1)"), "Casey Jones");
        var component = Render<AccountMenu>();

        Assert.Empty(component.FindAll("img"));
        Assert.Contains("CJ", component.Find(".account-avatar").TextContent);

        provider.SetPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));
        component.WaitForAssertion(() => Assert.Contains("Nicht angemeldet", component.Find("button.account-trigger").TextContent));
        await component.Find("button.account-trigger").ClickAsync(new());
        Assert.DoesNotContain("Abmelden", component.Markup);
    }

    private MutableAuthenticationStateProvider Configure(ProfileMode mode, ClaimsPrincipal principal, string displayName = "Shared anonymous profile")
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var provider = new MutableAuthenticationStateProvider(principal);
        Services.AddSingleton<AuthenticationStateProvider>(provider);
        Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ProfileOptions { Mode = mode }));
        Services.AddSingleton<ICurrentProfileAccessor>(new ProfileAccessorStub(displayName));
        return provider;
    }

    private static ClaimsPrincipal Authenticated(string name, string picture) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "subject"), new Claim(ClaimTypes.Name, name), new Claim("picture", picture)],
        "oidc", ClaimTypes.Name, ClaimTypes.Role));

    private sealed class MutableAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        private AuthenticationState state = new(principal);
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state);
        public void SetPrincipal(ClaimsPrincipal value)
        {
            state = new AuthenticationState(value);
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }
    }

    private sealed class ProfileAccessorStub(string displayName) : ICurrentProfileAccessor
    {
        public Task<UserProfile> GetRequiredAsync(CancellationToken cancellationToken = default) => Task.FromResult(
            new UserProfile("profile", "issuer", "subject", displayName, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }
}
