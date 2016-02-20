/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetInteractionCancelCmd : VMNetCommandBodyAbstract
    {
        public ushort ActionUID;
        public short CallerID;

        public override bool Execute(VM vm)
        {
            VMEntity caller = vm.GetObjectById(CallerID);
            //TODO: check if net user owns caller!
            if (caller == null) return false;

            var interaction = caller.Thread.Queue.FirstOrDefault(x => x.UID == ActionUID);
            if (interaction != null)
            {
                interaction.Cancelled = true;
                //cancel any idle parents after this interaction
                var index = caller.Thread.Queue.IndexOf(interaction);

                if (interaction.Mode == Engine.VMQueueMode.ParentIdle)
                {
                    for (int i = index + 1; i < caller.Thread.Queue.Count; i++)
                    {
                        if (caller.Thread.Queue[i].Mode == Engine.VMQueueMode.ParentIdle)
                        {
                            if (interaction.Mode == Engine.VMQueueMode.ParentIdle) caller.Thread.Queue.RemoveAt(i--);
                            else
                            {
                                caller.Thread.Queue[i].Cancelled = true;
                                caller.Thread.Queue[i].Priority = 0;
                            }
                        }
                        else if (caller.Thread.Queue[i].Mode == Engine.VMQueueMode.ParentExit)
                        {
                            caller.Thread.Queue[i].Cancelled = true;
                            caller.Thread.Queue[i].Priority = 0;
                        }
                        //parent exit needs to "appear" like it is cancelled.
                    }
                }

                if (caller.Thread.Queue[0] != interaction && interaction.Mode == Engine.VMQueueMode.Normal)
                {
                    caller.Thread.Queue.Remove(interaction);
                }
                else
                {
                    caller.SetFlag(VMEntityFlags.InteractionCanceled, true);
                    caller.Thread.Queue[0].Priority = 0;
                }
            }

            return true;

        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ActionUID);
            writer.Write(CallerID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ActionUID = reader.ReadUInt16();
            CallerID = reader.ReadInt16();
        }

        #endregion
    }
}
