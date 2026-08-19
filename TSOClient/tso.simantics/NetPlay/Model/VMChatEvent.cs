using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;

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
        Debug = 6,
        SurroundMessage = 7,
        SwitchLot = 8
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
        public sbyte TTSPitch;
        public bool Gender;

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
            var tsoState = ava?.TSOState as VMTSOAvatarState;
            TTSPitch = tsoState?.ChatTTSPitch ?? 0;
            Color = tsoState?.ChatColor ?? Color.LightGray;
            Gender = (ava?.GetPersonData(SimAntics.Model.VMPersonDataVariable.Gender) ?? 0) > 0;
        }

        public VMChatEvent(uint senderUid, uint senderColor, sbyte ttsPitch, bool gender, VMChatEventType type, params string[] text)
        {
            SenderUID = senderUid;
            Type = type;
            Text = text;
            Timestamp = DateTime.Now.ToShortTimeString();
            TTSPitch = ttsPitch;
            Gender = gender;
            Color = new Color(senderColor);
        }
    }
}
