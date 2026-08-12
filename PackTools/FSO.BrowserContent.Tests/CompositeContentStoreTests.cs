using Xunit;

namespace FSO.BrowserContent.Tests;

public class CompositeContentStoreTests
{
    [Fact]
    public async Task ContentPrefix_RoutesToOverlay()
    {
        var primaryDir = Path.Combine(Path.GetTempPath(), "fso-primary-" + Guid.NewGuid().ToString("N"));
        var overlayDir = Path.Combine(Path.GetTempPath(), "fso-overlay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(primaryDir);
        Directory.CreateDirectory(overlayDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(primaryDir, "game.txt"), "from-primary");
            await File.WriteAllTextAsync(Path.Combine(overlayDir, "ui.txt"), "from-overlay");

            using var store = new CompositeContentStore(
                new FileContentStore(primaryDir),
                new FileContentStore(overlayDir));

            Assert.Equal("from-primary", System.Text.Encoding.UTF8.GetString(await store.ReadAllBytesAsync("game.txt")));
            Assert.Equal("from-overlay", System.Text.Encoding.UTF8.GetString(await store.ReadAllBytesAsync("Content/ui.txt")));
            Assert.True(await store.ExistsAsync("Content/ui.txt"));
            Assert.False(await store.ExistsAsync("Content/missing.txt"));
        }
        finally
        {
            Directory.Delete(primaryDir, recursive: true);
            Directory.Delete(overlayDir, recursive: true);
        }
    }

    [Fact]
    public void SyncOpen_ContentPrefix()
    {
        var primaryDir = Path.Combine(Path.GetTempPath(), "fso-primary-" + Guid.NewGuid().ToString("N"));
        var overlayDir = Path.Combine(Path.GetTempPath(), "fso-overlay-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(primaryDir);
        Directory.CreateDirectory(overlayDir);
        try
        {
            File.WriteAllText(Path.Combine(overlayDir, "squares.png"), "png-bytes");
            using var store = new CompositeContentStore(
                new FileContentStore(primaryDir),
                new FileContentStore(overlayDir));
            using var stream = store.Open("Content/squares.png");
            using var reader = new StreamReader(stream);
            Assert.Equal("png-bytes", reader.ReadToEnd());
        }
        finally
        {
            Directory.Delete(primaryDir, recursive: true);
            Directory.Delete(overlayDir, recursive: true);
        }
    }
}
