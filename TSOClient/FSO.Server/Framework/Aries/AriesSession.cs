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
        
        public IoSession IoSession;


        public AriesSession(IoSession ioSession)
        {
            this.IoSession = ioSession;
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

        public void Write(object message)
        {
            this.IoSession.Write(message);
        }
    }
}
