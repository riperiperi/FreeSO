using Xunit;

namespace FSO.BrowserContent.Tests;

public class HttpContentStoreTests
{
    [Fact]
    public async Task ReadAllBytesAsync_FetchesFromLocalStaticHost()
    {
        await using var host = StaticFileHost.Start(TestPaths.ExamplesDirectory);
        using var store = new HttpContentStore(host.BaseUrl);

        var expected = await File.ReadAllBytesAsync(TestPaths.PetRockJson);
        var actual = await store.ReadAllBytesAsync("pet-rock.json");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task OpenAsync_FetchesKnownExample()
    {
        await using var host = StaticFileHost.Start(TestPaths.ExamplesDirectory);
        using var store = new HttpContentStore(host.BaseUrl);

        await using var stream = await store.OpenAsync("house-one-room.xml");
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();
        Assert.Contains("<", text);
    }

    [Fact]
    public async Task ExistsAsync_ReflectsHost()
    {
        await using var host = StaticFileHost.Start(TestPaths.ExamplesDirectory);
        using var store = new HttpContentStore(host.BaseUrl);

        Assert.True(await store.ExistsAsync("pet-rock.json"));
        Assert.False(await store.ExistsAsync("missing-asset.bin"));
    }

    [Fact]
    public async Task MissingFile_ThrowsFileNotFound()
    {
        await using var host = StaticFileHost.Start(TestPaths.ExamplesDirectory);
        using var store = new HttpContentStore(host.BaseUrl);

        await Assert.ThrowsAsync<FileNotFoundException>(() => store.ReadAllBytesAsync("nope.json"));
    }
}
