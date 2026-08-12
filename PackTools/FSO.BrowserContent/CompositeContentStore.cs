namespace FSO.BrowserContent;

/// <summary>
/// Routes content paths across multiple roots.
/// Paths starting with <c>Content/</c> go to the overlay store (FreeSO's
/// <c>Content/</c> tree). Everything else tries the primary store (TSO client
/// directory), then falls back to the overlay.
/// </summary>
public sealed class CompositeContentStore : IContentStore
{
    private readonly IContentStore _primary;
    private readonly IContentStore? _overlay;

    public CompositeContentStore(IContentStore primary, IContentStore? overlay = null)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _overlay = overlay;
    }

    public IContentStore Primary => _primary;
    public IContentStore? Overlay => _overlay;

    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default) =>
        RouteAsync(relativePath, (s, p, ct) => s.OpenAsync(p, ct), cancellationToken);

    public Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default) =>
        RouteAsync(relativePath, (s, p, ct) => s.ReadAllBytesAsync(p, ct), cancellationToken);

    public async Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var (store, path) = Resolve(relativePath);
        if (await store.ExistsAsync(path, cancellationToken).ConfigureAwait(false))
            return true;
        // Primary miss → try same relative name in the overlay (no Content/ prefix).
        if (_overlay != null && !ReferenceEquals(store, _overlay))
            return await _overlay.ExistsAsync(path, cancellationToken).ConfigureAwait(false);
        return false;
    }

    /// <summary>Synchronous open when every child store is a <see cref="FileContentStore"/>.</summary>
    public Stream Open(string relativePath)
    {
        var (store, path) = Resolve(relativePath);
        if (store is FileContentStore files)
        {
            try { return files.Open(path); }
            catch (FileNotFoundException) when (_overlay is FileContentStore overlay && !ReferenceEquals(store, overlay))
            {
                var fallback = StripContentPrefix(ContentPath.NormalizeRelative(relativePath)) ?? path;
                return overlay.Open(fallback);
            }
        }

        return OpenAsync(relativePath).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task<T> RouteAsync<T>(
        string relativePath,
        Func<IContentStore, string, CancellationToken, Task<T>> op,
        CancellationToken cancellationToken)
    {
        var (store, path) = Resolve(relativePath);
        try
        {
            return await op(store, path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (_overlay != null && !ReferenceEquals(store, _overlay) && IsMissing(ex))
        {
            var fallback = StripContentPrefix(ContentPath.NormalizeRelative(relativePath)) ?? path;
            return await op(_overlay, fallback, cancellationToken).ConfigureAwait(false);
        }
    }

    private (IContentStore Store, string Path) Resolve(string relativePath)
    {
        var norm = ContentPath.NormalizeRelative(relativePath);
        var stripped = StripContentPrefix(norm);
        if (stripped != null)
        {
            if (_overlay == null)
                throw new FileNotFoundException($"No overlay store for path '{relativePath}'.", relativePath);
            return (_overlay, stripped);
        }
        return (_primary, norm);
    }

    private static string? StripContentPrefix(string normalized)
    {
        const string prefix = "Content/";
        if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return normalized[prefix.Length..];
        return null;
    }

    private static bool IsMissing(Exception ex) =>
        ex is FileNotFoundException
        || ex is DirectoryNotFoundException
        || (ex is HttpRequestException h && h.StatusCode == System.Net.HttpStatusCode.NotFound);

    public void Dispose()
    {
        _primary.Dispose();
        _overlay?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _primary.DisposeAsync().ConfigureAwait(false);
        if (_overlay != null)
            await _overlay.DisposeAsync().ConfigureAwait(false);
    }
}
