using FSO.Common.Utils;
using FSO.Server.Common;

namespace FSO.Server.Servers
{
    public abstract class AbstractServer
    {
        public abstract void Start();
        public abstract void Shutdown();

        public abstract void AttachDebugger(IServerDebugger debugger);

        public event Callback<AbstractServer, ShutdownType> OnInternalShutdown;
        public void SignalInternalShutdown(ShutdownType type)
        {
            OnInternalShutdown?.Invoke(this, type);
        }
    }
}
