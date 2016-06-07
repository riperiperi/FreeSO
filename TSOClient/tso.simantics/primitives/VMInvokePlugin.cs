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
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.Engine.Scopes;

namespace FSO.SimAntics.Primitives
{
    public class VMInvokePlugin : VMPrimitiveHandler
    {
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var operand = (VMInvokePluginOperand)args;

            if (context.Thread.BlockingState != null && context.Thread.BlockingState is VMEODPluginThreadState)
            {
                var eodState = (VMEODPluginThreadState)context.Thread.BlockingState;
                if (eodState.Events.Count > 0)
                {
                    //pop off an event.
                    var evt = eodState.Events[0];
                    eodState.Events.RemoveAt(0);
                    context.Locals[operand.EventLocal] = evt.Code;
                    for (int i = 0; i < evt.Data.Length; i++)
                    {
                        context.Thread.TempRegisters[i] = evt.Data[i];
                    }
                    return VMPrimitiveExitCode.GOTO_FALSE;
                }
                else if (eodState.Ended)
                {
                    context.Thread.BlockingState = null;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
            } else {
                //not connected. initiate connection.
                var objID = context.Locals[operand.ObjectLocal];
                var personID = context.Locals[operand.PersonLocal];
                if (context.VM.IsServer)
                {
                    context.VM.EODHost.Connect(operand.PluginID, context.Caller,
                        context.VM.GetObjectById(objID),
                        (VMAvatar)context.VM.GetObjectById(personID),
                        operand.Joinable,
                        context.VM
                        );
                }
                context.Thread.BlockingState = new VMEODPluginThreadState()
                {
                    ObjectID = objID,
                    AvatarID = personID,
                    Joinable = operand.Joinable,
                    Ended = false
                };
            }

            context.Thread.TempRegisters[0] = 0;
            return VMPrimitiveExitCode.GOTO_FALSE;
        }
    }

    public class VMInvokePluginOperand : VMPrimitiveOperand 
    {
        public byte PersonLocal;
        public byte ObjectLocal;
        public byte EventLocal; //target of event id. values go in temp0
        public bool Joinable;

        public uint PluginID;
        //sign: 0x2a6356a0
        //pizzamakerplugin: 57 174 71 234

        #region VMPrimitiveOperand Members
        public void Read(byte[] bytes)
        {
            using (var io = IoBuffer.FromBytes(bytes, ByteOrder.LITTLE_ENDIAN))
            {
                PersonLocal = io.ReadByte();
                ObjectLocal = io.ReadByte();
                EventLocal = io.ReadByte();
                Joinable = io.ReadByte()>0;

                PluginID = io.ReadUInt32();
            }
        }

        public void Write(byte[] bytes) { }
        #endregion
    }
}
