using System.ComponentModel.DataAnnotations;

namespace PaperlessScanBridge.Application.Configuration;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";
    [Required] public string Command { get; init; } = "scanimage";
    [Range(1, 600)] public int TimeoutSeconds { get; init; } = 120;
}

public sealed class PaperlessOptions
{
    public const string SectionName = "Paperless";
    [Required, Url] public string BaseUrl { get; init; } = "http://paperless:8000";
    public string? ApiToken { get; init; }
}

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";
    [Required] public string ConnectionString { get; init; } = "Data Source=data/bridge.db";
}

public sealed class TemporaryStorageOptions
{
    public const string SectionName = "TemporaryStorage";
    [Required] public string Path { get; init; } = "temp";
}
