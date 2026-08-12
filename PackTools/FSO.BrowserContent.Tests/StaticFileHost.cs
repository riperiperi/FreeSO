using System.Net;
using System.Text;

namespace FSO.BrowserContent.Tests;

/// <summary>Minimal HttpListener static-file host for HttpContentStore tests.</summary>
internal sealed class StaticFileHost : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly Task _loop;

    private StaticFileHost(HttpListener listener, string baseUrl, CancellationTokenSource cts, Task loop)
    {
        _listener = listener;
        BaseUrl = baseUrl;
        _cts = cts;
        _loop = loop;
    }

    public string BaseUrl { get; }

    public static StaticFileHost Start(string rootDirectory)
    {
        int port;
        using (var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0))
        {
            probe.Start();
            port = ((IPEndPoint)probe.LocalEndpoint).Port;
        }

        var prefix = $"http://127.0.0.1:{port}/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var root = Path.GetFullPath(rootDirectory);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
            root += Path.DirectorySeparatorChar;

        var cts = new CancellationTokenSource();
        var loop = Task.Run(() => ServeLoop(listener, root, cts.Token));
        return new StaticFileHost(listener, prefix, cts, loop);
    }

    private static async Task ServeLoop(HttpListener listener, string root, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => Handle(ctx, root), CancellationToken.None);
        }
    }

    private static void Handle(HttpListenerContext ctx, string root)
    {
        try
        {
            var path = Uri.UnescapeDataString(ctx.Request.Url!.AbsolutePath.TrimStart('/'));
            path = path.Replace('/', Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(root, path));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            {
                ctx.Response.StatusCode = 404;
                var msg = Encoding.UTF8.GetBytes("not found");
                ctx.Response.OutputStream.Write(msg);
                ctx.Response.Close();
                return;
            }

            if (string.Equals(ctx.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentLength64 = new FileInfo(full).Length;
                ctx.Response.Close();
                return;
            }

            var bytes = File.ReadAllBytes(full);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes);
            ctx.Response.Close();
        }
        catch
        {
            try { ctx.Response.Abort(); } catch { /* ignore */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try { _listener.Stop(); } catch { /* ignore */ }
        try { _listener.Close(); } catch { /* ignore */ }
        try { await _loop.WaitAsync(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _cts.Dispose();
    }
}
