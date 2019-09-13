using FSO.SimAntics.Model.TSOPlatform;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetChatParamCmd : VMNetCommandBodyAbstract
    {
        public sbyte Pitch;
        public Color Col;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            var state = (VMTSOAvatarState)caller.TSOState;
            Col.A = 255;

            state.ChatTTSPitch = Math.Max((sbyte)-100, Math.Min((sbyte)100, Pitch));
            state.ChatColor = Col;
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return caller != null;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Pitch = reader.ReadSByte();
            Col = new Color(reader.ReadUInt32());
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Pitch);
            writer.Write(Col.PackedValue);
        }
    }
}
