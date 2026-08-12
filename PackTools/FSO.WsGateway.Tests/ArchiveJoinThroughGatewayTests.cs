using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using FSO.BrowserAries;
using Xunit;

namespace FSO.WsGateway.Tests;

public class ArchiveJoinThroughGatewayTests
{
    [Fact]
    public async Task BrowserAries_Join_ReachesLotJoined()
    {
        if (!HasPython()) return;

        var cityPort = FreePort();
        var lotPort = FreePort();
        using var cityProc = StartPython("tools/fake-city-server.py", cityPort);
        using var lotProc = StartPython("tools/fake-lot-server.py", lotPort);
        await Task.Delay(500);

        var gateway = new Gateway(new Dictionary<string, (string, int)>
        {
            ["/city"] = ("127.0.0.1", cityPort),
            ["/lot"] = ("127.0.0.1", lotPort),
        });
        await gateway.Start("http://127.0.0.1:0");
        try
        {
            await using var demo = new ArchiveJoinDemo(gateway.Address);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            await demo.RunAsync(cts.Token);

            Assert.Equal(JoinStage.LotJoined, demo.Stage);
            Assert.Equal("127.0.0.1:34101", demo.LotAddress);
            Assert.Contains("Kat", demo.ServerName ?? "");
        }
        finally
        {
            await gateway.Stop();
            TryKill(cityProc);
            TryKill(lotProc);
        }
    }

    static bool HasPython()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(3000);
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    static Process StartPython(string relativeScript, int port)
    {
        // bin/Debug/net9.0 → … → PackTools
        var packTools = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var gatewayDir = Path.Combine(packTools, "FSO.WsGateway");
        var script = Path.Combine(gatewayDir, relativeScript);
        Assert.True(File.Exists(script), "missing " + script);
        return Process.Start(new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{script}\" {port}",
            WorkingDirectory = gatewayDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
    }

    static void TryKill(Process p)
    {
        try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { /* ignore */ }
        p.Dispose();
    }
}
