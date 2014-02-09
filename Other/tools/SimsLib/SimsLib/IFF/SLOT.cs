/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace SimsLib.IFF
{
    /// <summary>
    /// WTF does this chunk do? Darren, document!
    /// </summary>
    public class SLOT : AbstractIffChunk
    {
        public SLOTItem[] Slots;

        public override void Read(Iff iff, System.IO.Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var zero = io.ReadUInt32();
                var version = io.ReadUInt32();
                var slotMagic = io.ReadBytes(4);
                var numSlots = io.ReadUInt32();

                Slots = new SLOTItem[numSlots];

                /** The span for version 4 is 34.  The span for version 6 is 54.  The span for version 7 is 58.  The span for version 8 is 62.  The span for version 9 is 66.  The span for version 10 is 70.  **/
                for (var i = 0; i < numSlots; i++)
                {
                    io.Mark();

                    var item = new SLOTItem();
                    item.Type = io.ReadUInt16();
                    item.Offset = new Vector3(
                        io.ReadFloat(),
                        io.ReadFloat(),
                        io.ReadFloat()
                    );

                    var standing = io.ReadInt32();
                    var sitting = io.ReadInt32();
                    var ground = io.ReadInt32();
                    var rsflags = io.ReadInt32();
                    var snaptargetslot = io.ReadInt32();
                    var minproximity = io.ReadInt32();

                    item.Standing = standing;
                    item.Sitting = sitting;
                    item.Ground = ground;
                    item.Rsflags = (SLOTFlags)rsflags;
                    item.SnapTargetSlot = snaptargetslot;
                    item.MinProximity = minproximity;

                    if (version >= 6)
                    {
                        var maxproximity = io.ReadInt32();
                        var optimalproximity = io.ReadInt32();
                        var i9 = io.ReadInt32();
                        var i10 = io.ReadInt32();
                        var gradient = io.ReadFloat();

                        item.MaxProximity = maxproximity;
                        item.OptimalProximity = optimalproximity;
                        item.Gradient = gradient;
                    }

                    if (version >= 7)
                    {
                        var i11 = io.ReadInt32();
                    }

                    if (version >= 8)
                    {
                        var facing = io.ReadInt32();
                        var resolution = io.ReadInt32();
                    }

                    Slots[i] = item;
                }
            }
        }
    }

    [Flags]
    public enum SLOTFlags
    {
        FaceAnywhere = -3,
        FaceTowardsObject = -2,
        FaceAwayFromObject = -1,
        NORTH = 1,
        NORTH_EAST = 2,
        EAST = 4,
        SOUTH_EAST = 8,
        SOUTH = 16,
        SOUTH_WEST = 32,
        WEST = 64,
        NORTH_WEST = 128,
        AllowAnyRotation = 256,
        Absolute = 512,
        FacingAwayFromObject = 1024,
        IgnoreRooms = 2048,
        SnapToDirection = 4096,
        RandomScoring = 8192,
        AllowFailureTrees = 16385,
        AllowDifferentAlts = 32768,
        UseAverageObjectLocation = 65536
    }

    public class SLOTItem
    {
        public ushort Type;
        public Vector3 Offset;
        public int Standing;
        public int Sitting;
        public int Ground;
        public SLOTFlags Rsflags;
        public int SnapTargetSlot;
        public int MinProximity;
        public int MaxProximity = -1;
        public int OptimalProximity = -1;
        public float Gradient;
        public int Facing;
        public int Resolution;
    }
}