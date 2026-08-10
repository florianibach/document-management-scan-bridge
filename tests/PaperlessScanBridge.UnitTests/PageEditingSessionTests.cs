using PaperlessScanBridge.Application.Configuration;
using PaperlessScanBridge.Application.Scanning;

namespace PaperlessScanBridge.UnitTests;

public sealed class PageEditingSessionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RotationAndDeletionAreSessionOnlyAndRenumberRemainingPages()
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0001.png"), Png());
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0002.png"), Png());
        var editor = new PageEditingSession(new TemporaryStorageOptions { Path = root });

        await editor.LoadAsync(id, false);
        var first = editor.Current!.Pages[0];
        editor.Rotate(first.Id);
        editor.Delete(first.Id);

        Assert.Single(editor.Current.Pages);
        Assert.Equal(1, editor.Current.Pages[0].Number);
        Assert.True(File.Exists(Path.Combine(directory, "page-0001.png")));
    }

    [Fact]
    public async Task CorruptPageIsRecoverableAndDoesNotHideValidPage()
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "page-0001.png"), "broken");
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0002.png"), Png());
        var editor = new PageEditingSession(new TemporaryStorageOptions { Path = root });

        await editor.LoadAsync(id, false);

        Assert.False(editor.Current!.Pages[0].IsAvailable);
        Assert.True(editor.Current.Pages[1].IsAvailable);
    }

    [Fact]
    public async Task ReloadKeepsStablePageIdentityForPersistedBoundaries()
    {
        var id = Guid.NewGuid();
        var directory = Path.Combine(root, id.ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "page-0001.png"), Png());
        var editor = new PageEditingSession(new TemporaryStorageOptions { Path = root });
        await editor.LoadAsync(id, false);
        var pageId = editor.Current!.Pages.Single().Id;

        await editor.LoadAsync(id, false);

        Assert.Equal(pageId, editor.Current!.Pages.Single().Id);
    }

    private static byte[] Png() => [137, 80, 78, 71, 13, 10, 26, 10, 1];
    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
