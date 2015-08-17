using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public class AriesPacketRouter : IAriesPacketRouter
    {
        private Dictionary<Type, AriesHandler> Handlers = new Dictionary<Type, AriesHandler>();

        public void On<T>(AriesHandler handler)
        {
            Handlers.Add(typeof(T), handler);
        }

        public void Handle(IAriesSession session, object message)
        {
            var type = message.GetType();
            if (Handlers.ContainsKey(type))
            {
                Handlers[type](session, message);
            }
        }
    }
}
