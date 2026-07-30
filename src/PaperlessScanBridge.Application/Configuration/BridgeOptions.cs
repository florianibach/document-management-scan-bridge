using System.ComponentModel.DataAnnotations;

namespace PaperlessScanBridge.Application.Configuration;

public sealed class ScannerOptions
{
    public const string SectionName = "Scanner";
    [Required] public string Command { get; init; } = "scanimage";
    [Range(1, 600)] public int TimeoutSeconds { get; init; } = 120;
    [Range(60, 7200)] public int ScanTimeoutSeconds { get; init; } = 1800;
    [Range(300, 86400)] public int MaximumScanDurationSeconds { get; init; } = 14400;
    public string? DeviceId { get; init; }
}

public sealed class ScannerDiscoveryOptions
{
    public const string SectionName = "ScannerDiscovery";
    [Range(1, 60)] public int TimeoutSeconds { get; init; } = 5;
    [Range(1, 60)] public int ValidationTimeoutSeconds { get; init; } = 10;
    [Required] public string SaneConfigurationDirectory { get; init; } = "data/sane.d";
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

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtectionStorage";
    [Required] public string Path { get; init; } = "data/dataprotection-keys";
}
