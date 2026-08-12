namespace FSO.BrowserContent;

/// <summary>Shared path normalization / traversal guards for content stores.</summary>
internal static class ContentPath
{
    public static string NormalizeRelative(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path is required.", nameof(relativePath));

        var normalized = relativePath.Replace('\\', '/').Trim();
        while (normalized.StartsWith("./", StringComparison.Ordinal))
            normalized = normalized[2..];
        if (normalized.StartsWith('/'))
            normalized = normalized.TrimStart('/');

        if (normalized.Length == 0)
            throw new ArgumentException("Relative path is empty after normalization.", nameof(relativePath));

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(p => p == ".." || p == "."))
            throw new ArgumentException($"Path traversal is not allowed: '{relativePath}'.", nameof(relativePath));

        return string.Join('/', parts);
    }

    public static string ToUrlPath(string relativePath) =>
        string.Join('/', NormalizeRelative(relativePath).Split('/').Select(Uri.EscapeDataString));
}
