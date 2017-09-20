using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model
{
    /// <summary>
    /// Internal client repesentation for VMServerDriver. Keeps some state and thread safe message queuing.
    /// </summary>
    public class VMNetClient
    {
        public uint PersistID;
        public string RemoteIP;
        public VMNetAvatarPersistState AvatarState; //initial... obviously this can change while the lot is running.
        public bool HadAvatar;
        public int InactivityTicks;
        public object NetHandle;

        private Queue<VMNetMessage> Messages = new Queue<VMNetMessage>();

        internal Queue<VMNetMessage> GetMessages()
        {
            lock (this)
            {
                var last = Messages;
                Messages = new Queue<VMNetMessage>();
                return last;
            }
        }

        internal void SubmitMessage(VMNetMessage msg)
        {
            lock (this)
            {
                Messages.Enqueue(msg);
            }
        }
    }
}
