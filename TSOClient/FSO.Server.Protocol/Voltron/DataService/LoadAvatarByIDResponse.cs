using FSO.Server.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Voltron.Model;

namespace FSO.Server.Protocol.Voltron.DataService
{
    [clsid(0x8ADF865D)]
    [cTSONetMessageParameter(DBResponseType.LoadAvatarByID)]
    public class LoadAvatarByIDResponse : IoBufferSerializable, IoBufferDeserializable
    {
        public uint AvatarId;

        public short AvatarSkills_Logic;
        public short AvatarSkills_LockLv_Logic;
        public short AvatarSkills_Body;
        public short AvatarSkills_LockLv_Body;
        public short AvatarSkills_LockLv_Mechanical;
        public short AvatarSkills_LockLv_Creativity;
        public short AvatarSkills_LockLv_Cooking;
        public short AvatarSkills_Cooking;
        public short AvatarSkills_Charisma;
        public short AvatarSkills_LockLv_Charisma;
        public short AvatarSkills_Mechanical;
        public short AvatarSkills_Creativity;

        public uint Cash = 0x46474849;

        public IoBuffer Serialize()
        {
            var result = AbstractVoltronPacket.Allocate(4);
            result.AutoExpand = true;

            result.PutUInt32(AvatarId);
            result.PutPascalVLCString("A");
            result.PutPascalVLCString("B");

            result.PutInt16(AvatarSkills_Logic);
            result.PutInt16(AvatarSkills_LockLv_Logic);
            result.PutInt16(AvatarSkills_Body);
            result.PutInt16(AvatarSkills_LockLv_Body);
            result.PutInt16(AvatarSkills_LockLv_Mechanical);
            result.PutInt16(AvatarSkills_LockLv_Creativity);
            result.PutInt16(AvatarSkills_LockLv_Cooking);
            result.PutInt16(AvatarSkills_Cooking);
            result.PutInt16(AvatarSkills_Charisma);
            result.PutInt16(AvatarSkills_LockLv_Charisma);
            result.PutInt16(AvatarSkills_Mechanical);
            result.PutInt16(AvatarSkills_Creativity);

            //Unknown
            result.PutUInt32(0x28292a2b);
            result.PutUInt32(0x2c2d2e2f);
            result.PutUInt32(0x30313233);
            result.PutUInt32(0x34353637);
            result.PutUInt32(0x38393a3b);
            result.PutUInt32(0x3c3d3e3f);
            result.PutUInt32(0x40414243);
            result.Put(0x44);
            result.Put(0x45);

            result.PutUInt32(Cash);

            //Unknown
            result.PutUInt32(0x4a4b4c4d);
            result.PutUInt32(0x4e4f5051);
            result.Put(0x52);

            result.PutPascalVLCString("C");
            result.PutPascalVLCString("D");
            result.PutPascalVLCString("E");
            result.PutPascalVLCString("F");
            result.PutPascalVLCString("G");

            result.PutUInt32(0x54555657);
            result.PutUInt32(0x58595A5B);
            result.PutUInt32(0x5C5D5E5F);
            result.PutUInt32(0x60616263);
            result.PutUInt32(0x64656667);
            result.PutUInt32(0x68696A6B);
            result.PutUInt32(0x6C6D6E6F);
            result.PutUInt32(0x70717273);
            result.PutUInt32(0x74757677);

            //Bonus count
            result.PutUInt32(0x01);

            //Unknown
            result.PutUInt32(0x7C7D7E7F);
            result.PutUInt32(0x80818283);

            //Sim bonus
            result.PutUInt32(0x84858687);

            //Property bonus
            result.PutUInt32(0x88898A8B);

            //Visitor bonus
            result.PutUInt32(0x8C8D8E8F);
            
            //Date string
            result.PutPascalVLCString("H");


            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt32(0x94959697);
            result.PutUInt64(0x98999a9b9c9d9e9f);

            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt32(0xa5a6a7a8);
            result.PutUInt32(0xa9aaabac);

            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt16(0xb1b2);
            result.PutUInt16(0xb3b4);
            result.PutUInt32(0xb5b6b7b8);
            result.PutUInt16(0xb9ba);

            result.PutUInt16(0xbbbc);
            result.PutUInt16(0xbdbe);
            result.PutUInt32(0xbfc0c1c2);
            result.PutUInt32(0xc3c4c5c6);

            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt32(0xcbcccdce);
            result.PutUInt32(0xcfd0d1d2);
            result.PutUInt32(0xd3d4d5d6);
            result.Put(0xd7);
            result.Put(0xd8);
            result.PutUInt32(0xd9dadbdc);
            result.PutUInt32(0xdddedfe0);
            result.PutUInt32(0xe1e2e3e4);

            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt32(0xe9eaebec);
            result.PutUInt32(0xedeeeff0);
            result.PutUInt32(0xf1f2f3f4);
            result.Put(0xf5);
            result.Put(0xf6);
            result.PutUInt32(0xf7f8f9fa);
            result.PutUInt32(0xfbfcfdfe);
            result.PutUInt32(0xff808182);

            //Unknown
            //count
            result.PutUInt32(0x01);

            result.PutUInt32(0x8788898a);
            result.PutUInt16(0x8b8c);
            result.PutUInt32(0x8d8e8f90);
            result.PutUInt32(0x91929394);
            result.PutUInt32(0x95969798);
            result.PutUInt32(0x999a9b9c);

            //Unknown
            //count
            result.PutUInt32(0x01);
            result.PutUInt16(0x7071);
            result.PutUInt64(0x7273747576777879);

            //Unknown
            result.PutUInt32(0x7a7b7c7d);
            result.PutUInt32(0x7e7f6061);
            result.PutUInt32(0x62636465);
            result.PutUInt32(0x66676869);
            result.PutUInt32(0x6a6b6c6d);
            result.PutUInt32(0x6e6f5051);
            result.PutUInt32(0x52535455);
            result.PutUInt32(0x56575859);
            result.PutUInt32(0x69f4d5e8);
            result.PutUInt32(0x5e5f4041);

            return result;
        }

        public void Deserialize(IoBuffer input)
        {
            AvatarId = input.GetUInt32();
            input.GetPascalVLCString(); //A
            input.GetPascalVLCString(); //B

            AvatarSkills_Logic = input.GetInt16();
            AvatarSkills_LockLv_Logic = input.GetInt16();
            AvatarSkills_Body = input.GetInt16();
            AvatarSkills_LockLv_Body = input.GetInt16();
            AvatarSkills_LockLv_Mechanical = input.GetInt16();
            AvatarSkills_LockLv_Creativity = input.GetInt16();
            AvatarSkills_LockLv_Cooking = input.GetInt16();
            AvatarSkills_Cooking = input.GetInt16();
            AvatarSkills_Charisma = input.GetInt16();
            AvatarSkills_LockLv_Charisma = input.GetInt16();
            AvatarSkills_Mechanical = input.GetInt16();
            AvatarSkills_Creativity = input.GetInt16();

            //Unknown
            input.GetUInt32(); //0x28292a2b
            input.GetUInt32(); //0x2c2d2e2f
            input.GetUInt32(); //0x30313233
            input.GetUInt32(); //0x34353637
            input.GetUInt32(); //0x38393a3b
            input.GetUInt32(); //0x3c3d3e3f
            input.GetUInt32(); //0x40414243
            input.Get(); //0x44
            input.Get(); //0x45

            Cash = input.GetUInt32();
        }
    }
}
