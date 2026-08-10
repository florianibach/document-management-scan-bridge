using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Documents;
using PaperlessScanBridge.Application.Paperless;

namespace PaperlessScanBridge.Infrastructure.Documents;

public sealed class FileScanBatchStore(TemporaryStorageOptions storage) : IScanBatchStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<ScanBatchSnapshot?> LoadAsync(Guid sessionId, string profileId, CancellationToken token = default)
    {
        var path = PathFor(sessionId, profileId);
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ScanBatchSnapshot>(stream, Json, token);
    }

    public async Task SaveAsync(ScanBatchSnapshot batch, string profileId, CancellationToken token = default)
    {
        var path = PathFor(batch.SessionId, profileId); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var partial = path + ".partial";
        try
        {
            await using (var stream = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                await JsonSerializer.SerializeAsync(stream, batch, Json, token);
            File.Move(partial, path, true);
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }

    private string PathFor(Guid sessionId, string profileId)
    {
        var owner = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(profileId)))[..16];
        return Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"), $"batch-{owner}.json");
    }
}

public sealed class ScanBatchProcessor(TemporaryStorageOptions storage, IPdfDocumentWriter writer, IPaperlessClient paperless) : IScanBatchProcessor
{
    public async Task CreatePdfAsync(Guid sessionId, BatchDocument document, CancellationToken token)
    {
        var sourceRoot = Path.Combine(Path.GetFullPath(storage.Path), sessionId.ToString("N"));
        var targetRoot = Path.Combine(Path.GetFullPath(storage.Path), document.Id.ToString("N"));
        Directory.CreateDirectory(targetRoot);
        foreach (var page in document.Pages)
        {
            token.ThrowIfCancellationRequested();
            var source = ResolvePage(sourceRoot, page.FileName);
            File.Copy(source, Path.Combine(targetRoot, page.FileName), true);
        }
        await writer.WriteAsync(document.Id, document.Pages.Select(page => new PdfPageInput(page.FileName, page.RotationDegrees)).ToArray(), token);
    }

    public Task<PaperlessResult> UploadAsync(Guid sessionId, BatchDocument document, CancellationToken token) =>
        paperless.UploadAsync(new(document.Id, document.Metadata.Title, document.Metadata.CorrespondentId,
            document.Metadata.DocumentTypeId, document.Metadata.Tags), cancellationToken: token);

    private static string ResolvePage(string root, string fileName)
    {
        if (Path.GetFileName(fileName) != fileName) throw new InvalidDataException("Invalid page file name.");
        var direct = Path.Combine(root, fileName); var ordered = Path.Combine(root, "ordered", fileName);
        return File.Exists(direct) ? direct : File.Exists(ordered) ? ordered : throw new FileNotFoundException("A batch page is missing.");
    }
}
