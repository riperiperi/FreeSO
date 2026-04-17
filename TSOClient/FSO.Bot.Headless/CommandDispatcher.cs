using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Bot.Headless;

/// <summary>
/// Bot-side inbound command channel (freesoexperiment-b9c).
///
/// Design decision (§4.1 of docs/design/embodiment-bridge.md, locked by this item — load-bearing
/// for 13 follow-on verb-family items):
///
/// <para>
/// Transport is newline-delimited JSON over the bot's <b>stdin</b> (commands sidecar→bot) and
/// <b>stdout</b> (events bot→sidecar, including command responses). Symmetric with the perception
/// stream already flowing on stdout; the sidecar owns both pipes as the parent process.
/// </para>
///
/// <para>
/// Why stdin (not a named pipe or loopback TCP):
/// <list type="bullet">
///   <item>The sidecar already spawns the bot and owns its stdin/stdout. No extra port binding,
///   no filesystem cleanup, no reconnect logic across a crash/respawn. The subprocess lifecycle
///   is the channel lifecycle — if either side dies, the channel closes cleanly.</item>
///   <item>Credentials never pass through the channel; stdin is private to the parent by OS
///   guarantee — no auth layer needed.</item>
///   <item>Debuggable with <c>tee</c> / <c>cat</c>; no packet analyzer.</item>
/// </list>
/// </para>
///
/// <para>
/// Message shape (inbound, sidecar→bot):
/// <code>{"id":"&lt;cmd-uuid&gt;","op":"walk-to","args":{...},"expect_response":true}</code>
/// </para>
///
/// <para>
/// Message shape (outbound, bot→sidecar — merged with the existing perception/dialog/system streams):
/// <code>{"kind":"response","ts":&lt;epoch-ms&gt;,"cmd_id":"&lt;cmd-uuid&gt;","ok":true,"payload":{...}}</code>
/// <code>{"kind":"response","ts":&lt;epoch-ms&gt;,"cmd_id":"&lt;cmd-uuid&gt;","ok":false,"error":"..."}</code>
/// </para>
///
/// <para>
/// Handler registration is open (<see cref="Register"/>); each verb-family item adds new op
/// handlers here. The I/O loop and response serialization do not change.
/// </para>
///
/// <para>
/// Error model: a malformed line, unknown op, or a handler that throws emits
/// <c>{"ok":false,"error":"..."}</c> correlated on <c>cmd_id</c>. The bot never crashes on bad
/// input. If <c>id</c> is missing on a malformed line, we log to stderr and drop.
/// </para>
/// </summary>
public sealed class CommandDispatcher
{
    public delegate Task<Response> HandlerFn(JsonObject args, CancellationToken ct);

    public sealed class Response
    {
        public bool Ok;
        public object Payload; // serialized via JsonSerializer
        public string Error;

        public static Response Success(object payload = null) => new Response { Ok = true, Payload = payload };
        public static Response Fail(string error) => new Response { Ok = false, Error = error ?? "unknown" };
    }

    private readonly Dictionary<string, HandlerFn> _handlers = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _cts = new();
    private Task _loop;

    public void Register(string op, HandlerFn handler)
    {
        if (string.IsNullOrEmpty(op)) throw new ArgumentException("op required");
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        _handlers[op] = handler;
    }

    public bool Has(string op) => _handlers.ContainsKey(op);

    /// <summary>
    /// Start the stdin reader loop. Each inbound line is parsed, dispatched on a pooled task,
    /// and the response is written to stdout via <see cref="PerceptionEmitter.EmitLine"/> so the
    /// response interleaves cleanly with perception/dialog events (one NDJSON line at a time,
    /// serialized by the emitter's internal lock).
    /// </summary>
    public void Start(TextReader stdin = null)
    {
        stdin ??= Console.In;
        _loop = Task.Run(() => ReadLoop(stdin, _cts.Token));
    }

    public void Stop()
    {
        try { _cts.Cancel(); } catch { }
    }

    private async Task ReadLoop(TextReader stdin, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            string line;
            try
            {
                line = await stdin.ReadLineAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Program.Log($"[ipc] stdin read threw: {ex.GetType().Name}: {ex.Message}");
                break;
            }
            if (line == null) break; // EOF — sidecar closed stdin
            line = line.Trim();
            if (line.Length == 0) continue;

            _ = Task.Run(() => HandleLineAsync(line, ct), ct);
        }
        Program.Log("[ipc] read loop exited");
    }

    internal async Task HandleLineAsync(string line, CancellationToken ct)
    {
        string id = null;
        string op = null;
        try
        {
            var node = JsonNode.Parse(line) as JsonObject;
            if (node == null)
            {
                Program.Log("[ipc] non-object line dropped");
                return;
            }
            id = (string)node["id"];
            op = (string)node["op"];
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(op))
            {
                Program.Log($"[ipc] line missing id/op (dropped): {Truncate(line)}");
                return;
            }
            var args = node["args"] as JsonObject ?? new JsonObject();

            if (!_handlers.TryGetValue(op, out var handler))
            {
                EmitResponse(id, Response.Fail($"unknown op: {op}"));
                return;
            }

            Response resp;
            try
            {
                resp = await handler(args, ct);
            }
            catch (Exception ex)
            {
                Program.Log($"[ipc] handler {op} threw: {ex.GetType().Name}: {ex.Message}");
                resp = Response.Fail($"{ex.GetType().Name}: {ex.Message}");
            }
            EmitResponse(id, resp ?? Response.Fail("handler returned null"));
        }
        catch (JsonException ex)
        {
            Program.Log($"[ipc] json parse failed on {Truncate(line)}: {ex.Message}");
            if (id != null) EmitResponse(id, Response.Fail($"bad json: {ex.Message}"));
        }
        catch (Exception ex)
        {
            Program.Log($"[ipc] dispatch {op ?? "?"} unexpected: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string Truncate(string s) => s == null ? "" : (s.Length > 200 ? s[..200] + "…" : s);

    internal static string SerializeResponse(string id, Response resp)
    {
        var obj = new JsonObject
        {
            ["kind"] = "response",
            ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["cmd_id"] = id,
            ["ok"] = resp.Ok,
        };
        if (resp.Ok)
        {
            if (resp.Payload != null)
            {
                var json = JsonSerializer.Serialize(resp.Payload);
                obj["payload"] = JsonNode.Parse(json);
            }
        }
        else
        {
            obj["error"] = resp.Error ?? "unknown";
        }
        return obj.ToJsonString();
    }

    private void EmitResponse(string id, Response resp)
    {
        try
        {
            PerceptionEmitter.EmitLine(SerializeResponse(id, resp));
        }
        catch (Exception ex)
        {
            Program.Log($"[ipc] emit response {id} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Built-in smoke handler — returns <c>{"pong":true}</c>. Used by the infra integration test
    /// to validate the I/O plumbing end-to-end without any VM-command semantics.
    /// </summary>
    public static HandlerFn Ping => (args, ct) =>
        Task.FromResult(Response.Success(new { pong = true }));
}
