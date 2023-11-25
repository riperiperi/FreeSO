using System;

namespace FSO.Server.Framework.Aries
{
    public interface IAriesPacketRouter
    {   
        void On<T>(AriesHandler handler);
        void On(Type type, AriesHandler handler);

        void AddHandlers(object obj);
    }
}
