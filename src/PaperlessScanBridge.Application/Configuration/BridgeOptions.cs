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
    [Required, PaperlessBaseUrl] public string BaseUrl { get; init; } = "http://paperless:8000";
    public string? ApiToken { get; init; }
    [Range(1, 300)] public int TimeoutSeconds { get; init; } = 60;
    public bool ShowHttpWarning { get; init; } = true;
}

public sealed class PaperlessBaseUrlAttribute : ValidationAttribute
{
    public PaperlessBaseUrlAttribute() => ErrorMessage = PaperlessUrlPolicy.ValidationMessage;

    public override bool IsValid(object? value) => value is string text && PaperlessUrlPolicy.TryParse(text, out _);
}

public static class PaperlessUrlPolicy
{
    public const string ValidationMessage = "The Paperless URL must be an absolute HTTP or HTTPS URL without embedded credentials.";

    public static bool TryParse(string? value, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var candidate) ||
            (candidate.Scheme != Uri.UriSchemeHttp && candidate.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(candidate.Host) ||
            !string.IsNullOrEmpty(candidate.UserInfo))
            return false;

        uri = candidate;
        return true;
    }

    public static bool IsUnencrypted(string? value) =>
        TryParse(value, out var uri) && uri!.Scheme == Uri.UriSchemeHttp;
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
