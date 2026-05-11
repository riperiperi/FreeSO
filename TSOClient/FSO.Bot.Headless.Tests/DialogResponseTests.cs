/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FSO.Bot.Headless;
using Xunit;

namespace FSO.Bot.Headless.Tests;

/// <summary>
/// Unit tests for the dialog-family IPC handler (freesoexperiment-849):
/// <c>respond-to-dialog</c>.
///
/// <para>
/// These tests do NOT require a live VM or FSO server. They exercise:
/// <list type="bullet">
///   <item>Argument validation (missing/invalid args → ok=false error shapes).</item>
///   <item>Handler registration via <see cref="DialogHandlers.RegisterAll"/>.</item>
///   <item>CommandDispatcher integration (HandleLineAsync → ok/fail shape).</item>
///   <item>Class identity: VMNetDialogResponseCmd exists as the wire PDU.</item>
/// </list>
/// Live VM + wire-effect tests live in <c>tests/integration/verb-dialog.sh</c>.
/// </para>
/// </summary>
public class DialogResponseTests
{
    // ---- Registration ----

    [Fact]
    public void RegisterAll_NullArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DialogHandlers.RegisterAll(null, null));
    }

    [Fact]
    public void RegisterAll_NullVMHost_Throws()
    {
        var d = new CommandDispatcher();
        Assert.Throws<ArgumentNullException>(() => DialogHandlers.RegisterAll(d, null));
    }

    // ---- Argument validation via CommandDispatcher ----

    [Fact]
    public async Task Dispatcher_MissingDialogId_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("respond-to-dialog", (args, ct) =>
            Task.FromResult(DialogHandlers.RespondToDialog(null, args)));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(
            """{"id":"c-rd-1","op":"respond-to-dialog","args":{"response_kind":"ok"}}""",
            default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-rd-1", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        var err = (string)node["error"] ?? "";
        Assert.Contains("dialog_id", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dispatcher_MissingResponseKind_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("respond-to-dialog", (args, ct) =>
            Task.FromResult(DialogHandlers.RespondToDialog(null, args)));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(
            """{"id":"c-rd-2","op":"respond-to-dialog","args":{"dialog_id":"42"}}""",
            default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-rd-2", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        var err = (string)node["error"] ?? "";
        Assert.Contains("response_kind", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dispatcher_UnknownResponseKind_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("respond-to-dialog", (args, ct) =>
            Task.FromResult(DialogHandlers.RespondToDialog(null, args)));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(
            """{"id":"c-rd-3","op":"respond-to-dialog","args":{"dialog_id":"42","response_kind":"banana"}}""",
            default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.Equal("c-rd-3", (string)node["cmd_id"]);
        Assert.False((bool)node["ok"]);
        var err = (string)node["error"] ?? "";
        Assert.Contains("banana", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dispatcher_IntegerKindMissingIntegerValue_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("respond-to-dialog", (args, ct) =>
            Task.FromResult(DialogHandlers.RespondToDialog(null, args)));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(
            """{"id":"c-rd-4","op":"respond-to-dialog","args":{"dialog_id":"42","response_kind":"integer"}}""",
            default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.False((bool)node["ok"]);
        var err = (string)node["error"] ?? "";
        Assert.Contains("integer_value", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dispatcher_StringKindMissingStringValue_ReturnsOkFalse()
    {
        var d = new CommandDispatcher();
        d.Register("respond-to-dialog", (args, ct) =>
            Task.FromResult(DialogHandlers.RespondToDialog(null, args)));

        string captured = null;
        var latch = new ManualResetEventSlim();
        using var _sub = PerceptionEmitterCapture.Capture(s => { captured = s; latch.Set(); });
        await d.HandleLineAsync(
            """{"id":"c-rd-5","op":"respond-to-dialog","args":{"dialog_id":"42","response_kind":"string"}}""",
            default);
        Assert.True(latch.Wait(TimeSpan.FromSeconds(2)), "response never emitted");

        var node = JsonNode.Parse(captured).AsObject();
        Assert.False((bool)node["ok"]);
        var err = (string)node["error"] ?? "";
        Assert.Contains("string_value", err, StringComparison.OrdinalIgnoreCase);
    }

    // ---- No-avatar path (vm host is null) ----

    [Fact]
    public void RespondToDialog_NullVMHost_NoAvatarError()
    {
        // RespondToDialog with a null vmHost after arg parsing completes (valid args)
        // should return ok=false with "no live avatar".
        var args = new JsonObject
        {
            ["dialog_id"] = "12345",
            ["response_kind"] = "ok",
        };
        // Passing null vmHost: will fail at the avatar presence check.
        var resp = DialogHandlers.RespondToDialog(null, args);
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Wire PDU class identity ----

    [Fact]
    public void WirePdu_VMNetDialogResponseCmd_ClassIdentity()
    {
        // Guards against accidental class removal or rename.
        Assert.NotNull(typeof(FSO.SimAntics.NetPlay.Model.Commands.VMNetDialogResponseCmd));
    }

    // ---- ResponseCode mapping (unit-level, no VM required) ----

    /// <summary>
    /// Validate the ResponseCode mapping by calling RespondToDialog on a thin
    /// stub CommandDispatcher (args parsed, vm=null so it bails out before SendCommand).
    /// We only assert the error shape for the null-vm path to confirm arg parsing succeeded.
    ///
    /// Full ResponseCode → wire-PDU mapping is proven by the integration test
    /// (tests/integration/verb-dialog.sh) which exercises VMNetDialogResponseCmd.Execute().
    /// </summary>
    [Theory]
    [InlineData("ok")]
    [InlineData("cancel")]
    public void RespondToDialog_ValidKind_NoAvatarError_NotArgError(string kind)
    {
        var args = new JsonObject
        {
            ["dialog_id"] = "1",
            ["response_kind"] = kind,
        };
        var resp = DialogHandlers.RespondToDialog(null, args);
        // Should fail with avatar error, NOT arg-validation error — args parsed cleanly.
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RespondToDialog_IntegerKind_ValidArgs_NoAvatarError()
    {
        var args = new JsonObject
        {
            ["dialog_id"] = "1",
            ["response_kind"] = "integer",
            ["integer_value"] = (long)50,
        };
        var resp = DialogHandlers.RespondToDialog(null, args);
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RespondToDialog_StringKind_ValidArgs_NoAvatarError()
    {
        var args = new JsonObject
        {
            ["dialog_id"] = "1",
            ["response_kind"] = "string",
            ["string_value"] = "hello",
        };
        var resp = DialogHandlers.RespondToDialog(null, args);
        Assert.False(resp.Ok);
        Assert.Contains("avatar", resp.Error, StringComparison.OrdinalIgnoreCase);
    }
}
