using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMGenericDialogCommand : VMNetCommandBodyAbstract
    {
        public override bool AcceptFromClient { get { return false; } }

        public string Title;
        public string Message;
        public override bool Execute(VM vm)
        {
            vm.SignalDialog(new SimAntics.Model.VMDialogInfo
            {
                Title = Title,
                Message = Message,
                Block = false,
            });
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Title);
            writer.Write(Message);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Title = reader.ReadString();
            Message = reader.ReadString();
        }
        #endregion
    }
}
