using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Infrastructure.Documents;
using PdfSharp.Pdf.IO;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class PdfSharpDocumentWriterTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"pdf-writer-{Guid.NewGuid():N}");
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAABCAIAAAB7QOjdAAAAD0lEQVR4nGP4z8DA8J8BAAf/Af8Bf4mnAAAAAElFTkSuQmCC");

    [Fact]
    public async Task CreatesValidOrderedRotatedPdfAndReplacesItAtomically()
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0001.png"), Png);
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0002.png"), Png);
        var writer = new PdfSharpDocumentWriter(new TemporaryStorageOptions { Path = root });

        var path = await writer.WriteAsync(id,
            [new("page-0002.png", 90), new("page-0001.png", 0)], CancellationToken.None);

        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        Assert.Equal(2, document.PageCount);
        Assert.True(document.Pages[0].Height.Point > document.Pages[0].Width.Point);
        Assert.True(document.Pages[1].Width.Point > document.Pages[1].Height.Point);
        Assert.False(File.Exists(path + ".partial"));

        var replacement = await writer.WriteAsync(id, [new("page-0001.png", 180)], CancellationToken.None);
        using var replaced = PdfReader.Open(replacement, PdfDocumentOpenMode.Import);
        Assert.Equal(1, replaced.PageCount);
    }

    [Fact]
    public async Task CorruptInputRemovesPartialAndRetainsSessionPages()
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        var source = Path.Combine(directory, "page-0001.png");
        await File.WriteAllTextAsync(source, "not png");
        var writer = new PdfSharpDocumentWriter(new TemporaryStorageOptions { Path = root });

        await Assert.ThrowsAnyAsync<Exception>(() => writer.WriteAsync(id, [new("page-0001.png", 0)], CancellationToken.None));

        Assert.True(File.Exists(source));
        Assert.False(File.Exists(Path.Combine(directory, "document.pdf.partial")));
        Assert.False(File.Exists(Path.Combine(directory, "document.pdf")));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
