using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Primitives;
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
            if (ResponseText.Length > 32) ResponseText = ResponseText.Substring(0, 32);

            VMEntity caller = vm.Entities.FirstOrDefault(x => x.PersistID == ActorUID);
            //TODO: check if net user owns caller!
            if (caller == null || caller is VMGameObject || 
                caller.Thread.BlockingState == null || !(caller.Thread.BlockingState is VMDialogResult)) return false;
            var state = (VMDialogResult)caller.Thread.BlockingState;
            state.Responded = true;
            state.ResponseCode = ResponseCode;
            state.ResponseText = ResponseText;
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            if (ResponseText.Length > 32) ResponseText = ResponseText.Substring(0, 32);
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
