/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Utils;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type defines a list of interactions for an object and assigns a BHAV subroutine 
    /// for each interaction. The pie menu labels shown to the user are stored in a TTAs chunk with 
    /// the same ID.
    /// </summary>
    public class TTAB : IffChunk
    {
        public TTABInteraction[] Interactions = new TTABInteraction[0];
        public Dictionary<uint, TTABInteraction> InteractionByIndex = new Dictionary<uint, TTABInteraction>();

        public static float[] AttenuationValues = {
            0, //custom
            0, //none
            0.002f, //low
            0.02f, //medium
            0.1f, //high (falloff entirely in 10 tiles)
            };

        /// <summary>
        /// Reads a TTAB chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a TTAB chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                InteractionByIndex.Clear();
                Interactions = new TTABInteraction[io.ReadUInt16()];
                if (Interactions.Length == 0) return; //no interactions, don't bother reading remainder.
                var version = io.ReadUInt16();
                IOProxy iop;
                if (version != 9 && version != 10) iop = new TTABNormal(io);
                else
                {
                    var compressionCode = io.ReadByte();
                    if (compressionCode != 1) throw new Exception("hey what!!");
                    iop = new TTABFieldEncode(io); //haven't guaranteed that this works, since none of the objects in the test lot use it.
                }
                for (int i = 0; i < Interactions.Length; i++)
                {
                    var result = new TTABInteraction();
                    result.ActionFunction = iop.ReadUInt16();   
                    result.TestFunction = iop.ReadUInt16();
                    result.MotiveEntries = new TTABMotiveEntry[iop.ReadUInt32()];
                    result.Flags = (TTABFlags)iop.ReadUInt32();
                    result.TTAIndex = iop.ReadUInt32();
                    if (version > 6) result.AttenuationCode = iop.ReadUInt32();
                    result.AttenuationValue = iop.ReadFloat();
                    result.AutonomyThreshold = iop.ReadUInt32();
                    result.JoiningIndex = iop.ReadInt32();
                    for (int j = 0; j < result.MotiveEntries.Length; j++)
                    {
                        var motive = new TTABMotiveEntry();
                        if (version > 6) motive.EffectRangeMinimum = iop.ReadInt16();
                        motive.EffectRangeMaximum = iop.ReadInt16();
                        if (version > 6) motive.PersonalityModifier = iop.ReadUInt16();
                        result.MotiveEntries[j] = motive;
                    }
                    if (version > 9)
                    {
                        result.Flags2 = (TSOFlags)iop.ReadUInt32();
                    }
                    Interactions[i] = result;
                    InteractionByIndex.Add(result.TTAIndex, result);
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt16((ushort)Interactions.Length);
                io.WriteUInt16(8); //version. don't save to high version cause we can't write out using the complex io proxy.
                for (int i = 0; i < Interactions.Length; i++)
                {
                    var action = Interactions[i];
                    io.WriteUInt16(action.ActionFunction);
                    io.WriteUInt16(action.TestFunction);
                    io.WriteUInt32((uint)action.MotiveEntries.Length);
                    io.WriteUInt32((uint)action.Flags);
                    io.WriteUInt32(action.TTAIndex);
                    io.WriteUInt32(action.AttenuationCode);
                    io.WriteFloat(action.AttenuationValue);
                    io.WriteUInt32(action.AutonomyThreshold);
                    io.WriteInt32(action.JoiningIndex);
                    for (int j=0; j < action.MotiveEntries.Length; j++)
                    {
                        var mot = action.MotiveEntries[j];
                        io.WriteInt16(mot.EffectRangeMinimum);
                        io.WriteInt16(mot.EffectRangeMaximum);
                        io.WriteUInt16(mot.PersonalityModifier);
                    }
                    //TODO: write out TSOFlags
                }
            }
            return true;
        }

        public void InsertInteraction(TTABInteraction action, int index)
        {
            var newInt = new TTABInteraction[Interactions.Length + 1];
            if (index == -1) index = 0;
            Array.Copy(Interactions, newInt, index); //copy before strings
            newInt[index] = action;
            Array.Copy(Interactions, index, newInt, index + 1, (Interactions.Length - index));
            Interactions = newInt;

            if (!InteractionByIndex.ContainsKey(action.TTAIndex)) InteractionByIndex.Add(action.TTAIndex, action);
        }

        public void DeleteInteraction(int index)
        {
            var action = Interactions[index];
            var newInt = new TTABInteraction[Interactions.Length - 1];
            if (index == -1) index = 0;
            Array.Copy(Interactions, newInt, index); //copy before strings
            Array.Copy(Interactions, index + 1, newInt, index, (Interactions.Length - (index + 1)));
            Interactions = newInt;

            if (InteractionByIndex.ContainsKey(action.TTAIndex)) InteractionByIndex.Remove(action.TTAIndex);
        }
    }

    abstract class IOProxy
    {
        public abstract ushort ReadUInt16();
        public abstract short ReadInt16();
        public abstract int ReadInt32();
        public abstract uint ReadUInt32();
        public abstract float ReadFloat();

        public IoBuffer io;
        public IOProxy(IoBuffer io)
        {
            this.io = io;
        }
    }

   class TTABNormal : IOProxy
    {
        public override ushort ReadUInt16() { return io.ReadUInt16(); }
        public override short ReadInt16() { return io.ReadInt16(); }
        public override int ReadInt32() { return io.ReadInt32(); }
        public override uint ReadUInt32() { return io.ReadUInt32(); }
        public override float ReadFloat() { return io.ReadFloat(); }

        public TTABNormal(IoBuffer io) : base(io) { }
    }

    /// <summary>
    /// Used to read values from field encoded stream.
    /// </summary>
    class TTABFieldEncode : IOProxy
    {
        private byte bitPos = 0;
        private byte curByte = 0;
        static byte[] widths = { 5, 8, 13, 16 };
        static byte[] widths2 = { 6, 11, 21, 32 };

        public void setBytePos(int n)
        {
            io.Seek(SeekOrigin.Begin, n);
            curByte = io.ReadByte();
            bitPos = 0;
        }

        public override ushort ReadUInt16() 
        {
            return (ushort)ReadField(false);
        }

        public override short ReadInt16()
        {
            return (short)ReadField(false);
        }

        public override int ReadInt32()
        {
            return (int)ReadField(true);
        }

        public override uint ReadUInt32()
        {
            return (uint)ReadField(true);
        }

        public override float ReadFloat()
        {
            return (float)ReadField(true);
            //this is incredibly wrong
        }

        private long ReadField(bool big)
        {
            if (ReadBit() == 0) return 0;

            uint code = ReadBits(2);
            byte width = (big)?widths2[code]:widths[code];
            long value = ReadBits(width);
            value |= -(value & (1 << (width-1)));

            return value;
        }

        private uint ReadBits(int n)
        {
            uint total = 0;
            for (int i = 0; i < n; i++)
            {
                total += (uint)(ReadBit() << ((n - i)-1));
            }
            return total;
        }

        private byte ReadBit()
        {
            byte result = (byte)((curByte & (1 << (7 - bitPos))) >> (7 - bitPos));
            if (++bitPos > 7)
            {
                bitPos = 0;
                try
                {
                    curByte = io.ReadByte();
                }
                catch (Exception)
                {
                    curByte = 0; //no more data, read 0
                }
            }
            return result;
        }

        public TTABFieldEncode(IoBuffer io) : base(io) 
        {
            curByte = io.ReadByte();
            bitPos = 0;
        }
    }

    /// <summary>
    /// Represents an interaction in a TTAB chunk.
    /// </summary>
    public class TTABInteraction
    {
        public ushort ActionFunction;
        public ushort TestFunction;
        public TTABMotiveEntry[] MotiveEntries;
        public TTABFlags Flags;
        public uint TTAIndex;
        public uint AttenuationCode;
        public float AttenuationValue;
        public uint AutonomyThreshold;
        public int JoiningIndex;
        public TSOFlags Flags2 = (TSOFlags)0x1f; //allow a lot of things

        public InteractionMaskFlags MaskFlags {
            get {
                return (InteractionMaskFlags)(((int)Flags >> 16) & 0xF);
            }
            set
            {
                Flags = (TTABFlags)(((int)Flags & 0xFFFF) | ((int)value << 16));
            }
        }

        //ALLOW
        public bool AllowVisitors
        {
            get { return (Flags & TTABFlags.AllowVisitors) > 0; }
            set { Flags &= ~(TTABFlags.AllowVisitors); if (value) Flags |= TTABFlags.AllowVisitors; }
        }
        public bool AllowFriends
        {
            get { return (Flags2 & TSOFlags.AllowFriends) > 0; }
            set { Flags2 &= ~(TSOFlags.AllowFriends); if (value) Flags2 |= TSOFlags.AllowFriends; }
        }
        public bool AllowRoommates
        {
            get { return (Flags2 & TSOFlags.AllowRoommates) > 0; }
            set { Flags2 &= ~(TSOFlags.AllowRoommates); if (value) Flags2 |= TSOFlags.AllowRoommates; }
        }
        public bool AllowObjectOwner
        {
            get { return (Flags2 & TSOFlags.AllowObjectOwner) > 0; }
            set { Flags2 &= ~(TSOFlags.AllowObjectOwner); if (value) Flags2 |= TSOFlags.AllowObjectOwner; }
        }
        public bool UnderParentalControl
        {
            get { return (Flags2 & TSOFlags.UnderParentalControl) > 0; }
            set { Flags2 &= ~(TSOFlags.UnderParentalControl); if (value) Flags2 |= TSOFlags.UnderParentalControl; }
        }
        public bool AllowCSRs
        {
            get { return (Flags2 & TSOFlags.AllowCSRs) > 0; }
            set { Flags2 &= ~(TSOFlags.AllowCSRs); if (value) Flags2 |= TSOFlags.AllowCSRs; }
        }
        public bool AllowGhosts
        {
            get { return (Flags2 & TSOFlags.AllowGhost) > 0; }
            set { Flags2 &= ~(TSOFlags.AllowGhost); if (value) Flags2 |= TSOFlags.AllowGhost; }
        }
        public bool AllowCats
        {
            get { return (Flags & TTABFlags.AllowCats) > 0; }
            set { Flags &= ~(TTABFlags.AllowCats); if (value) Flags |= TTABFlags.AllowCats; }
        }
        public bool AllowDogs
        {
            get { return (Flags & TTABFlags.AllowDogs) > 0; }
            set { Flags &= ~(TTABFlags.AllowDogs); if (value) Flags |= TTABFlags.AllowDogs; }
        }

        //FLAGS
        public bool Debug
        {
            get { return (Flags & TTABFlags.Debug) > 0; }
            set { Flags &= ~(TTABFlags.Debug); if (value) Flags |= TTABFlags.Debug; }
        }

        public bool Leapfrog {
            get { return (Flags & TTABFlags.Leapfrog) > 0; }
            set { Flags &= ~(TTABFlags.Leapfrog); if (value) Flags |= TTABFlags.Leapfrog; }
        }
        public bool MustRun
        {
            get { return (Flags & TTABFlags.MustRun) > 0; }
            set { Flags &= ~(TTABFlags.MustRun); if (value) Flags |= TTABFlags.MustRun; }
        }
        public bool AutoFirst
        {
            get { return (Flags & TTABFlags.AutoFirstSelect) > 0; }
            set { Flags &= ~(TTABFlags.AutoFirstSelect); if (value) Flags |= TTABFlags.AutoFirstSelect; }
        }
        public bool RunImmediately
        {
            get { return (Flags & TTABFlags.RunImmediately) > 0; }
            set { Flags &= ~(TTABFlags.RunImmediately); if (value) Flags |= TTABFlags.RunImmediately; }
        }
        public bool AllowConsecutive
        {
            get { return (Flags & TTABFlags.AllowConsecutive) > 0; }
            set { Flags &= ~(TTABFlags.AllowConsecutive); if (value) Flags |= TTABFlags.AllowConsecutive; }
        }


        public bool Carrying
        {
            get { return (MaskFlags & InteractionMaskFlags.AvailableWhenCarrying) > 0; }
            set { MaskFlags &= ~(InteractionMaskFlags.AvailableWhenCarrying); if (value) MaskFlags |= InteractionMaskFlags.AvailableWhenCarrying; }
        }
        public bool Repair
        {
            get { return (MaskFlags & InteractionMaskFlags.IsRepair) > 0; }
            set { MaskFlags &= ~(InteractionMaskFlags.IsRepair); if (value) MaskFlags |= InteractionMaskFlags.IsRepair; }
        }
        public bool AlwaysCheck
        {
            get { return (MaskFlags & InteractionMaskFlags.RunCheckAlways) > 0; }
            set { MaskFlags &= ~(InteractionMaskFlags.RunCheckAlways); if (value) MaskFlags |= InteractionMaskFlags.RunCheckAlways; }
        }
        public bool WhenDead
        {
            get { return (MaskFlags & InteractionMaskFlags.AvailableWhenDead) > 0; }
            set { MaskFlags &= ~(InteractionMaskFlags.AvailableWhenDead); if (value) MaskFlags |= InteractionMaskFlags.AvailableWhenDead; }
        }
    }

    /// <summary>
    /// Represents a motive entry in a TTAB chunk.
    /// </summary>
    public struct TTABMotiveEntry
    {
        public short EffectRangeMinimum;
        public short EffectRangeMaximum;
        public ushort PersonalityModifier;
    }

    public enum TTABFlags
    {
        AllowVisitors = 1, //COVERED, TODO for no TSOFlags? (default to only roomies, unless this flag set)
        Joinable = 1 << 1, //TODO
        RunImmediately = 1 << 2, //COVERED
        AllowConsecutive = 1 << 3, //TODO

        Debug = 1 << 7, //COVERED: only available to roomies for now
        AutoFirstSelect = 1 << 8, //TODO (autonomus first select?)
        Leapfrog = 1 << 9, //COVERED
        MustRun = 1 << 10, //TODO (where would this NOT run?)
        AllowDogs = 1 << 11, //COVERED
        AllowCats = 1 << 12, //COVERED

        TSOAvailableCarrying = 1 << 16, //COVERED
        TSOIsRepair = 1 << 17, //TODO (only available when wear = 0)
        TSORunCheckAlways = 1 << 18, //TODO
        TSOAvailableWhenDead = 1<<19 //COVERED
    }

    public enum TSOFlags
    {
        NonEmpty = 1, //if this is the only flag set, flags aren't empty intentionally. force Owner, Roommates, Friends to on
        AllowObjectOwner = 1 << 1, //COVERED
        AllowRoommates = 1 << 2, //COVERED
        AllowFriends = 1 << 3, //TODO
        AllowVisitors = 1 << 4, //COVERED
        AllowGhost = 1 << 5, //COVERED
        UnderParentalControl = 1 << 6, //TODO: interactions always available
        AllowCSRs = 1 << 7 //COVERED: only available to admins
    }

    public enum InteractionMaskFlags
    {
        AvailableWhenCarrying = 1,
        IsRepair = 1<<1,
        RunCheckAlways = 1 << 2,
        AvailableWhenDead = 1 << 3,
    }
}
