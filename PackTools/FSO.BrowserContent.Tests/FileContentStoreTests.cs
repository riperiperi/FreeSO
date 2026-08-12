using Xunit;

namespace FSO.BrowserContent.Tests;

public class FileContentStoreTests
{
    [Fact]
    public async Task ReadAllBytesAsync_ReadsKnownExample()
    {
        var expected = await File.ReadAllBytesAsync(TestPaths.PetRockJson);
        using var store = new FileContentStore(TestPaths.ExamplesDirectory);

        var actual = await store.ReadAllBytesAsync("pet-rock.json");

        Assert.Equal(expected, actual);
        Assert.Contains("\"name\"", System.Text.Encoding.UTF8.GetString(actual));
    }

    [Fact]
    public async Task OpenAsync_ReturnsReadableStream()
    {
        using var store = new FileContentStore(TestPaths.ExamplesDirectory);
        await using var stream = await store.OpenAsync("house-one-room.xml");
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        Assert.Contains("<", text);
        Assert.True(text.Length > 0);
    }

    [Fact]
    public async Task ExistsAsync_TrueForPresent_FalseForMissing()
    {
        using var store = new FileContentStore(TestPaths.ExamplesDirectory);

        Assert.True(await store.ExistsAsync("pet-rock.json"));
        Assert.False(await store.ExistsAsync("does-not-exist.json"));
    }

    [Fact]
    public void SyncOpen_MatchesFileOpenRead()
    {
        using var store = new FileContentStore(TestPaths.ExamplesDirectory);
        using var fromStore = store.Open("pet-rock.json");
        using var fromDisk = File.OpenRead(TestPaths.PetRockJson);

        var a = new byte[fromStore.Length];
        var b = new byte[fromDisk.Length];
        fromStore.ReadExactly(a);
        fromDisk.ReadExactly(b);
        Assert.Equal(b, a);
    }

    [Fact]
    public void RejectsPathTraversal()
    {
        using var store = new FileContentStore(TestPaths.ExamplesDirectory);
        Assert.Throws<ArgumentException>(() => store.Resolve("../CLAUDE.md"));
    }
}
