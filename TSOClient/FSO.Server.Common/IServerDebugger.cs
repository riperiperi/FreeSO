using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Common
{
    public interface IServerDebugger
    {
        IPacketLogger GetPacketLogger();
        void AddSocketServer(ISocketServer server);
    }


    public interface ISocketServer
    {
        List<ISocketSession> GetSocketSessions();
    }


    public interface ISocketSession
    {
        void Write(params object[] data);
    }
}
