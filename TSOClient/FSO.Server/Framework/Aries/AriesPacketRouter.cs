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
            this.On(typeof(T), handler);
        }

        public void Handle(IAriesSession session, object message)
        {
            var type = message.GetType();
            if (Handlers.ContainsKey(type))
            {
                Handlers[type](session, message);
            }
        }

        public void On(Type type, AriesHandler handler)
        {
            Handlers.Add(type, handler);
        }

        public void AddHandlers(object obj)
        {
            var methods = obj.GetType().GetMethods();

            foreach (var method in methods)
            {
                var args = method.GetParameters();
                if (method.Name.StartsWith("Handle") &&
                    args.Length == 2 &&
                    typeof(IAriesSession).IsAssignableFrom(args[0].ParameterType))
                {
                    this.On(args[1].ParameterType, new AriesHandler(delegate (IAriesSession session, object msg)
                    {
                        method.Invoke(obj, new object[] { session, msg });
                    }));
                }
            }
        }
    }
}
