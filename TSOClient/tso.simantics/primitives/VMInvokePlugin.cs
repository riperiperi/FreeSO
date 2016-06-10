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
        public static readonly HashSet<uint> IdleEvents = new HashSet<uint>()
        {
            0x2a6356a0,
            0x4a5be8ab,
            0xea47ae39
        };

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            //this probably shouldn't use an AsyncWaitState. I can see this having its own state.
            var operand = (VMInvokePluginOperand)args;

            var localEvt = context.Locals[operand.EventLocal];
            if (localEvt < 0)
            {
                if (context.VM.EODHost != null) context.VM.EODHost.ForceDisconnectObj(context.Caller);
                context.Thread.EODConnection = null;
            }

            if (context.Thread.EODConnection != null)
            {
                if (context.VM.EODHost != null) context.VM.EODHost.SimanticsDeliver(localEvt, context.Caller);

                var eodState = context.Thread.EODConnection;
                while (eodState.Events.Count > 0)
                {
                    //pop off an event.
                    var evt = eodState.Events[0];
                    eodState.Events.RemoveAt(0);
                    if (evt.Code < 0) continue;
                    context.Locals[operand.EventLocal] = evt.Code;
                    for (int i = 0; i < evt.Data.Length; i++)
                    {
                        context.Thread.TempRegisters[i] = evt.Data[i];
                    }
                    return VMPrimitiveExitCode.GOTO_FALSE;
                }

                if (eodState.Ended)
                {
                    context.Thread.EODConnection = null;
                    return VMPrimitiveExitCode.GOTO_TRUE;
                }
            } else if (context.Locals[operand.EventLocal] == -2) {
                //not connected. initiate connection.
                var objID = context.Locals[operand.ObjectLocal];
                var personID = context.Locals[operand.PersonLocal];

                if (operand.Joinable)
                {
                    //does the target object exist? does it have a server already? make one.
                    var serverObj = context.VM.GetObjectById(objID);
                    if (serverObj == null) return VMPrimitiveExitCode.GOTO_TRUE; //no server
                    if (serverObj.Thread.EODConnection == null)
                    {
                        //connect the server object

                        serverObj.Thread.EODConnection = new VMEODPluginThreadState()
                        {
                            ObjectID = objID,
                            AvatarID = 0,
                            Joinable = true,
                            Ended = false
                        };
                        if (context.VM.IsServer)
                        {
                            context.VM.EODHost.Connect(operand.PluginID, serverObj,
                                serverObj,
                                null,
                                true,
                                context.VM
                                );
                        }
                    }
                }

                context.Thread.EODConnection = new VMEODPluginThreadState()
                {
                    ObjectID = objID,
                    AvatarID = personID,
                    Joinable = operand.Joinable,
                    Ended = false
                };
                if (context.VM.IsServer)
                {
                    context.VM.EODHost.Connect(operand.PluginID, context.Caller,
                        context.VM.GetObjectById(objID),
                        (VMAvatar)context.VM.GetObjectById(personID),
                        operand.Joinable,
                        context.VM
                        );
                }

            } else
            {
                return VMPrimitiveExitCode.GOTO_TRUE;
            }

            context.Locals[operand.EventLocal] = 0;
            if (IdleEvents.Contains(operand.PluginID))
            {
                context.Thread.TempRegisters[0] = 0;
                return VMPrimitiveExitCode.GOTO_FALSE;
            }
            else return VMPrimitiveExitCode.CONTINUE_NEXT_TICK;
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
        //dancefloor: 0x4a5be8ab
        //pizza: 0xea47ae39

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
