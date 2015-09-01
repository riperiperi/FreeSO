using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [cTSONetMessageParameter(DBRequestType.LoadAvatarByID)]
    public class LoadAvatarByIDRequest : IoBufferSerializable, IoBufferDeserializable
    {
        public uint AvatarId;
        public uint Unknown1 = 0;
        public uint Unknown2 = 0x69f4d5e8;

        public void Deserialize(IoBuffer input)
        {
            this.AvatarId = input.GetUInt32();
            this.Unknown1 = input.GetUInt32();

            //Reserved - 32 bytes of uninitialized memory (just like Heartbleed); equal to "BA AD F0 0D BA AD F0 0D ..." if you are running the game in a debugger
            input.Skip(32);

            this.Unknown2 = input.GetUInt32();
        }

        public IoBuffer Serialize()
        {
            var result = AbstractVoltronPacket.Allocate(44);
            result.PutUInt32(AvatarId);
            result.PutUInt32(Unknown1);
            result.Skip(32);
            result.PutUInt32(Unknown2);
            return result;
        }
    }
}
