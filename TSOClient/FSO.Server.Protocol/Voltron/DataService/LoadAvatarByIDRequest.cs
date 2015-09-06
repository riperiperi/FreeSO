using FSO.Server.Protocol.Voltron.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [cTSONetMessageParameter(DBRequestType.LoadAvatarByID)]
    public class LoadAvatarByIDRequest : IoBufferSerializable, IoBufferDeserializable
    {
        public uint AvatarId;
        public uint Unknown1 = 0;
        public uint Unknown2 = 0x69f4d5e8;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            this.AvatarId = input.GetUInt32();
            this.Unknown1 = input.GetUInt32();

            //Reserved - 32 bytes of uninitialized memory (just like Heartbleed); equal to "BA AD F0 0D BA AD F0 0D ..." if you are running the game in a debugger
            input.Skip(32);

            this.Unknown2 = input.GetUInt32();
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutUInt32(Unknown1);
            output.Skip(32);
            output.PutUInt32(Unknown2);
        }
    }
}
