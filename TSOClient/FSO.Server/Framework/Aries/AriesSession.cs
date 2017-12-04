using Mina.Core.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Aries
{
    public class AriesSession : IAriesSession
    {
        public bool IsAuthenticated { get; set; }
        public uint LastRecv { get; set; }
        public IoSession IoSession;

        public AriesSession(IoSession ioSession)
        {
            this.IoSession = ioSession;
            IsAuthenticated = false;
        }

        public bool Connected
        {
            get
            {
                return IoSession?.Connected ?? false;
            }
        }

        public virtual void Close()
        {
            this.IoSession.Close(false);
        }
        
        public void Write(params object[] messages)
        {
            //TODO: Frame this more efficiently
            foreach(var message in messages)
            {
                this.IoSession.Write(message);
            }
        }

        public override string ToString()
        {
            return IoSession.ToString();
        }

        public T UpgradeSession<T>() where T : AriesSession {
            var instance = (T)Activator.CreateInstance(typeof(T), new object[] { IoSession });
            instance.IsAuthenticated = this.IsAuthenticated;
            IoSession.SetAttribute("s", instance);
            return instance;
        }

        public object GetAttribute(string key)
        {
            return IoSession.GetAttribute(key);
        }

        public void SetAttribute(string key, object value)
        {
            IoSession.SetAttribute(key, value);
        }
    }
}
