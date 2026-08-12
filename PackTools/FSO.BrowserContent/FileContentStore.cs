namespace FSO.BrowserContent;

/// <summary>
/// Desktop content store: reads from a rooted directory via <see cref="FileStream"/>.
/// Drop-in for FreeSO paths under FreeSO.app Content/ and the TSO client directory.
/// </summary>
public sealed class FileContentStore : IContentStore
{
    private readonly string _rootFullPath;

    public FileContentStore(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
            throw new ArgumentException("Root directory is required.", nameof(rootDirectory));

        _rootFullPath = Path.GetFullPath(rootDirectory);
        if (!_rootFullPath.EndsWith(Path.DirectorySeparatorChar))
            _rootFullPath += Path.DirectorySeparatorChar;
    }

    public string RootDirectory => _rootFullPath;

    public Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = Open(relativePath);
        return Task.FromResult(stream);
    }

    public Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ReadAllBytes(relativePath));
    }

    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(Resolve(relativePath)));
    }

    /// <summary>Synchronous open for desktop call sites that today use <c>File.OpenRead</c>.</summary>
    public Stream Open(string relativePath) =>
        new FileStream(Resolve(relativePath), FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <summary>Synchronous full read for desktop call sites that today use <c>File.ReadAllBytes</c>.</summary>
    public byte[] ReadAllBytes(string relativePath) => File.ReadAllBytes(Resolve(relativePath));

    public string Resolve(string relativePath)
    {
        var relative = ContentPath.NormalizeRelative(relativePath).Replace('/', Path.DirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(_rootFullPath, relative));
        if (!full.StartsWith(_rootFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Path escapes content root: '{relativePath}'.", nameof(relativePath));
        return full;
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
