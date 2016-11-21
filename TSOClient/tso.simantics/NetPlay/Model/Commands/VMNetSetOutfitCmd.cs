using FSO.SimAntics.Engine.Scopes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSetOutfitCmd : VMNetCommandBodyAbstract
    {
        public uint UID;
        public VMPersonSuits Scope;
        public ulong Outfit;

        public override bool Execute(VM vm)
        {
            VMAvatar avatar = vm.Entities.FirstOrDefault(x => x.PersistID == UID) as VMAvatar;
            if (avatar == null) { return false; }

            switch (Scope)
            {
                case VMPersonSuits.DefaultDaywear:
                    avatar.DefaultSuits.Daywear = Outfit;
                    break;
                case VMPersonSuits.DefaultSleepwear:
                    avatar.DefaultSuits.Sleepwear = Outfit;
                    break;
                case VMPersonSuits.DefaultSwimwear:
                    avatar.DefaultSuits.Swimwear = Outfit;
                    break;
                case VMPersonSuits.DynamicDaywear:
                    avatar.DynamicSuits.Daywear = Outfit;
                    break;
                case VMPersonSuits.DynamicSleepwear:
                    avatar.DynamicSuits.Sleepwear = Outfit;
                    break;
                case VMPersonSuits.DynamicSwimwear:
                    avatar.DynamicSuits.Swimwear = Outfit;
                    break;
                case VMPersonSuits.DynamicCostume:
                    avatar.DynamicSuits.Costume = Outfit;
                    break;
                case VMPersonSuits.DecorationHead:
                    avatar.Decoration.Head = Outfit;
                    break;
                case VMPersonSuits.DecorationBack:
                    avatar.Decoration.Back = Outfit;
                    break;
                case VMPersonSuits.DecorationShoes:
                    avatar.Decoration.Shoes = Outfit;
                    break;
                case VMPersonSuits.DecorationTail:
                    avatar.Decoration.Tail = Outfit;
                    break;
            }

            return true;
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(UID);
            writer.Write((short)Scope);
            writer.Write(Outfit);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            UID = reader.ReadUInt32();
            Scope = (VMPersonSuits)reader.ReadInt16();
            Outfit = reader.ReadUInt64();
        }
    }
}
