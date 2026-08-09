using System;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// The seam between UIMakeSomethingDialog (the player-facing "Make Something" panel) and
    /// whatever holds the actual agent conversation (an MCP client talking to FSO.ModServer,
    /// per PLAYER-LAYER-DESIGN.md). Deliberately small and opaque to how the bridge works —
    /// the panel only ever sees narration strings, a finished object's GUID, or a player-safe
    /// error string. Never a tool name, never JSON, never a stack trace or MCP error code —
    /// that's a contract on the bridge implementation, not just a style preference (see
    /// PLAYER-LAYER-DESIGN.md §2 step 3: progress reads as casual narration, not a build log).
    ///
    /// Per the approved design: implementations of this interface marshal their own callbacks
    /// onto the game thread before invoking them. UIMakeSomethingDialog does not call
    /// GameThread.NextUpdate itself — it trusts events arrive already safe to touch UI state.
    /// </summary>
    public interface IMakeSomethingAgent
    {
        /// <summary>Player pressed send / hit enter with this text.</summary>
        void SendMessage(string playerText);

        /// <summary>One line of in-character progress narration to append to the chat log.</summary>
        event Action<string> OnNarration;

        /// <summary>The agent finished; the object is compiled and ready, identified by GUID.</summary>
        event Action<uint> OnObjectComplete;

        /// <summary>Something went wrong. Message must already be player-safe.</summary>
        event Action<string> OnError;
    }
}
