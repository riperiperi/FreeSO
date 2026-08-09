using System;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace FSO.ModServer
{
    public class PackSession
    {
        public string Id;
        public JObject Pack;
        public readonly object Lock = new object();
    }

    /// <summary>
    /// In-memory session state, keyed by pack_session_id — per MCP-DESIGN.md §1/§5
    /// ("a simple ConcurrentDictionary<string, PackSession> in the server process is
    /// sufficient for v1"). Static because tool methods are static (SDK attribute pattern);
    /// this is also what lets xunit call the handlers directly without standing up a host.
    /// </summary>
    public static class SessionStore
    {
        private static readonly ConcurrentDictionary<string, PackSession> Sessions = new();

        public static PackSession Create(JObject pack)
        {
            var session = new PackSession { Id = Guid.NewGuid().ToString("N"), Pack = pack };
            Sessions[session.Id] = session;
            return session;
        }

        public static bool TryGet(string id, out PackSession session) => Sessions.TryGetValue(id, out session);
    }
}
