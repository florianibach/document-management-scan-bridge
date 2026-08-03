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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery(options => options.Cookie.Name = ".PaperlessScanBridge.Antiforgery.v2");
builder.Services.AddOptions<ScannerOptions>().BindConfiguration(ScannerOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PaperlessOptions>().BindConfiguration(PaperlessOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TemporaryStorageOptions>().BindConfiguration(TemporaryStorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<ScannerDiscoveryOptions>().BindConfiguration(ScannerDiscoveryOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DataProtectionStorageOptions>().BindConfiguration(DataProtectionStorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
var dataProtectionStorage = builder.Configuration.GetSection(DataProtectionStorageOptions.SectionName).Get<DataProtectionStorageOptions>() ?? new();
Directory.CreateDirectory(dataProtectionStorage.Path);
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
builder.Services.AddSingleton<ISaneAirscanConfigurationWriter, SaneAirscanConfigurationWriter>();
builder.Services.AddSingleton<IScannerEndpointValidator, EsclScannerEndpointValidator>();
builder.Services.AddHttpClient("escl-validation").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
var persistence = builder.Configuration.GetSection(PersistenceOptions.SectionName).Get<PersistenceOptions>() ?? new();
var databasePath = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(persistence.ConnectionString).DataSource;
if (!string.IsNullOrWhiteSpace(Path.GetDirectoryName(databasePath))) Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var temporaryStorage = builder.Configuration.GetSection(TemporaryStorageOptions.SectionName).Get<TemporaryStorageOptions>() ?? new();
Directory.CreateDirectory(temporaryStorage.Path);
builder.Services.AddDbContextFactory<BridgeDbContext>(options => options.UseSqlite(persistence.ConnectionString));
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BridgeDbContext>>();
    await using var context = await db.CreateDbContextAsync();
    await context.Database.MigrateAsync();
    var selectedRepository = scope.ServiceProvider.GetRequiredService<ISelectedScannerRepository>();
    var selected = await selectedRepository.GetAsync(CancellationToken.None);
    if (selected is not null) await scope.ServiceProvider.GetRequiredService<ISaneAirscanConfigurationWriter>().WriteAsync(selected, CancellationToken.None);
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");
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
