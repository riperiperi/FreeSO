using System.Net;

namespace FSO.BrowserContent;

/// <summary>
/// Browser-oriented content store: fetches bytes over HTTP from a static content base URL.
/// Pair with a CDN / Kestrel static-file host of FreeSO Content/ + game data.
/// </summary>
public sealed class HttpContentStore : IContentStore
{
    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly bool _ownsClient;

    public HttpContentStore(string baseUrl, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));

        if (!baseUrl.EndsWith('/'))
            baseUrl += "/";

        _baseUri = new Uri(baseUrl, UriKind.Absolute);

        if (httpClient == null)
        {
            _http = new HttpClient { BaseAddress = _baseUri };
            _ownsClient = true;
        }
        else
        {
            _http = httpClient;
            _ownsClient = false;
            if (_http.BaseAddress == null)
                _http.BaseAddress = _baseUri;
        }
    }

    public Uri BaseUri => _baseUri;

    public async Task<Stream> OpenAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadAllBytesAsync(relativePath, cancellationToken).ConfigureAwait(false);
        return new MemoryStream(bytes, writable: false);
    }

    public async Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(relativePath);
        using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new FileNotFoundException($"Content not found at '{uri}'.", relativePath);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(relativePath);
        using var request = new HttpRequestMessage(HttpMethod.Head, uri);
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.MethodNotAllowed ||
            response.StatusCode == HttpStatusCode.NotImplemented)
        {
            // Some static hosts reject HEAD; fall back to a ranged GET.
            using var get = new HttpRequestMessage(HttpMethod.Get, uri);
            get.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
            using var getResponse = await _http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return getResponse.IsSuccessStatusCode || getResponse.StatusCode == HttpStatusCode.PartialContent;
        }

        return response.IsSuccessStatusCode;
    }

    public Uri BuildUri(string relativePath)
    {
        var urlPath = ContentPath.ToUrlPath(relativePath);
        return new Uri(_baseUri, urlPath);
    }

    public void Dispose()
    {
        if (_ownsClient)
            _http.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
