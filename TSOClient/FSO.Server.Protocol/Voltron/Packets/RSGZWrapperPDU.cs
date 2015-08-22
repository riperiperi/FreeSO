using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;
using FSO.Server.Protocol.Utils;
using System.ComponentModel;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class RSGZWrapperPDU : AbstractVoltronPacket
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Gender Gender { get; set; }
        public SkinTone SkinTone { get; set; }
        public uint HeadOutfitId { get; set; }
        public uint BodyOutfitId { get; set; }
        

        public override void Deserialize(IoBuffer input)
        {
            //ignoring the dbwrapper junk
            //byte 17 and 18 change without changing settings, perhaps a message id?
            input.Skip(37);
            this.Name = input.GetPascalVLCString();
            this.Description = input.GetPascalVLCString();
            this.Gender = input.Get() == 0x1 ? Gender.FEMALE : Gender.MALE;

            var skin = input.Get();
            switch (skin) {
                default:
                case 0x00:
                    SkinTone = SkinTone.LIGHT;
                    break;
                case 0x01:
                    SkinTone = SkinTone.MEDIUM;
                    break;
                case 0x02:
                    SkinTone = SkinTone.DARK;
                    break;
            }

            this.HeadOutfitId = input.GetUInt32();
            input.Skip(4); //Unknown
            this.BodyOutfitId = input.GetUInt32();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.RSGZWrapperPDU;
        }

        public override IoBuffer Serialize()
        {
            //TODO:
            return Allocate(0);
        }
    }
}
