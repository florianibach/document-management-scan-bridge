using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class SaneOutputParserTests
{
    [Fact]
    public void ParsesDevicesAndIgnoresNoise()
    {
        var device = Assert.Single(SaneOutputParser.ParseDevices("noise\ndevice `airscan:e0:HP OfficeJet' is a WSD HP OfficeJet all-in-one\n"));
        Assert.Equal("airscan:e0:HP OfficeJet", device.Identifier);
        Assert.Equal("WSD HP OfficeJet all-in-one", device.DisplayName);
    }

    [Fact]
    public void ParsesCapabilitiesAndGeometry()
    {
        const string output = """
              --source [Flatbed|ADF|ADF Duplex] [Flatbed]
              --mode [Color|Gray] [Color]
              --resolution [75|150|300|600]dpi [300]
              -x 0..215.9mm [215.9]
              -y 0..355.6mm [297]
            """;
        var result = SaneOutputParser.ParseCapabilities(output);
        Assert.Equal(["Flatbed", "ADF", "ADF Duplex"], result.Sources);
        Assert.Equal(["Color", "Gray"], result.Formats);
        Assert.Equal([75, 150, 300, 600], result.Resolutions);
        Assert.Contains("A4", result.PaperSizes);
        Assert.Contains("Legal", result.PaperSizes);
    }

    [Fact]
    public void ParsesUnbracketedSaneChoiceListsInsteadOfOnlyTheDefault()
    {
        const string output = "  --source Flatbed|'ADF Simplex' [Flatbed]\n  --mode Color|Gray [Color]\n  --resolution 100|200|300dpi [300]";
        var result = SaneOutputParser.ParseCapabilities(output);
        Assert.Equal(["Flatbed", "ADF Simplex"], result.Sources);
        Assert.Equal([100, 200, 300], result.Resolutions);
    }
}
