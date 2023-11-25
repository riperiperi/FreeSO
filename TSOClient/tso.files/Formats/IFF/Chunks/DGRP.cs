using System;
using System.IO;
using FSO.Files.Utils;
using Microsoft.Xna.Framework;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type collects SPR# and SPR2 resources into a "drawing group" which 
    /// can be used to display one tile of an object from all directions and zoom levels. 
    /// Objects which span across multiple tiles have a separate DGRP chunk for each tile. 
    /// A DGRP chunk always consists of 12 images (one for every direction/zoom level combination), 
    /// which in turn contain info about one or more sprites.
    /// </summary>
    public class DGRP : IffChunk
    {
        public DGRPImage[] Images { get; set; }

        /// <summary>
        /// Gets a DGRPImage instance from this DGRP instance.
        /// </summary>
        /// <param name="direction">The direction the DGRP is facing.</param>
        /// <param name="zoom">Zoom level DGRP is drawn at.</param>
        /// <param name="worldRotation">Current rotation of world.</param>
        /// <returns>A DGRPImage instance.</returns>
        public DGRPImage GetImage(uint direction, uint zoom, uint worldRotation){

            uint rotatedDirection = 0;

            /**LeftFront = 0x10,
            LeftBack = 0x40,
            RightFront = 0x04,
            RightBack = 0x01**/
            int rotateBits = (int)direction << ((int)worldRotation * 2);
            rotatedDirection = (uint)((rotateBits & 255) | (rotateBits >> 8));

            foreach(DGRPImage image in Images)
            {
                if (image.Direction == rotatedDirection && image.Zoom == zoom)
                {
                    return image;
                }
            }
            return null;
        }

        /// <summary>
        /// Reads a DGRP from a stream instance.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream instance holding a DGRP chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var version = io.ReadUInt16();
                uint imageCount = version < 20003 ? io.ReadUInt16() : io.ReadUInt32();
                Images = new DGRPImage[imageCount];

                for (var i = 0; i < imageCount; i++)
                {
                    var image = new DGRPImage(this);
                    image.Read(version, io);
                    Images[i] = image;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteUInt16(20004);
                io.WriteUInt32((uint)Images.Length);

                foreach (var img in Images)
                {
                    img.Write(io);
                }
            }
            return true;
        }
    }

    /// <summary>
    /// A DGRP is made up of multiple DGRPImages,
    /// which are made up of multiple DGRPSprites.
    /// </summary>
    public class DGRPImage 
    {
        private DGRP Parent;
        public uint Direction;
        public uint Zoom;
        public DGRPSprite[] Sprites;

        public DGRPImage(DGRP parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// Reads a DGRPImage from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a DGRPImage.</param>
        public void Read(uint version, IoBuffer io)
        {
            uint spriteCount = 0;
            if (version < 20003){
                spriteCount = io.ReadUInt16();
                Direction = io.ReadByte();
                Zoom = io.ReadByte();
            }else{
                Direction = io.ReadUInt32();
                Zoom = io.ReadUInt32();
                spriteCount = io.ReadUInt32();
            }

            this.Sprites = new DGRPSprite[spriteCount];
            for (var i = 0; i < spriteCount; i++){
                var sprite = new DGRPSprite(Parent);
                sprite.Read(version, io);
                this.Sprites[i] = sprite;
            }
        }

        public void Write(IoWriter io)
        {
            io.WriteUInt32(Direction);
            io.WriteUInt32(Zoom);
            io.WriteUInt32((uint)Sprites.Length);
            foreach (var spr in Sprites)
            {
                spr.Write(io);
            }
        }
    }

    [Flags]
    public enum DGRPSpriteFlags
    {
        Flip = 0x1,
        Unknown = 0x2, //set for end table
        Luminous = 0x4,
        Unknown2 = 0x8,
        Unknown3 = 0x10 //set for end table
    }

    /// <summary>
    /// Makes up a DGRPImage.
    /// </summary>
    public class DGRPSprite : ITextureProvider, IWorldTextureProvider 
    {
        private DGRP Parent;
        public uint SpriteID;
        public uint SpriteFrameIndex;
        public DGRPSpriteFlags Flags;

        public Vector2 SpriteOffset;
        public Vector3 ObjectOffset;

        public bool Flip {
            get { return (Flags & DGRPSpriteFlags.Flip) > 0; }
            set {
                Flags = Flags & (~DGRPSpriteFlags.Flip);
                if (value) Flags |= DGRPSpriteFlags.Flip;
            }
        }

        public bool Luminous
        {
            get { return (Flags & DGRPSpriteFlags.Luminous) > 0; }
            set
            {
                Flags = Flags & (~DGRPSpriteFlags.Luminous);
                if (value) Flags |= DGRPSpriteFlags.Luminous;
            }
        }

        public DGRPSprite(DGRP parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// Reads a DGRPSprite from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a DGRPSprite.</param>
        public void Read(uint version, IoBuffer io)
        {
            if (version < 20003)
            {
                //Unknown ignored "Type" field
                var type = io.ReadUInt16();
                SpriteID = io.ReadUInt16();
                SpriteFrameIndex = io.ReadUInt16();

                var flagsRaw = io.ReadUInt16();
                Flags = (DGRPSpriteFlags)flagsRaw;

                SpriteOffset.X = io.ReadInt16();
                SpriteOffset.Y = io.ReadInt16();

                if (version == 20001)
                {
                    ObjectOffset.Z = io.ReadFloat();
                }
            }
            else
            {
                SpriteID = io.ReadUInt32();
                SpriteFrameIndex = io.ReadUInt32();
                SpriteOffset.X = io.ReadInt32();
                SpriteOffset.Y = io.ReadInt32();
                ObjectOffset.Z = io.ReadFloat();
                Flags = (DGRPSpriteFlags)io.ReadUInt32();
                if (version == 20004)
                {
                    ObjectOffset.X = io.ReadFloat();
                    ObjectOffset.Y = io.ReadFloat();
                }
            }
        }

        public void Write(IoWriter io)
        {
            io.WriteUInt32(SpriteID);
            io.WriteUInt32(SpriteFrameIndex);
            io.WriteInt32((int)SpriteOffset.X);
            io.WriteInt32((int)SpriteOffset.Y);
            io.WriteFloat(ObjectOffset.Z);
            io.WriteUInt32((uint)Flags);
            io.WriteFloat(ObjectOffset.X);
            io.WriteFloat(ObjectOffset.Y);
        }

        /// <summary>
        /// Gets position of this sprite.
        /// </summary>
        /// <returns>A Vector2 instance holding position of this sprite.</returns>
        public Vector2 GetPosition()
        {
            var iff = Parent.ChunkParent;
            var spr2 = iff.Get<SPR2>((ushort)this.SpriteID);
            if (spr2 != null)
            {
                return spr2.Frames[this.SpriteFrameIndex].Position;
            }
            return new Vector2(0, 0);
        }

        #region ITextureProvider Members

        public Microsoft.Xna.Framework.Graphics.Texture2D GetTexture(Microsoft.Xna.Framework.Graphics.GraphicsDevice device){
            var iff = Parent.ChunkParent;
            var spr2 = iff.Get<SPR2>((ushort)this.SpriteID);
            if (spr2 != null){
                return spr2.Frames[this.SpriteFrameIndex].GetTexture(device);
            }
            var spr1 = iff.Get<SPR>((ushort)this.SpriteID);
            if (spr1 != null){
                return spr1.Frames[(int)this.SpriteFrameIndex].GetTexture(device);
            }
            return null;
        }

        #endregion

        #region IWorldTextureProvider Members

        public byte[] GetDepth()
        {
            var iff = Parent.ChunkParent;
            var spr2 = iff.Get<SPR2>((ushort)this.SpriteID);
            if (spr2 != null)
            {
                spr2.Frames[this.SpriteFrameIndex].DecodeIfRequired(true);
                var buf = spr2.Frames[this.SpriteFrameIndex].ZBufferData;
                return buf;
            }
            return null;
        }

        public WorldTexture GetWorldTexture(Microsoft.Xna.Framework.Graphics.GraphicsDevice device)
        {
            var iff = Parent.ChunkParent;
            var spr2 = iff.Get<SPR2>((ushort)this.SpriteID);
            if (spr2 != null)
            {
                return spr2.Frames[this.SpriteFrameIndex].GetWorldTexture(device);
            }
            var spr1 = iff.Get<SPR>((ushort)this.SpriteID);
            if (spr1 != null)
            {
                var result = new WorldTexture();
                result.Pixel = spr1.Frames[(int)this.SpriteFrameIndex].GetTexture(device);
                return result;
            }
            return null;
        }

        public Point GetDimensions()
        {
            var iff = Parent.ChunkParent;
            var spr2 = iff.Get<SPR2>((ushort)this.SpriteID);
            if (spr2 != null)
            {
                var frame = spr2.Frames[this.SpriteFrameIndex];
                return new Point(frame.Width, frame.Height);
            }
            var spr1 = iff.Get<SPR>((ushort)this.SpriteID);
            if (spr1 != null)
            {
                var result = new WorldTexture();
                var frame = spr1.Frames[(int)this.SpriteFrameIndex];
                return new Point(frame.Width, frame.Height);
            }
            return new Point(1, 1);
        }

        #endregion
    }
}
