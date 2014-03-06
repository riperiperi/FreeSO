using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TSO.Files.utils;
using TSO.Common.content;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Hand groups collect together the various hand gesture appearances of the game. 
    /// There are always exactly 18 appearances referenced in a hand group (3 skin colors × 2 hands × 3 gestures).
    /// </summary>
    public class HandGroup
    {
        private uint m_Version;
        public HandSet LightSkin = new HandSet();
        public HandSet MediumSkin = new HandSet();
        public HandSet DarkSkin = new HandSet();

        /// <summary>
        /// Reads a Handgroup from a supplied Stream.
        /// </summary>
        /// <param name="Str">The Stream to read from.</param>
        public void Read(Stream Str)
        {
            using(IoBuffer IOBuf = new IoBuffer(Str))
            {
                m_Version = IOBuf.ReadUInt32();

                LightSkin.LeftHand.Idle.FileID = IOBuf.ReadUInt32();
                LightSkin.LeftHand.Idle.TypeID = IOBuf.ReadUInt32();
                LightSkin.LeftHand.Fist.FileID = IOBuf.ReadUInt32();
                LightSkin.LeftHand.Fist.TypeID = IOBuf.ReadUInt32();
                LightSkin.LeftHand.Pointing.FileID = IOBuf.ReadUInt32();
                LightSkin.LeftHand.Pointing.TypeID = IOBuf.ReadUInt32();

                LightSkin.RightHand.Idle.FileID = IOBuf.ReadUInt32();
                LightSkin.RightHand.Idle.TypeID = IOBuf.ReadUInt32();
                LightSkin.RightHand.Fist.FileID = IOBuf.ReadUInt32();
                LightSkin.RightHand.Fist.TypeID = IOBuf.ReadUInt32();
                LightSkin.RightHand.Pointing.FileID = IOBuf.ReadUInt32();
                LightSkin.RightHand.Pointing.TypeID = IOBuf.ReadUInt32();

                MediumSkin.LeftHand.Idle.FileID = IOBuf.ReadUInt32();
                MediumSkin.LeftHand.Idle.TypeID = IOBuf.ReadUInt32();
                MediumSkin.LeftHand.Fist.FileID = IOBuf.ReadUInt32();
                MediumSkin.LeftHand.Fist.TypeID = IOBuf.ReadUInt32();
                MediumSkin.LeftHand.Pointing.FileID = IOBuf.ReadUInt32();
                MediumSkin.LeftHand.Pointing.TypeID = IOBuf.ReadUInt32();

                MediumSkin.RightHand.Idle.FileID = IOBuf.ReadUInt32();
                MediumSkin.RightHand.Idle.TypeID = IOBuf.ReadUInt32();
                MediumSkin.RightHand.Fist.FileID = IOBuf.ReadUInt32();
                MediumSkin.RightHand.Fist.TypeID = IOBuf.ReadUInt32();
                MediumSkin.RightHand.Pointing.FileID = IOBuf.ReadUInt32();
                MediumSkin.RightHand.Pointing.TypeID = IOBuf.ReadUInt32();

                DarkSkin.LeftHand.Idle.FileID = IOBuf.ReadUInt32();
                DarkSkin.LeftHand.Idle.TypeID = IOBuf.ReadUInt32();
                DarkSkin.LeftHand.Fist.FileID = IOBuf.ReadUInt32();
                DarkSkin.LeftHand.Fist.TypeID = IOBuf.ReadUInt32();
                DarkSkin.LeftHand.Pointing.FileID = IOBuf.ReadUInt32();
                DarkSkin.LeftHand.Pointing.TypeID = IOBuf.ReadUInt32();

                DarkSkin.RightHand.Idle.FileID = IOBuf.ReadUInt32();
                DarkSkin.RightHand.Idle.TypeID = IOBuf.ReadUInt32();
                DarkSkin.RightHand.Fist.FileID = IOBuf.ReadUInt32();
                DarkSkin.RightHand.Fist.TypeID = IOBuf.ReadUInt32();
                DarkSkin.RightHand.Pointing.FileID = IOBuf.ReadUInt32();
                DarkSkin.RightHand.Pointing.TypeID = IOBuf.ReadUInt32();
            }
        }
    }

    public class HandSet
    {
        public Hand LeftHand = new Hand();
        public Hand RightHand = new Hand();
    }

    public class Hand
    {
        public Gesture Idle = new Gesture();
        public Gesture Fist = new Gesture();
        public Gesture Pointing = new Gesture();
    }

    public class Gesture
    {
        public uint FileID;
        public uint TypeID;

        public ContentID ID
        {
            get { return new ContentID(TypeID, FileID); }
        }
    }
}
