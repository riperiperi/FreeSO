using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;
using System;

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
        Debug = 6
    }

    public class VMChatEvent
    {
        public VMChatEventType Type;
        public Color Color;
        public string[] Text;
        public int Visitors = 0;
        public uint SenderUID = 0;
        public byte ChannelID = 0;
        public string Timestamp;
        public VMTSOChatChannel Channel;

        public VMChatEvent(VMAvatar ava, VMChatEventType type, byte channelID, params string[] text) : this(ava, type, text)
        {
            ChannelID = channelID;
        }

        public VMChatEvent(VMAvatar ava, VMChatEventType type, params string[] text)
        {
            SenderUID = ava?.PersistID ?? 0;
            Type = type;
            Text = text;
            Timestamp = DateTime.Now.ToShortTimeString();
            Color = (ava?.TSOState as VMTSOAvatarState)?.ChatColor ?? Color.LightGray;
        }
    }
}
