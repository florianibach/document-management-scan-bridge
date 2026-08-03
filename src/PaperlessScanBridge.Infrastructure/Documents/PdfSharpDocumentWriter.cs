using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Documents;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PaperlessScanBridge.Infrastructure.Documents;

public sealed class PdfSharpDocumentWriter(TemporaryStorageOptions storage) : IPdfDocumentWriter
{
    private const double DefaultDpi = 300;

    public async Task<string> WriteAsync(Guid sessionId, IReadOnlyList<PdfPageInput> pages, CancellationToken cancellationToken)
    {
        if (pages.Count == 0) throw new ArgumentException("A PDF requires at least one page.", nameof(pages));
        var sessionRoot = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"));
        if (!Directory.Exists(sessionRoot)) throw new DirectoryNotFoundException("The scan session is no longer available.");
        var output = Path.Combine(sessionRoot, "document.pdf");
        var partial = output + ".partial";

        try
        {
            await Task.Run(() => BuildDocument(sessionRoot, pages, partial, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partial, output, overwrite: true);
            return output;
        }
        finally
        {
            if (File.Exists(partial)) File.Delete(partial);
        }
    }

    private static void BuildDocument(string sessionRoot, IReadOnlyList<PdfPageInput> pages, string partial, CancellationToken token)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Scanned document";
        foreach (var input in pages)
        {
            token.ThrowIfCancellationRequested();
            if (Path.GetFileName(input.FileName) != input.FileName || !input.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A session page has an unsupported file name or format.");
            if (input.RotationDegrees is not (0 or 90 or 180 or 270))
                throw new InvalidDataException("A session page has an unsupported rotation.");

            var direct = Path.Combine(sessionRoot, input.FileName);
            var ordered = Path.Combine(sessionRoot, "ordered", input.FileName);
            var source = File.Exists(direct) ? direct : ordered;
            if (!File.Exists(source)) throw new FileNotFoundException("A reviewed session page is missing.");

            using var image = XImage.FromFile(source);
            var page = document.AddPage();
            var width = image.PixelWidth * 72d / DefaultDpi;
            var height = image.PixelHeight * 72d / DefaultDpi;
            var quarterTurn = input.RotationDegrees is 90 or 270;
            page.Width = XUnit.FromPoint(quarterTurn ? height : width);
            page.Height = XUnit.FromPoint(quarterTurn ? width : height);
            using var graphics = XGraphics.FromPdfPage(page);
            graphics.TranslateTransform(page.Width.Point / 2, page.Height.Point / 2);
            graphics.RotateTransform(input.RotationDegrees);
            graphics.DrawImage(image, -width / 2, -height / 2, width, height);
        }
        document.Save(partial);
    }
}
