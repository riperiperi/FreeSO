using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Engine;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Marshals.Threads
{
    public class VMRoutingFrameMarshal : VMStackFrameMarshal
    {
        public VMRoomPortal[] Rooms;
        public VMRoomPortal CurrentPortal; //NULLable

        public Point[] WalkTo; //NULLable
        public double WalkDirection;
        public double TargetDirection;
        public bool IgnoreRooms;

        public VMRoutingFrameState State;
        public int PortalTurns;
        public int WaitTime;
        public int Timeout;
        public int Retries;

        public bool AttemptedChair;
        public float TurnTweak;
        public int TurnFrames;

        public int MoveTotalFrames;
        public int MoveFrames;
        public int Velocity;

        public bool CallFailureTrees;

        public VMRoomPortal[] IgnoredRooms;
        public short[] AvatarsToConsider;

        public LotTilePos PreviousPosition;
        public LotTilePos CurrentWaypoint;

        public bool RoomRouteInvalid;
        public SLOTItem Slot; //NULLable
        public short Target; //object id
        public VMFindLocationResultMarshal[] Choices; //NULLable
        public VMFindLocationResultMarshal CurRoute; //NULLable

        public short LastWalkStyle = -1;

        public VMRoutingFrameMarshal() { }
        public VMRoutingFrameMarshal(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            var roomN = reader.ReadInt32();
            Rooms = new VMRoomPortal[roomN];
            for (int i=0; i< roomN; i++) Rooms[i] = new VMRoomPortal(reader);

            if (reader.ReadBoolean()) CurrentPortal = new VMRoomPortal(reader);

            var wtLen = reader.ReadInt32();
            if (wtLen > -1)
            {
                WalkTo = new Point[wtLen];
                for (int i = 0; i < wtLen; i++) WalkTo[i] = new Point(reader.ReadInt32(), reader.ReadInt32());
            }

            WalkDirection = reader.ReadDouble();
            TargetDirection = reader.ReadDouble();
            IgnoreRooms = reader.ReadBoolean();

            State = (VMRoutingFrameState)reader.ReadByte();
            PortalTurns = reader.ReadInt32();
            WaitTime = reader.ReadInt32();
            Timeout = reader.ReadInt32();
            Retries = reader.ReadInt32();

            AttemptedChair = reader.ReadBoolean();
            TurnTweak = reader.ReadSingle();
            TurnFrames = reader.ReadInt32();

            MoveTotalFrames = reader.ReadInt32();
            MoveFrames = reader.ReadInt32();
            Velocity = reader.ReadInt32();

            CallFailureTrees = reader.ReadBoolean();

            var igrN = reader.ReadInt32();
            IgnoredRooms = new VMRoomPortal[igrN];
            for (int i = 0; i < igrN; i++) IgnoredRooms[i] = new VMRoomPortal(reader);

            var avaN = reader.ReadInt32();
            AvatarsToConsider = new short[avaN];
            for (int i = 0; i < avaN; i++) AvatarsToConsider[i] = reader.ReadInt16();

            PreviousPosition.Deserialize(reader);
            CurrentWaypoint.Deserialize(reader);

            RoomRouteInvalid = reader.ReadBoolean();

            if (reader.ReadBoolean()) {
                Slot = SLOTItemSerializer.Deserialize(reader);
            }
            Target = reader.ReadInt16();

            var chLen = reader.ReadInt32();
            if (chLen > -1)
            {
                Choices = new VMFindLocationResultMarshal[chLen];
                for (int i = 0; i < chLen; i++)
                {
                    Choices[i] = new VMFindLocationResultMarshal();
                    Choices[i].Deserialize(reader);
                }
            }

            if (reader.ReadBoolean())
            {
                CurRoute = new VMFindLocationResultMarshal();
                CurRoute.Deserialize(reader);
            }

            if (Version > 29)
            {
                LastWalkStyle = reader.ReadInt16();
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);

            writer.Write(Rooms.Length);
            foreach (var item in Rooms) item.SerializeInto(writer);
            writer.Write(CurrentPortal != null);
            if (CurrentPortal != null) CurrentPortal.SerializeInto(writer);

            writer.Write((WalkTo == null) ? -1 : WalkTo.Length);
            if (WalkTo != null)
            {
                foreach (var item in WalkTo)
                {
                    writer.Write(item.X);
                    writer.Write(item.Y);
                }
            }

            writer.Write(WalkDirection);
            writer.Write(TargetDirection);
            writer.Write(IgnoreRooms);

            writer.Write((byte)State);
            writer.Write(PortalTurns);
            writer.Write(WaitTime);
            writer.Write(Timeout);
            writer.Write(Retries);

            writer.Write(AttemptedChair);
            writer.Write(TurnTweak);
            writer.Write(TurnFrames);

            writer.Write(MoveTotalFrames);
            writer.Write(MoveFrames);
            writer.Write(Velocity);

            writer.Write(CallFailureTrees);

            writer.Write(IgnoredRooms.Length);
            foreach (var item in IgnoredRooms) item.SerializeInto(writer);
            writer.Write(AvatarsToConsider.Length);
            foreach (var item in AvatarsToConsider) writer.Write(item);

            PreviousPosition.SerializeInto(writer);
            CurrentWaypoint.SerializeInto(writer);
            writer.Write(RoomRouteInvalid);

            writer.Write(Slot != null);
            if (Slot != null) SLOTItemSerializer.SerializeInto(Slot, writer);
            writer.Write(Target);

            writer.Write((Choices == null) ? -1 : Choices.Length);
            if (Choices != null)
            {
                foreach (var item in Choices) item.SerializeInto(writer);
            }
            writer.Write(CurRoute != null);
            if (CurRoute != null) CurRoute.SerializeInto(writer);

            writer.Write(LastWalkStyle);
        }
    }

    public static class SLOTItemSerializer
    {

        public static SLOTItem Deserialize(BinaryReader reader)
        {
            var result = new SLOTItem();
            result.Type = reader.ReadUInt16();
            result.Offset = new Vector3();
            result.Offset.X = reader.ReadSingle();
            result.Offset.Y = reader.ReadSingle();
            result.Offset.Z = reader.ReadSingle();

            result.Standing = reader.ReadInt32();
            result.Sitting = reader.ReadInt32();
            result.Ground = reader.ReadInt32();
            result.Rsflags = (SLOTFlags)reader.ReadInt32();
            result.SnapTargetSlot = reader.ReadInt32();
            result.MinProximity = reader.ReadInt32();
            result.MaxProximity = reader.ReadInt32();
            result.OptimalProximity = reader.ReadInt32();
            result.Gradient = reader.ReadSingle();
            result.Facing = (SLOTFacing)reader.ReadInt32();
            result.Resolution = reader.ReadInt32();
            result.Height = reader.ReadInt32();
            return result;
        }

        public static void SerializeInto(SLOTItem item, BinaryWriter writer)
        {
            writer.Write(item.Type);
            writer.Write(item.Offset.X);
            writer.Write(item.Offset.Y);
            writer.Write(item.Offset.Z);

            writer.Write(item.Standing);
            writer.Write(item.Sitting);
            writer.Write(item.Ground);
            writer.Write((int)item.Rsflags);
            writer.Write(item.SnapTargetSlot);
            writer.Write(item.MinProximity);
            writer.Write(item.MaxProximity);
            writer.Write(item.OptimalProximity);
            writer.Write(item.Gradient);
            writer.Write((int)item.Facing);
            writer.Write(item.Resolution);
            writer.Write(item.Height);
        }
    }

    public class VMFindLocationResultMarshal : VMSerializable
    {
        public float RadianDirection;
        public LotTilePos Position;
        public double Score;
        public bool FaceAnywhere;
        public short Chair;
        public SLOTFlags RouteEntryFlags;

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(RadianDirection);
            Position.SerializeInto(writer);
            writer.Write(Score);
            writer.Write(FaceAnywhere);
            writer.Write(Chair);
            writer.Write((int)RouteEntryFlags);
        }

        public void Deserialize(BinaryReader reader)
        {
            RadianDirection = reader.ReadSingle();
            Position = new LotTilePos();
            Position.Deserialize(reader);
            Score = reader.ReadDouble();
            FaceAnywhere = reader.ReadBoolean();
            Chair = reader.ReadInt16();
            RouteEntryFlags = (SLOTFlags)reader.ReadInt32();
        }
    }
}
