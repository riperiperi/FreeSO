using System;
using System.Threading;
using FSO.Common.Utils;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Throwaway local fake of IMakeSomethingAgent, for building/testing UIMakeSomethingDialog
    /// without waiting on the real MCP bridge. Emits a few canned narration lines on a timer,
    /// then OnObjectComplete. Delete once the real bridge lands — do not build on this.
    /// </summary>
    public class StubMakeSomethingAgent : IMakeSomethingAgent
    {
        public event Action<string> OnNarration;
        public event Action<uint> OnObjectComplete;
        public event Action<string> OnError;

        static readonly string[] CannedNarration =
        {
            "Thinking about what you asked for...",
            "Sketching out a shape for it...",
            "Giving it something to say...",
            "Teaching it to notice you...",
            "Testing it out...",
            "Just about done...",
        };

        // Held as fields, not locals — a Timer with no surviving reference can be GC'd mid-countdown.
        Timer _timer;
        int _step;

        public void SendMessage(string playerText)
        {
            _step = 0;
            _timer = new Timer(_ =>
            {
                if (_step < CannedNarration.Length)
                {
                    var line = CannedNarration[_step++];
                    GameThread.NextUpdate(x => OnNarration?.Invoke(line));
                }
                else
                {
                    GameThread.NextUpdate(x => OnObjectComplete?.Invoke(0x7F000099));
                    _timer.Dispose();
                }
            }, null, 600, 900);
        }
    }
}
