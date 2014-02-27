using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.simantics.engine;
using tso.files.utils;
using tso.simantics.model;
using tso.files.formats.iff.chunks;

namespace tso.simantics.primitives
{
    public class VMRefresh : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context){
            var operand = context.GetCurrentOperand<VMRefreshOperand>();
            Trace("refresh: ");
            VMEntity target = null;
            switch (operand.TargetObject)
            {
                case 0:
                    target = context.Caller;
                    break;
                case 1:
                    target = context.StackObject;
                    break;
            }

            switch (operand.RefreshType)
            {
                case 0: //graphic
                    if (target.GetType() == typeof(VMGameObject))
                    {
                        var TargObj = (VMGameObject)target;
                        TargObj.RefreshGraphic();
                    }
                    break;
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRefreshOperand : VMPrimitiveOperand
    {
        public short TargetObject;
        public short RefreshType;

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                TargetObject = io.ReadInt16();
                RefreshType = io.ReadInt16();
            }
        }
        #endregion
    }
}
