/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.NetPlay.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Model
{
    public struct VMRoomInfo
    {
        public List<VMRoomPortal> Portals;
        public List<VMRoomPortal> WindowPortals;
        public List<VMEntity> Entities;

        public List<VMObstacle> DynamicObstacles;
        public VMObstacleSet StaticObstacles;
        public RoomLighting Light;
        public VMRoom Room;
    }

    public struct VMRoom
    {
        public ushort RoomID;
        public ushort LightBaseRoom;
        public sbyte Floor;

        public bool IsOutside;
        public ushort Area;
        public bool IsPool;
        public bool Unroutable;

        public HashSet<ushort> AdjRooms; //all adjacent rooms, for DFS traversal of light
        public List<VMObstacle> WallObs;
        public List<VMObstacle> RoomObs;
        public VMObstacleSet RoutingObstacles;
        public List<Vector2[]> WallLines;
        public List<Vector2[]> FenceLines;
        public Rectangle Bounds;

        public List<ushort> SupportRooms;
    }

    public class VMRoomPortal : VMSerializable {
        public short ObjectID;
        public ushort TargetRoom;

        public VMRoomPortal(short obj, ushort target)
        {
            ObjectID = obj;
            TargetRoom = target;
        }

        public VMRoomPortal(BinaryReader reader)
        {
            Deserialize(reader);
        }

        public static bool operator ==(VMRoomPortal c1, VMRoomPortal c2) //are these necessary?
        {
            return equals(c1, c2);
        }

        public static bool operator !=(VMRoomPortal c1, VMRoomPortal c2)
        {
            return !equals(c1, c2);
        }

        private static bool equals(VMRoomPortal c1, VMRoomPortal c2)
        {
            if ((object)c1 == (object)c2) return true;
            if ((object)c1 == null || (object)c2 == null) return false;
            return c1.ObjectID == c2.ObjectID && c1.TargetRoom == c2.TargetRoom;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VMRoomPortal)) return false;
            return equals(this, (VMRoomPortal)obj);
        }

        public override int GetHashCode()
        {
            return ObjectID+TargetRoom;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectID);
            writer.Write(TargetRoom);
        }

        public void Deserialize(BinaryReader reader)
        {
            ObjectID = reader.ReadInt16();
            TargetRoom = reader.ReadUInt16();
        }
    }
}
