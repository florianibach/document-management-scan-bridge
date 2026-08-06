using PaperlessScanBridge.Web;
using PaperlessScanBridge.Web.Components;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Infrastructure.Persistence;
using PaperlessScanBridge.Application.Scanning;
using PaperlessScanBridge.Infrastructure.Processes;
using PaperlessScanBridge.Infrastructure.Scanning;
using Microsoft.AspNetCore.DataProtection;
using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Infrastructure.Documents;
using PaperlessScanBridge.Application.Paperless;
using PaperlessScanBridge.Infrastructure.Paperless;
using PaperlessScanBridge.Application.Profiles;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.Cookie.Name = ".PaperlessScanBridge.Antiforgery.v2");
builder.Services.AddOptions<ScannerOptions>().BindConfiguration(ScannerOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PaperlessOptions>().BindConfiguration(PaperlessOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TemporaryStorageOptions>().BindConfiguration(TemporaryStorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ScannerDiscoveryOptions>().BindConfiguration(ScannerDiscoveryOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DataProtectionStorageOptions>().BindConfiguration(DataProtectionStorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ProfileOptions>().BindConfiguration(ProfileOptions.SectionName).ValidateDataAnnotations()
    .Validate(o => o.Mode == ProfileMode.Anonymous || !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Oidc:Authority"]), "OIDC authority is required in OpenIdConnect profile mode.")
    .Validate(o => o.Mode == ProfileMode.Anonymous || !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Oidc:ClientId"]), "OIDC client ID is required in OpenIdConnect profile mode.")
    .Validate(o => o.Mode == ProfileMode.Anonymous || !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Oidc:ClientSecret"]), "OIDC client secret is required in OpenIdConnect profile mode.")
    .Validate(o => string.IsNullOrWhiteSpace(o.RemoteSignOutUrl) || Uri.TryCreate(o.RemoteSignOutUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps, "Remote sign-out URL must be an absolute HTTPS URL.")
    .ValidateOnStart();
var dataProtectionStorage = builder.Configuration.GetSection(DataProtectionStorageOptions.SectionName).Get<DataProtectionStorageOptions>() ?? new();
Directory.CreateDirectory(dataProtectionStorage.Path);

var profileOptions = builder.Configuration.GetSection(ProfileOptions.SectionName).Get<ProfileOptions>() ?? new();
if (profileOptions.Mode == ProfileMode.OpenIdConnect)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    }).AddCookie(options =>
    {
        options.Cookie.Name = ".PaperlessScanBridge.Auth.v1";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/signin";
        options.AccessDeniedPath = "/access-denied";
    }).AddOpenIdConnect(options =>
    {
        builder.Configuration.GetSection("Authentication:Oidc").Bind(options);
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-oidc";
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
    });
    builder.Services.AddAuthorization(options => options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
}
else
{
    builder.Services.AddAuthorization();
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionStorage.Path))
    .SetApplicationName("PaperlessScanBridge");
builder.Services.AddSingleton(new BuildInformation(builder.Configuration["Build:Commit"] ?? "unknown"));
builder.Services.AddSingleton<IProcessRunner, SystemProcessRunner>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScannerOptions>>().Value);
builder.Services.AddSingleton<IScanner, SaneScanner>();
builder.Services.AddSingleton<ISimplexScannerAdapter, SaneSimplexScannerAdapter>();
builder.Services.AddScoped<ISimplexScanWorkflow, SimplexScanWorkflow>();
// A workflow belongs to one interactive browser circuit. It must never leak the flip decision,
// status, or cancellation controls into another independently connected browser.
builder.Services.AddScoped<IManualDuplexWorkflow, ManualDuplexWorkflow>();
builder.Services.AddScoped<IPageEditingSession, PageEditingSession>();
builder.Services.AddScoped<IPdfCreationWorkflow, PdfCreationWorkflow>();
builder.Services.AddSingleton<IPdfDocumentWriter, PdfSharpDocumentWriter>();
builder.Services.AddScoped<IPaperlessUploadWorkflow, PaperlessUploadWorkflow>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaperlessOptions>>().Value);
builder.Services.AddHttpClient<IPaperlessClient, PaperlessClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaperlessOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TemporaryStorageOptions>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ScannerDiscoveryOptions>>().Value);
builder.Services.AddSingleton<IZeroconfBrowser, ZeroconfBrowser>();
builder.Services.AddSingleton<IScannerDiscoveryService, ScannerDiscoveryService>();
builder.Services.AddSingleton<ISelectedScannerRepository, SelectedScannerRepository>();
builder.Services.AddSingleton<IProfileDefaultsRepository, ProfileDefaultsRepository>();
builder.Services.AddSingleton<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<ICurrentProfileAccessor, CurrentProfileAccessor>();
builder.Services.AddScoped<IProfileDefaultsService, ProfileDefaultsService>();
builder.Services.AddSingleton<ISaneAirscanConfigurationWriter, SaneAirscanConfigurationWriter>();
builder.Services.AddSingleton<IScannerEndpointValidator, EsclScannerEndpointValidator>();
builder.Services.AddHttpClient("escl-validation").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
var persistence = builder.Configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new();
var databasePath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(persistence.ConnectionString).DataSource;
if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(databasePath))) Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var temporaryStorage = builder.Configuration.GetSection(TemporaryStorageOptions.SectionName).Get<TemporaryStorageOptions>() ?? new();
Directory.CreateDirectory(temporaryStorage.Path);
builder.Services.AddDbContextFactory<BridgeDbContext>(options => options.UseSqlite(persistence.ConnectionString));
builder.Services.AddHealthChecks()
    .AddCheck<DeploymentReadinessHealthCheck>("deployment_readiness", tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseForwardedHeaders();
if (profileOptions.Mode == ProfileMode.OpenIdConnect) { app.UseAuthentication(); app.UseAuthorization(); }
app.UseAntiforgery();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BridgeDbContext>>();
    await using var context = await db.CreateDbContextAsync();
    await context.Database.MigrateAsync();
    if (profileOptions.LegacyDefaultsMigration == LegacyDefaultsMigrationMode.Reset)
    {
        await context.ProfileDefaults.Where(x => x.ProfileId == "anonymous").ExecuteDeleteAsync();
    }
    var selectedRepository = scope.ServiceProvider.GetRequiredService<ISelectedScannerRepository>();
    var selected = await selectedRepository.GetAsync(CancellationToken.None);
    if (selected is not null) await scope.ServiceProvider.GetRequiredService<ISaneAirscanConfigurationWriter>().WriteAsync(selected, CancellationToken.None);
}

app.MapStaticAssets();
var components = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
if (profileOptions.Mode == ProfileMode.OpenIdConnect) components.RequireAuthorization();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGet("/signin", () => Results.Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme])).AllowAnonymous();
app.MapPost("/signout", LocalSignOutEndpoint.SignOutAsync);
app.MapGet("/signed-out", () => Results.Content("""<!doctype html><html lang="de"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Abgemeldet</title><link rel="stylesheet" href="/lib/bootstrap/dist/css/bootstrap.min.css"></head><body class="p-3"><main class="container py-4"><h1>Abgemeldet</h1><div class="alert alert-success">Du wurdest von Scan Bridge abgemeldet. Falls der Identitätsanbieter nicht erreichbar war, wurde zumindest die lokale Sitzung sicher beendet.</div><a class="btn btn-primary" href="/signin">Erneut anmelden</a></main></body></html>""", "text/html")).AllowAnonymous();
app.MapGet("/api/scanners", async (IScannerDiscoveryService discovery, CancellationToken cancellationToken) =>
    Results.Ok(await discovery.DiscoverAsync(cancellationToken)));
