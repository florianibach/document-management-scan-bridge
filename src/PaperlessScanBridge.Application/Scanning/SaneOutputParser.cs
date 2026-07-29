using System.Globalization;
using System.Text.RegularExpressions;

namespace PaperlessScanBridge.Application.Scanning;

public static partial class SaneOutputParser
{
    public static IReadOnlyList<ScannerDevice> ParseDevices(string output) =>
        DeviceLine().Matches(output)
            .Select(match => new ScannerDevice(match.Groups["id"].Value, match.Groups["name"].Value.Trim()))
            .DistinctBy(device => device.Identifier, StringComparer.Ordinal)
            .ToArray();

    public static ScannerCapabilities ParseCapabilities(string output)
    {
        var sources = ValuesFor(output, "--source");
        var formats = ValuesFor(output, "--mode");
        var resolutions = ValuesFor(output, "--resolution")
            .Select(value => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dpi) ? dpi : 0)
            .Where(value => value > 0).Distinct().Order().ToArray();

        var maxWidth = MaximumMillimetres(output, "-x");
        var maxHeight = MaximumMillimetres(output, "-y");
        var paperSizes = KnownPaperSizes
            .Where(size => maxWidth >= size.Width && maxHeight >= size.Height)
            .Select(size => size.Name).ToArray();

        return new(sources, formats, resolutions, paperSizes);
    }

    private static IReadOnlyList<string> ValuesFor(string output, string option)
    {
        var line = output.Split('\n').FirstOrDefault(value => value.TrimStart().StartsWith(option + " ", StringComparison.Ordinal));
        if (line is null) return [];
        var values = BracketValues().Match(line).Groups["values"].Value;
        if (string.IsNullOrWhiteSpace(values)) return [];
        return values.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(value => !value.Contains("..", StringComparison.Ordinal)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static decimal MaximumMillimetres(string output, string option)
    {
        var line = output.Split('\n').FirstOrDefault(value => value.TrimStart().StartsWith(option + " ", StringComparison.Ordinal));
        if (line is null) return 0;
        var matches = Millimetres().Matches(line);
        return matches.Select(match => decimal.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture)).DefaultIfEmpty().Max();
    }

    private static readonly (string Name, decimal Width, decimal Height)[] KnownPaperSizes =
        [("A5", 148, 210), ("Letter", 215.9m, 279.4m), ("A4", 210, 297), ("Legal", 215.9m, 355.6m)];

    [GeneratedRegex("device `(?<id>[^']+)' is a (?<name>.+)$", RegexOptions.Multiline)]
    private static partial Regex DeviceLine();
    [GeneratedRegex("\\[(?<values>[^]]+)\\]")]
    private static partial Regex BracketValues();
    [GeneratedRegex("(?<value>\\d+(?:\\.\\d+)?)mm")]
    private static partial Regex Millimetres();
}
