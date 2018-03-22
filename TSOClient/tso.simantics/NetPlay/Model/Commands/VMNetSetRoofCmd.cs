using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetSetRoofCmd : VMNetCommandBodyAbstract
    {
        public float Pitch;
        public uint Style;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (!vm.TS1 && (caller == null || caller.AvatarState.Permissions < VMTSOAvatarPermissions.Owner)) return false;
            if (Style >= Content.Content.Get().WorldRoofs.Count) return false;
            Pitch = Math.Max(0f, Math.Min(1.25f, Pitch));
            vm.Context.Architecture.SetRoof(Pitch, Style);
            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Pitch);
            writer.Write(Style);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Pitch = reader.ReadSingle();
            Style = reader.ReadUInt32();
        }

        #endregion
    }
}
