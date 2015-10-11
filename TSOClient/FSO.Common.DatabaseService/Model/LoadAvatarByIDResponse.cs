using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Common.Serialization.TypeSerializers;
using FSO.Common.DatabaseService.Framework;

namespace FSO.Common.DatabaseService.Model
{
    [DatabaseResponse(DBResponseType.LoadAvatarByID)]
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

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutPascalVLCString("A");
            output.PutPascalVLCString("B");

            output.PutInt16(AvatarSkills_Logic);
            output.PutInt16(AvatarSkills_LockLv_Logic);
            output.PutInt16(AvatarSkills_Body);
            output.PutInt16(AvatarSkills_LockLv_Body);
            output.PutInt16(AvatarSkills_LockLv_Mechanical);
            output.PutInt16(AvatarSkills_LockLv_Creativity);
            output.PutInt16(AvatarSkills_LockLv_Cooking);
            output.PutInt16(AvatarSkills_Cooking);
            output.PutInt16(AvatarSkills_Charisma);
            output.PutInt16(AvatarSkills_LockLv_Charisma);
            output.PutInt16(AvatarSkills_Mechanical);
            output.PutInt16(AvatarSkills_Creativity);

            //Unknown
            output.PutUInt32(0x28292a2b);
            output.PutUInt32(0x2c2d2e2f);
            output.PutUInt32(0x30313233);
            output.PutUInt32(0x34353637);
            output.PutUInt32(0x38393a3b);
            output.PutUInt32(0x3c3d3e3f);
            output.PutUInt32(0x40414243);
            output.Put(0x44);
            output.Put(0x45);

            output.PutUInt32(Cash);

            //Unknown
            output.PutUInt32(0x4a4b4c4d);
            output.PutUInt32(0x4e4f5051);
            output.Put(0x52);

            output.PutPascalVLCString("C");
            output.PutPascalVLCString("D");
            output.PutPascalVLCString("E");
            output.PutPascalVLCString("F");
            output.PutPascalVLCString("G");

            output.PutUInt32(0x54555657);
            output.PutUInt32(0x58595A5B);
            output.PutUInt32(0x5C5D5E5F);
            output.PutUInt32(0x60616263);
            output.PutUInt32(0x64656667);
            output.PutUInt32(0x68696A6B);
            output.PutUInt32(0x6C6D6E6F);
            output.PutUInt32(0x70717273);
            output.PutUInt32(0x74757677);

            //Bonus count
            output.PutUInt32(0x01);

            //Unknown
            output.PutUInt32(0x7C7D7E7F);
            output.PutUInt32(0x80818283);

            //Sim bonus
            output.PutUInt32(0x84858687);

            //Property bonus
            output.PutUInt32(0x88898A8B);

            //Visitor bonus
            output.PutUInt32(0x8C8D8E8F);
            
            //Date string
            output.PutPascalVLCString("H");


            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt32(0x94959697);
            output.PutUInt64(0x98999a9b9c9d9e9f);

            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt32(0xa5a6a7a8);
            output.PutUInt32(0xa9aaabac);

            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt16(0xb1b2);
            output.PutUInt16(0xb3b4);
            output.PutUInt32(0xb5b6b7b8);
            output.PutUInt16(0xb9ba);

            output.PutUInt16(0xbbbc);
            output.PutUInt16(0xbdbe);
            output.PutUInt32(0xbfc0c1c2);
            output.PutUInt32(0xc3c4c5c6);

            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt32(0xcbcccdce);
            output.PutUInt32(0xcfd0d1d2);
            output.PutUInt32(0xd3d4d5d6);
            output.Put(0xd7);
            output.Put(0xd8);
            output.PutUInt32(0xd9dadbdc);
            output.PutUInt32(0xdddedfe0);
            output.PutUInt32(0xe1e2e3e4);

            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt32(0xe9eaebec);
            output.PutUInt32(0xedeeeff0);
            output.PutUInt32(0xf1f2f3f4);
            output.Put(0xf5);
            output.Put(0xf6);
            output.PutUInt32(0xf7f8f9fa);
            output.PutUInt32(0xfbfcfdfe);
            output.PutUInt32(0xff808182);

            //Unknown
            //count
            output.PutUInt32(0x01);

            output.PutUInt32(0x8788898a);
            output.PutUInt16(0x8b8c);
            output.PutUInt32(0x8d8e8f90);
            output.PutUInt32(0x91929394);
            output.PutUInt32(0x95969798);
            output.PutUInt32(0x999a9b9c);

            //Unknown
            //count
            output.PutUInt32(0x01);
            output.PutUInt16(0x7071);
            output.PutUInt64(0x7273747576777879);

            //Unknown
            output.PutUInt32(0x7a7b7c7d);
            output.PutUInt32(0x7e7f6061);
            output.PutUInt32(0x62636465);
            output.PutUInt32(0x66676869);
            output.PutUInt32(0x6a6b6c6d);
            output.PutUInt32(0x6e6f5051);
            output.PutUInt32(0x52535455);
            output.PutUInt32(0x56575859);
            output.PutUInt32(0x69f4d5e8);
            output.PutUInt32(0x5e5f4041);
        }

        public void Deserialize(IoBuffer input, ISerializationContext context)
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
