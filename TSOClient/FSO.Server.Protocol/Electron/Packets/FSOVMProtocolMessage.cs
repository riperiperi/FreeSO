using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FSOVMProtocolMessage : AbstractElectronPacket
    {
        public bool UseCst;
        public string Title;
        public string Message;

        public FSOVMProtocolMessage()
        {

        }

        public FSOVMProtocolMessage(bool cst, string title, string body)
        {
            UseCst = cst;
            Title = title;
            Message = body;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            UseCst = input.GetBool();
            Title = input.GetPascalVLCString();
            Message = input.GetPascalVLCString();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FSOVMProtocolMessage;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutBool(UseCst);
            output.PutPascalVLCString(Title);
            output.PutPascalVLCString(Message);
        }
    }
}
