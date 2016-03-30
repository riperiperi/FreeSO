using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetDialogResponseCmd : VMNetCommandBodyAbstract
    {
        public byte ResponseCode;
        public string ResponseText;

        public override bool Execute(VM vm)
        {
            VMEntity caller = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            //TODO: check if net user owns caller!
            if (caller == null || caller is VMGameObject || caller.Thread.BlockingDialog == null) return false;
            caller.Thread.BlockingDialog.Responded = true;
            caller.Thread.BlockingDialog.ResponseCode = ResponseCode;
            caller.Thread.BlockingDialog.ResponseText = ResponseText;
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ResponseCode);
            writer.Write(ResponseText);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ResponseCode = reader.ReadByte();
            ResponseText = reader.ReadString();
        }
        #endregion
    }
}
