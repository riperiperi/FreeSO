using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class GlobalChatMessage : AbstractElectronPacket
    {
        public uint SenderUID;
        public string SenderName = "";
        public string Message = "";
        public uint Color;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            SenderUID = input.GetUInt32();
            SenderName = input.GetPascalVLCString();
            Message = input.GetPascalVLCString();
            Color = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType() => ElectronPacketType.GlobalChatMessage;

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(SenderUID);
            output.PutPascalVLCString(SenderName);
            output.PutPascalVLCString(Message);
            output.PutUInt32(Color);
        }
    }
}