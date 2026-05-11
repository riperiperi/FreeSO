/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.Bot.Headless;

/// <summary>
/// Bot-side handler for the dialog-family IPC op (freesoexperiment-849):
/// <c>respond-to-dialog</c>.
///
/// <para>
/// The Sims Online VM can raise a modal dialog from within a Sim's behavior tree
/// (e.g. "How much to spend on groceries?" from the Fridge's Restock interaction).
/// Until a <see cref="VMNetDialogResponseCmd"/> is sent, the Sim's thread stays
/// blocked in <see cref="FSO.SimAntics.Primitives.VMDialogPrivateStrings"/> with its
/// <c>BlockingState</c> set to a <see cref="FSO.SimAntics.Primitives.VMDialogResult"/>.
/// </para>
///
/// <para>
/// The agent sees the dialog via <c>perception.recent_events[]</c> entries with
/// <c>kind == "dialog"</c>. It reads <c>extras.dialog_id</c> (a ulong serialised as a
/// decimal string) and calls <c>respond-to-dialog</c> with:
/// <list type="bullet">
///   <item><c>dialog_id</c> — the id from the perception event (validated but not used
///     server-side; the VM matches by blocked-thread state, not by id). Included for
///     correctness and future server-side validation.</item>
///   <item><c>response_kind</c> — one of <c>ok</c>, <c>cancel</c>, <c>integer</c>,
///     <c>string</c>.</item>
///   <item><c>integer_value</c> — required when response_kind is <c>integer</c>.</item>
///   <item><c>string_value</c> — required when response_kind is <c>string</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>ResponseCode mapping</b> (per VMDialogResult comment):
/// <list type="bullet">
///   <item>0 = OK / Yes — used for <c>ok</c></item>
///   <item>1 = No — unused in this handler (map to ok/cancel semantics for simplicity)</item>
///   <item>2 = Cancel — used for <c>cancel</c></item>
/// </list>
/// For <c>integer</c> and <c>string</c> response kinds, ResponseCode is 0 (OK) and
/// ResponseText carries the value. The VM's NumericEntry/TextEntry branch parses
/// ResponseText — see VMDialogPrivateStrings.cs switch on VMDialogType.
/// </para>
///
/// <para>
/// Thread safety: <see cref="RespondToDialog"/> calls
/// <see cref="HeadlessVMHost.RunUnderTickLock{T}"/> to read the avatar's BlockingState
/// safely. The tick thread owns VMThread.BlockingState; reading it without the lock races.
/// The actual PDU dispatch (<see cref="VMClientDriver.SendCommand"/>) is done outside the
/// lock because SendCommand has its own internal lock.
/// </para>
/// </summary>
public static class DialogHandlers
{
    public static void RegisterAll(CommandDispatcher dispatcher, HeadlessVMHost vmHost)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
        if (vmHost == null) throw new ArgumentNullException(nameof(vmHost));

        dispatcher.Register("respond-to-dialog",
            (args, ct) => Task.FromResult(RespondToDialog(vmHost, args)));
    }

    /// <summary>
    /// respond-to-dialog — answer a pending VM dialog.
    ///
    /// Args:
    ///   dialog_id      (string, required) — ulong dialog id from perception.recent_events extras
    ///   response_kind  (string, required) — ok | cancel | integer | string
    ///   integer_value  (long, optional)   — required when response_kind = integer
    ///   string_value   (string, optional) — required when response_kind = string
    ///
    /// Response payload on success:
    /// <code>{"ok": true, "dialog_id": "...", "response_kind": "...", "response_code": 0, "response_text": "50"}</code>
    ///
    /// Response on refusal:
    /// <code>{"ok": false, "error": "..."}</code>
    /// </summary>
    internal static CommandDispatcher.Response RespondToDialog(HeadlessVMHost vmHost, JsonObject args)
    {
        // --- arg parsing ---

        var dialogIdArg = (string)args["dialog_id"];
        if (string.IsNullOrEmpty(dialogIdArg))
            return CommandDispatcher.Response.Fail("respond-to-dialog requires dialog_id");

        var responseKindArg = (string)args["response_kind"];
        if (string.IsNullOrEmpty(responseKindArg))
            return CommandDispatcher.Response.Fail("respond-to-dialog requires response_kind (ok|cancel|integer|string)");

        byte responseCode;
        string responseText;

        switch (responseKindArg.ToLowerInvariant())
        {
            case "ok":
                responseCode = 0;  // Yes/OK
                responseText = "";
                break;

            case "cancel":
                responseCode = 2;  // Cancel
                responseText = "";
                break;

            case "integer":
            {
                var intArg = args["integer_value"];
                if (intArg == null)
                    return CommandDispatcher.Response.Fail("respond-to-dialog: response_kind=integer requires integer_value");
                long iv;
                if (intArg is System.Text.Json.Nodes.JsonValue jv && jv.TryGetValue<long>(out var lv))
                    iv = lv;
                else if (!long.TryParse(intArg.ToString(), out iv))
                    return CommandDispatcher.Response.Fail($"respond-to-dialog: integer_value not a valid integer (got {intArg})");

                responseCode = 0;  // OK — numeric entry confirms with OK
                responseText = iv.ToString();
                break;
            }

            case "string":
            {
                var sv = (string)args["string_value"];
                if (sv == null)
                    return CommandDispatcher.Response.Fail("respond-to-dialog: response_kind=string requires string_value");
                responseCode = 0;  // OK — text entry confirms with OK
                responseText = sv;
                break;
            }

            default:
                return CommandDispatcher.Response.Fail(
                    $"respond-to-dialog: unknown response_kind '{responseKindArg}' (expected ok|cancel|integer|string)");
        }

        // --- avatar presence check (under tick lock to read BlockingState safely) ---

        VMAvatar caller = null;
        if (vmHost != null)
        {
            caller = vmHost.RunUnderTickLock<VMAvatar>(() =>
                vmHost.VM?.GetAvatarByPersist(vmHost.MyAvatarPersistId));
        }

        if (caller == null)
            return CommandDispatcher.Response.Fail("respond-to-dialog: no live avatar");

        // Note: we do NOT pre-check caller.Thread.BlockingState here even though
        // it would be cleaner. Reason: the state check and the SendCommand are not
        // atomic, and the VM may un-block the avatar between our check and the send.
        // VMNetDialogResponseCmd.Execute() has a defensive null guard; the server-side
        // VM will simply no-op if the avatar is no longer blocked. This is correct and
        // safe — better to send a spurious PDU than to refuse a valid response because
        // of a tiny TOCTOU window.

        // --- send the command ---

        var cmd = new VMNetDialogResponseCmd
        {
            ResponseCode = responseCode,
            ResponseText = responseText ?? "",
        };
        vmHost.Driver.SendCommand(cmd);

        return CommandDispatcher.Response.Success(new
        {
            dialog_id = dialogIdArg,
            response_kind = responseKindArg,
            response_code = (int)responseCode,
            response_text = responseText ?? "",
        });
    }
}
