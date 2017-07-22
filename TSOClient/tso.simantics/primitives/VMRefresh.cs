/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.Engine;
using FSO.Files.Utils;
using FSO.SimAntics.Model;
using FSO.Files.Formats.IFF.Chunks;
using System.IO;

namespace FSO.SimAntics.Primitives
{
    public class VMRefresh : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMRefreshOperand)args;
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
                    if (target is VMGameObject)
                    {
                        var TargObj = (VMGameObject)target;
                        TargObj.RefreshGraphic();
                    }
                    break;
                case 1: //light
                    context.VM.Context.RefreshLighting(context.VM.Context.GetObjectRoom(target), true, new HashSet<ushort>());
                    if (target is VMGameObject) ((VMGameObject)target).RefreshLight();
                    break;
                case 2: //area contribution
                    context.VM.Context.RefreshRoomScore(context.VM.Context.GetObjectRoom(target));
                    break;
            }

            return VMPrimitiveExitCode.GOTO_TRUE;
        }
    }

    public class VMRefreshOperand : VMPrimitiveOperand
    {
        public short TargetObject { get; set; }
        public short RefreshType { get; set; }

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN)){
                TargetObject = io.ReadInt16();
                RefreshType = io.ReadInt16();
            }
        }

        public void Write(byte[] bytes) {
            using (var io = new BinaryWriter(new MemoryStream(bytes)))
            {
                io.Write(TargetObject);
                io.Write(RefreshType);
            }
        }
        #endregion
    }
}
