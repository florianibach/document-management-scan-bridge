using PaperlessScanBridge.Web.Components;
using Microsoft.EntityFrameworkCore;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddOptions<ScannerOptions>().BindConfiguration(ScannerOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PaperlessOptions>().BindConfiguration(PaperlessOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<PersistenceOptions>().BindConfiguration(PersistenceOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TemporaryStorageOptions>().BindConfiguration(TemporaryStorageOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
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
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");

app.Run();
