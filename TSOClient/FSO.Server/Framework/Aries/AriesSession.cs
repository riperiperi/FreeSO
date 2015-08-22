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
        public uint UserId { get; set; }
        public uint AvatarId { get; set; }
        public bool IsAuthenticated { get; set; }

        public IoSession IoSession;


        public AriesSession(IoSession ioSession)
        {
            this.IoSession = ioSession;
            IsAuthenticated = false;
        }

        public bool IsAnonymous {
            get{
                return AvatarId == 0;
            }
        }

        public void Close()
        {
            this.IoSession.Close(true);
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
    }
}
