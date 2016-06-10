using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model
{
    public enum VMChatEventType
    {
        Message = 0,
        MessageMe = 1,
        Join = 2,
        Leave = 3,
        Arch = 4,
        Generic = 5,
    }

    public class VMChatEvent
    {
        public VMChatEventType Type;
        public string[] Text;
        public int Visitors = 0;
        public uint SenderUID = 0;

        public VMChatEvent(uint uid, VMChatEventType type, params string[] text)
        {
            SenderUID = uid;
            Type = type;
            Text = text;
        }
    }
}
