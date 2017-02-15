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
using FSO.LotView.Model;
using FSO.SimAntics.Utils;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMBlueprintRestoreCmd : VMNetCommandBodyAbstract
    {
        public byte[] XMLData;
        public short JobLevel = -1;
        public int FloorClipX;
        public int FloorClipY;
        public int FloorClipWidth;
        public int FloorClipHeight;
        public int OffsetX;
        public int OffsetY;
        public int TargetSize;

        public override bool AcceptFromClient { get { return false; } }

        public override bool Execute(VM vm)
        {
            XmlHouseData lotInfo;
            using (var stream = new MemoryStream(XMLData))
            {
                lotInfo = XmlHouseData.Parse(stream);
            }

            vm.SetGlobalValue(11, JobLevel); //set job level before hand 

            var activator = new VMWorldActivator(vm, vm.Context.World);
            activator.FloorClip = new Microsoft.Xna.Framework.Rectangle(FloorClipX, FloorClipY, FloorClipWidth, FloorClipHeight);
            activator.Offset = new Microsoft.Xna.Framework.Point(OffsetX, OffsetY);
            activator.TargetSize = TargetSize;
            var blueprint = activator.LoadFromXML(lotInfo);

            if (VM.UseWorld)
            {
                vm.Context.World.InitBlueprint(blueprint);
                vm.Context.Blueprint = blueprint;
            }

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            if (XMLData == null) writer.Write(0);
            else
            {
                writer.Write(XMLData.Length);
                writer.Write(XMLData);
                writer.Write(JobLevel);

                writer.Write(FloorClipX);
                writer.Write(FloorClipY);
                writer.Write(FloorClipWidth);
                writer.Write(FloorClipHeight);
                writer.Write(OffsetX);
                writer.Write(OffsetY);
                writer.Write(TargetSize);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            XMLData = reader.ReadBytes(length);
            JobLevel = reader.ReadInt16();

            FloorClipX = reader.ReadInt32();
            FloorClipY = reader.ReadInt32();
            FloorClipWidth = reader.ReadInt32();
            FloorClipHeight = reader.ReadInt32();
            OffsetX = reader.ReadInt32();
            OffsetY = reader.ReadInt32();
            TargetSize = reader.ReadInt32();
        }

        #endregion
    }
}
