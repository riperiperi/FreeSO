namespace FSO.BrowserContent;

/// <summary>
/// Minimal seam between FreeSO content loading and the underlying byte source.
/// Desktop uses <see cref="FileContentStore"/>; browser uses <see cref="HttpContentStore"/>.
/// Paths are content-relative (e.g. "uigraphics/foo.png", "objectdata/objects/foo.iff").
/// </summary>
public interface IContentStore : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Open a readable stream for <paramref name="relativePath"/>.
    /// Caller owns the stream and must dispose it.
    /// </summary>
    Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>Read the entire file into a byte array.</summary>
    Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>True if the relative path can be opened.</summary>
    Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default);
}
