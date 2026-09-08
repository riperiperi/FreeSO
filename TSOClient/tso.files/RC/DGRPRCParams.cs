using FSO.Files.Utils;

namespace FSO.Files.RC
{
    public enum DGRPRCFlags : byte
    {
        /// <summary>
        /// Fixes counters not connecting. Makes some assumptions about corner tiles.
        /// </summary>
        CounterFix = 1,

        /// <summary>
        /// Doesn't generate meshes for flipped sprites at all. Useful for fixing invalid paintings.
        /// </summary>
        DisableFlip = 2,

        /// <summary>
        /// Pushes out backfaces slightly (front-back world direction). Useful for paintings.
        /// </summary>
        BackfaceAdjust = 4,
        
        /// <summary>
        /// Increases the mesh density slightly for depth discontinuities.
        /// </summary>
        IncreaseDensity = 8,

        /// <summary>
        /// Fixes tiles not connecting.
        /// </summary>
        TileFix = 16,

        /// <summary>
        /// Fixes object fences not connecting and appearing to bend the wrong way.
        /// </summary>
        ObjectFenceFix = 32,

        /// <summary>
        /// Clipping for object fences with 6 DGRPs
        /// </summary>
        ObjectFence6Graphic = 64,

        /// <summary>
        /// Counter fix without the corner tiles.
        /// </summary>
        CounterFixBasic = 128,
    }

    public class DGRPRCParams
    {
        public bool[] Rotations = new bool[] { true, true, true, true };
        public bool DoorFix; //depending on subtile, disable certain rotations to fix door.

        public DGRPRCFlags Flags;

        public bool CounterFix
        {
            get
            {
                return Flags.HasFlag(DGRPRCFlags.CounterFix);
            }
            set
            {
                if (value)
                {
                    Flags |= DGRPRCFlags.CounterFix;
                }
                else
                {
                    Flags &= ~DGRPRCFlags.CounterFix;
                }
            }
        } //extrapolate z on sides of counter to the edge of the tile.

        public int StartDGRP;
        public int EndDGRP;
        public bool BlenderTweak;
        public bool Simplify = true;

        public bool InRange(int dgrp)
        {
            return ((StartDGRP == EndDGRP && EndDGRP == 0) || (dgrp >= StartDGRP && dgrp <= EndDGRP));
        }

        public DGRPRCParams() { }

        public DGRPRCParams(DGRPRCFlags flags)
        {
            Flags = flags;
        }

        public DGRPRCParams(DGRPRCParams prev)
        {
            Rotations = [..prev.Rotations];
            DoorFix = prev.DoorFix;
            Flags = prev.Flags;
            StartDGRP = prev.StartDGRP;
            EndDGRP = prev.EndDGRP;
            BlenderTweak = prev.BlenderTweak;
            Simplify = prev.Simplify;
        }

        public DGRPRCParams(IoBuffer io, int version)
        {
            Rotations = new bool[4];
            for (int i = 0; i < 4; i++) Rotations[i] = io.ReadByte() > 0;
            DoorFix = io.ReadByte() > 0;
            Flags = (DGRPRCFlags)io.ReadByte();
            StartDGRP = io.ReadInt32();
            EndDGRP = io.ReadInt32();
            BlenderTweak = io.ReadByte() > 0;
            Simplify = io.ReadByte() > 0;
        }

        public void Save(IoWriter io)
        {
            foreach (var rotation in Rotations) io.WriteByte((byte)(rotation ? 1 : 0));
            io.WriteByte((byte)(DoorFix ? 1 : 0));
            io.WriteByte((byte)(Flags));
            io.WriteInt32(StartDGRP);
            io.WriteInt32(EndDGRP);
            io.WriteByte((byte)(BlenderTweak ? 1 : 0));
            io.WriteByte((byte)(Simplify ? 1 : 0));
        }
    }
}
