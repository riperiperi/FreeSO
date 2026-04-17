using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the CommandDispatcher's JSON in / JSON out contract
/// (freesoexperiment-b9c). These tests don't touch the VM — they exercise the
/// parsing and response-serialization paths the sidecar relies on.
/// </summary>
public class CommandDispatcherTests
{
    [Fact]
    public async Task Dispatch_UnknownOp_ReturnsOkFalseWithCorrelation()
    {
        var d = new CommandDispatcher();
        // Register nothing; any op is "unknown".
        var line = """{"id":"c-1","op":"not-a-real-op","args":{}}""";

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(line, default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("response", (string)node["kind"]);
        Assert.Equal("c-1", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        Assert.Contains("unknown op", (string)node["error"]);
    }

    [Fact]
    public async Task Dispatch_HandlerThrows_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("boom", (args, ct) => throw new InvalidOperationException("nope"));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-2","op":"boom","args":{}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-2", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        Assert.Contains("InvalidOperationException", (string)node["error"]);
    }

    [Fact]
    public async Task Dispatch_OkPath_EmitsPayload()
    {
        var d = new CommandDispatcher();
        d.Register("echo", (args, ct) => Task.FromResult(
            CommandDispatcher.Response.Success(new { pong = true, x = 42 })));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync("""{"id":"c-3","op":"echo","args":{}}""", default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)));

        var node = JsonNode.Parse(captured).AsObject();
        Assert.True((bool)node["ok"]);
        Assert.NotNull(node["payload"]);
        var payload = node["payload"].AsObject();
        Assert.True((bool)payload["pong"]);
        Assert.Equal(42, (int)payload["x"]);
    }

    [Fact]
    public void SerializeResponse_ShapeIsStable()
    {
        var ok = CommandDispatcher.Response.Success(new { a = 1 });
        var json = CommandDispatcherAccessor.SerializeResponse("abc", ok);
        var node = JsonNode.Parse(json).AsObject();
        Assert.Equal("response", (string)node["kind"]);
        Assert.Equal("abc", (string)node["cmd_id"]);
        Assert.True((bool)node["ok"]);
        Assert.Equal(1, (int)node["payload"].AsObject()["a"]);
        Assert.NotNull(node["ts"]);

        var fail = CommandDispatcher.Response.Fail("oops");
        var jfail = CommandDispatcherAccessor.SerializeResponse("xyz", fail);
        var nfail = JsonNode.Parse(jfail).AsObject();
        Assert.False((bool)nfail["ok"]);
        Assert.Equal("oops", (string)nfail["error"]);
    }
}

/// <summary>
/// Invokes CommandDispatcher.SerializeResponse across the InternalsVisibleTo
/// boundary (FSO.Bot.Headless → FSO.Bot.Headless.Tests).
/// </summary>
internal static class CommandDispatcherAccessor
{
    public static string SerializeResponse(string id, CommandDispatcher.Response r)
        => (string)typeof(CommandDispatcher).GetMethod(
            "SerializeResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
          .Invoke(null, new object[] { id, r });
}

/// <summary>
/// Captures the next line the dispatcher writes via PerceptionEmitter.EmitLine
/// by temporarily redirecting the underlying stdout writer. Uses
/// ReserveStdout()'s mechanism indirectly — we set Console.Out to a
/// StringWriter and also call ReserveStdout to route EmitLine through it.
/// </summary>
internal static class PerceptionEmitterCapture
{
    public static IDisposable Capture(Action<string> onLine)
    {
        // PerceptionEmitter.EmitLine uses a private _realStdout captured at
        // ReserveStdout. The simplest test path: redirect Console.Out, then
        // call ReserveStdout (which captures Console.Out). Restore on dispose.
        var original = Console.Out;
        var sink = new LineSink(onLine);
        Console.SetOut(sink);
        ResetReservedStdout();
        PerceptionEmitter.ReserveStdout();
        return new DisposableAction(() =>
        {
            ResetReservedStdout();
            Console.SetOut(original);
        });
    }

    private static void ResetReservedStdout()
    {
        // Reset the private _realStdout so ReserveStdout will re-capture on
        // the next call. The field is private static — set back to null via
        // reflection.
        var f = typeof(PerceptionEmitter).GetField(
            "_realStdout",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        f?.SetValue(null, null);
    }

    private sealed class LineSink : System.IO.TextWriter
    {
        private readonly Action<string> _onLine;
        private readonly System.Text.StringBuilder _buf = new();
        public LineSink(Action<string> onLine) { _onLine = onLine; }
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;
        public override void Write(char value)
        {
            if (value == '\n') { Flush1(); return; }
            _buf.Append(value);
        }
        public override void Write(string value)
        {
            if (value == null) return;
            foreach (var ch in value) Write(ch);
        }
        public override void WriteLine(string value) { Write(value); Flush1(); }
        private void Flush1()
        {
            var s = _buf.ToString();
            _buf.Clear();
            if (!string.IsNullOrEmpty(s)) _onLine(s);
        }
    }

    private sealed class DisposableAction : IDisposable
    {
        private Action _a;
        public DisposableAction(Action a) { _a = a; }
        public void Dispose() { var a = _a; _a = null; a?.Invoke(); }
    }
}
