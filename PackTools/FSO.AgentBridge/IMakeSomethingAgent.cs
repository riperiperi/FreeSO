using System;

namespace FSO.AgentBridge
{
    /// <summary>
    /// The seam between the player-facing UI and the agent loop. Per
    /// PLAYER-LAYER-DESIGN.md §2.3 the player never sees a tool name, JSON, or a build log —
    /// so nothing on this interface carries one. Narration is prose; errors are prose; the
    /// only structured value that escapes is the finished object's GUID.
    ///
    /// Every event is raised through the agent's dispatcher (see MakeSomethingAgent), so a
    /// consumer may touch UI state directly in a handler without marshaling it again.
    /// </summary>
    public interface IMakeSomethingAgent
    {
        void SendMessage(string playerText);

        /// <summary>One line of in-character progress. Never a tool name, never JSON.</summary>
        event Action<string> OnNarration;

        /// <summary>The finished object's GUID, ready to place.</summary>
        event Action<uint> OnObjectComplete;

        /// <summary>Player-safe message. Never a stack trace, never an internal error code.</summary>
        event Action<string> OnError;
    }
}
