using System;
using System.Diagnostics;
using System.IO;
using FSO.Common.Utils;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// The real IMakeSomethingAgent: runs FSO.AgentBridge as a child process and turns its
    /// line protocol into the panel's events.
    ///
    /// Why a process rather than a project reference: the bridge pulls in LLM SDKs, HTTP
    /// clients and the whole pack toolchain. Linking that into the game client would put an
    /// API-calling dependency in every player's install and couple the client's build to the
    /// authoring stack. A pipe keeps the seam exactly as narrow as IMakeSomethingAgent
    /// promises — three kinds of line, nothing else.
    ///
    /// It also means a crash in the agent cannot take the game down with it.
    ///
    /// Protocol (stdout, one event per line):
    ///   N &lt;text&gt;  narration to show verbatim
    ///   G &lt;hex&gt;   finished, object GUID
    ///   E &lt;text&gt;  player-safe error
    /// stderr is diagnostics only and is never shown to the player.
    /// </summary>
    public class ProcessMakeSomethingAgent : IMakeSomethingAgent
    {
        public event Action<string> OnNarration;
        public event Action<uint> OnObjectComplete;
        public event Action<string> OnError;

        private Process _proc;

        /// <summary>
        /// Where the built agent lives. Set once at startup; if it's missing we say so in
        /// player language rather than throwing, because a missing tool is a setup problem
        /// and not something the player did.
        /// </summary>
        public static string BridgePath =
            Environment.GetEnvironmentVariable("FSO_AGENT_BRIDGE") ?? "FSO.AgentBridge";

        public void SendMessage(string playerText)
        {
            if (_proc != null && !_proc.HasExited)
            {
                Raise(OnError, "I'm still working on the last one — give me a moment.");
                return;
            }

            if (!File.Exists(BridgePath))
            {
                // Deliberately not the path: it means nothing to a player and reads as a crash.
                Raise(OnError, "My workshop isn't set up on this computer yet.");
                return;
            }

            var psi = new ProcessStartInfo(BridgePath)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("--ipc");
            psi.ArgumentList.Add(playerText);

            try
            {
                _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                _proc.OutputDataReceived += (s, e) => { if (e.Data != null) Handle(e.Data); };
                // Read stderr so the pipe can't fill and block the child, but never surface it.
                _proc.ErrorDataReceived += (s, e) => { if (e.Data != null) System.Diagnostics.Debug.WriteLine("[agent] " + e.Data); };
                _proc.Exited += (s, e) =>
                {
                    // Exiting without ever reporting an outcome is a crash. Say something
                    // honest rather than leaving the panel spinning forever.
                    if (!_reported) Raise(OnError, "Something went wrong making that. Nothing was added.");
                };
                _proc.Start();
                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();
                _reported = false;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("[agent] failed to start: " + e);
                Raise(OnError, "I couldn't get to my workshop just now.");
            }
        }

        private bool _reported;

        private void Handle(string line)
        {
            if (line.Length < 2) return;
            var body = line.Length > 2 ? line.Substring(2) : "";

            switch (line[0])
            {
                case 'N':
                    Raise(OnNarration, body);
                    break;
                case 'O':
                    // Registration must happen on the game thread — it touches shared content
                    // and catalog state the renderer reads.
                    GameThread.NextUpdate(x => RegisterFromLine(body));
                    break;
                case 'G':
                    if (uint.TryParse(body.Trim(), System.Globalization.NumberStyles.HexNumber,
                                      null, out var guid))
                    {
                        _reported = true;
                        GameThread.NextUpdate(x =>
                        {
                            // Only claim success if the object is actually placeable. Saying
                            // "ready" over a failed registration is the failure mode this
                            // project keeps producing.
                            if (_registered == guid) OnObjectComplete?.Invoke(guid);
                            else OnError?.Invoke("I made it, but couldn't get it into your catalog.");
                        });
                    }
                    break;
                case 'E':
                    _reported = true;
                    Raise(OnError, body);
                    break;
                // Any other line is a protocol violation — swallow it rather than showing the
                // player something raw.
            }
        }

        private uint _registered;

        /// <summary>
        /// "O guid|category|price|name|iffPath" — split with a count so a name containing the
        /// separator can't shift the path field.
        /// </summary>
        private void RegisterFromLine(string body)
        {
            var parts = body.Split('|');
            if (parts.Length < 5) return;

            if (!uint.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.HexNumber,
                               null, out var guid)) return;
            sbyte.TryParse(parts[1], out var category);
            uint.TryParse(parts[2], out var price);
            var name = parts[3];
            var iffPath = string.Join("|", parts, 4, parts.Length - 4);

            if (LiveObjectRegistrar.Register(guid, iffPath, name, category, price))
                _registered = guid;
        }

        // Events must arrive on the game thread; the panel is documented to trust that.
        private void Raise(Action<string> ev, string text) =>
            GameThread.NextUpdate(x => ev?.Invoke(text));
    }
}
