using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSO.Bot.Headless;

/// <summary>
/// NDJSON writer for perception ticks + dialog events. stdout is newline-delimited JSON ONLY;
/// any logging goes to stderr (see <see cref="Program.Log"/>).
///
/// Serialisation uses snake_case property naming to match the perception schema documented in
/// <c>docs/design/embodiment-bridge.md §5</c>.
/// </summary>
public static class PerceptionEmitter
{
    // Captured at ReserveStdout() time, before Console.Out is redirected to stderr to silence
    // library Console.WriteLine chatter (upstream FSO VM driver writes diagnostics via
    // System.Console.WriteLine; we don't want to pollute the NDJSON stream).
    private static TextWriter _realStdout;
    private static readonly object _stdoutLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        // Preserve dictionary keys (already snake_case).
        DictionaryKeyPolicy = null,
    };

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
        DictionaryKeyPolicy = null,
    };

    public static string Serialize(PerceptionTick tick) =>
        JsonSerializer.Serialize(tick, JsonOptions);

    public static string SerializePretty(PerceptionTick tick) =>
        JsonSerializer.Serialize(tick, PrettyOptions);

    public static string SerializeEvent(PerceptionEvent evt, string kindOverride = null)
    {
        // Wrap standalone events as {"kind": "dialog", ...} at the top level.
        var doc = new Dictionary<string, object>
        {
            ["kind"] = kindOverride ?? evt.Kind ?? "event",
            ["t"] = evt.T,
        };
        if (!string.IsNullOrEmpty(evt.Text)) doc["text"] = evt.Text;
        if (evt.Extras != null)
        {
            foreach (var kv in evt.Extras) doc[kv.Key] = kv.Value;
        }
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    /// <summary>
    /// Capture the real stdout and redirect Console.Out to stderr. Call once at process start
    /// before any FSO library code runs. After this, any library that does Console.WriteLine
    /// lands on stderr; only EmitLine() reaches real stdout.
    /// </summary>
    public static void ReserveStdout()
    {
        if (_realStdout != null) return;
        _realStdout = Console.Out;
        // Point Console.Out at stderr (with autoflush) so stray Console.WriteLine from FSO
        // libraries doesn't land on the NDJSON stream.
        var err = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
        Console.SetOut(err);
    }

    /// <summary>Write one NDJSON line to the captured real stdout. Thread-safe.</summary>
    public static void EmitLine(string json)
    {
        lock (_stdoutLock)
        {
            var w = _realStdout ?? Console.Out;
            w.WriteLine(json);
            w.Flush();
        }
    }
}
