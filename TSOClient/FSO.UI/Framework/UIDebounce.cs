using FSO.Common.Utils;

namespace FSO.Client.UI.Framework
{
    public class UIDebounce
    {
        public const int DEFAULT_TIMEOUT = 1500;
        public int Timeout { get; internal set; }
        private GameThreadTimeout ThreadTimeout;

        public UIDebounce() : this(DEFAULT_TIMEOUT)
        {
        }

        public UIDebounce(int timeout){
            Timeout = timeout;
        }

        public void Invoke(Callback callback){
            if(ThreadTimeout != null){
                ThreadTimeout.Clear();
            };
            ThreadTimeout = GameThread.SetTimeout(callback, Timeout);
        }
    }
}
