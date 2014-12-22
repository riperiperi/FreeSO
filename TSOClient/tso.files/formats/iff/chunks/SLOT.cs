/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.utils;
using Microsoft.Xna.Framework;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This format isn't documented on the wiki! Thanks, Darren!
    /// </summary>
    public class SLOT : IffChunk
    {
        //public SLOTItem[] Slots;
        public Dictionary<ushort, List<SLOTItem>> Slots;

        public override void Read(Iff iff, System.IO.Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN)){
                var zero = io.ReadUInt32();
                var version = io.ReadUInt32();
                var slotMagic = io.ReadBytes(4);
                var numSlots = io.ReadUInt32();

                Slots = new Dictionary<ushort, List<SLOTItem>>();

                /** The span for version 4 is 34.  The span for version 6 is 54.  The span for version 7 is 58.  The span for version 8 is 62.  The span for version 9 is 66.  The span for version 10 is 70.  **/
                for (var i = 0; i < numSlots; i++){
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
                        

                        item.MaxProximity = maxproximity;
                        item.OptimalProximity = optimalproximity;
                    }

                    if (version >= 7) item.Gradient = io.ReadFloat();

                    if (version >= 8) item.Height = io.ReadInt32();

                    if (version >= 9) item.Facing = io.ReadInt32();

                    if (version >= 10) item.Resolution = io.ReadInt32();

                    if (!Slots.ContainsKey(item.Type)) Slots.Add(item.Type, new List<SLOTItem>());
                    Slots[item.Type].Add(item);
                }
            }
        }
    }

    [Flags]
    public enum SLOTFlags : int
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
        RandomScoring=8192,
        AllowFailureTrees = 16385,
        AllowDifferentAlts=32768,
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
        public int Height;
    }
}
