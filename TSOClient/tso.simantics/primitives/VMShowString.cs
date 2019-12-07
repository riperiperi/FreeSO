using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Utils;
using FSO.SimAntics.Engine;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Primitives
{
    public class VMShowString : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            if (context.StackObject is VMGameObject) return VMPrimitiveExitCode.GOTO_TRUE;
            var operand = (VMShowStringOperand)args;

            var table = context.ScopeResource.Get<STR>(operand.StringTable);
            var avatar = context.StackObject as VMAvatar;

            if (table != null)
            {
                var message = VMDialogHandler.ParseDialogString(context, table.GetString(operand.StringID - 1), table);
                var vm = context.VM;

                var channelID = 0;

                vm.SignalChatEvent(new VMChatEvent(avatar, VMChatEventType.Message, (byte)(channelID & 0x7f), avatar.Name, message));
                if ((channelID & 0x80) == 0) avatar.Message = message;
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMShowStringOperand : VMPrimitiveOperand
    {
        public ushort StringTable { get; set; } = 300;
        public ushort StringID { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                StringTable = io.ReadUInt16();
                StringID = io.ReadUInt16();
            }
        }

        public void Write(byte[] bytes)
        {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(StringTable);
                io.Write(StringID);
            }
        }
        #endregion
    }
}