app.MapGet("/api/scanners/selected", async (IScannerDiscoveryService discovery, CancellationToken cancellationToken) =>
    await discovery.GetSelectedAsync(cancellationToken) is { } selected ? Results.Ok(selected) : Results.NotFound());
app.MapPost("/api/scanners/{discoveryId}/select", async (string discoveryId, IScannerDiscoveryService discovery, CancellationToken cancellationToken) =>
{
    var result = await discovery.SelectAsync(discoveryId, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Scanner) : Results.BadRequest(new { result.Diagnostic });
});
app.MapGet("/api/scan-sessions/{sessionId:guid}/pages/{fileName}", (Guid sessionId, string fileName, TemporaryStorageOptions storage) =>
{
    if (Path.GetFileName(fileName) != fileName || !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return Results.BadRequest();
    var root = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"));
    var direct = Path.Combine(root, fileName);
    var ordered = Path.Combine(root, "ordered", fileName);
    var path = File.Exists(direct) ? direct : ordered;
    return File.Exists(path) ? Results.File(path, "image/png", enableRangeProcessing: true) : Results.NotFound();
});
app.MapGet("/api/scan-sessions/{sessionId:guid}/document", (Guid sessionId, TemporaryStorageOptions storage) =>
{
    var path = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"), "document.pdf");
    return File.Exists(path) ? Results.File(path, "application/pdf", "scan.pdf", enableRangeProcessing: true) : Results.NotFound();
});

app.Run();

public sealed record BuildInformation(string Commit);
