using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Utils;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSessionResponse : IAriesPacket
    {
        public string User { get; set; }
        public string AriesVersion { get; set; }
        public string Email { get; set; }
        public string Authserv { get; set; }
        public ushort Product { get; set; }
        public byte Unknown { get; set; }
        public string ServiceIdent { get; set; }
        public ushort Unknown2 { get; set; }
        public string Password { get; set; }

        public void Deserialize(IoBuffer input)
        {
            this.User = input.GetString(112, Encoding.ASCII);
            this.AriesVersion = input.GetString(80, Encoding.ASCII);
            this.Email = input.GetString(40, Encoding.ASCII);
            this.Authserv = input.GetString(84, Encoding.ASCII);
            this.Product = input.GetUInt16();
            this.Unknown = input.Get();
            this.ServiceIdent = input.GetString(3, Encoding.ASCII);
            this.Unknown2 = input.GetUInt16();
            this.Password = input.GetString(32, Encoding.ASCII);
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestClientSessionResponse;
        }

        public IoBuffer Serialize()
        {
            var buffer = IoBuffer.Allocate(356);
            buffer.PutString(this.User, 112, Encoding.ASCII);
            buffer.PutString(this.AriesVersion, 80, Encoding.ASCII);
            buffer.PutString(this.Email, 40, Encoding.ASCII);
            buffer.PutString(this.Authserv, 84, Encoding.ASCII);
            buffer.PutUInt16(this.Product);
            buffer.Put(0x39);
            buffer.PutString(this.ServiceIdent, 3, Encoding.ASCII);
            buffer.PutUInt16(this.Unknown2);
            buffer.PutString(this.Password, 32, Encoding.ASCII);
            return buffer;
        }
    }
}
