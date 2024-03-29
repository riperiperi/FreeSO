﻿using System.IO;
using FSO.Files.Utils;
using FSO.Common.Content;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Represents an appearance for a model.
    /// </summary>
    public class Appearance
    {
        public string Name;

        public uint ThumbnailTypeID;
        public uint ThumbnailFileID;
        public AppearanceBinding[] Bindings;

        //bcf values
        public int Type;
        public int Zero;

        public BCF ParentBCF;

        /// <summary>
        /// Gets the ContentID instance for this appearance.
        /// </summary>
        public ContentID ThumbnailID
        {
            get
            {
                return new ContentID(ThumbnailTypeID, ThumbnailFileID);
            }
        }


        public void ReadBCF(BCFReadProxy io)
        {
            Name = io.ReadPascalString();
            Type = io.ReadInt32();
            Zero = io.ReadInt32();

            var numBindings = io.ReadUInt32();
            Bindings = new AppearanceBinding[numBindings];

            for (var i = 0; i < numBindings; i++)
            {
                //bindings are included verbatim here.
                var bnd = new Binding();
                bnd.Bone = io.ReadPascalString();
                bnd.MeshName = io.ReadPascalString();
                bnd.CensorFlagBits = io.ReadInt32();
                bnd.Zero = io.ReadInt32();

                Bindings[i] = new AppearanceBinding
                {
                    RealBinding = bnd
                };
            }
        }

        public void WriteBCF(BCFWriteProxy io)
        {
            io.WritePascalString(Name);
            io.WriteInt32(Type);
            io.WriteInt32(Zero);

            io.WriteUInt32((uint)Bindings.Length);
            foreach (var binding in Bindings)
            {
                io.WritePascalString(binding.RealBinding.Bone);
                io.WritePascalString(binding.RealBinding.MeshName);
                io.WriteInt32(binding.RealBinding.CensorFlagBits);
                io.WriteInt32(binding.RealBinding.Zero);
            }
        }

        /// <summary>
        /// Reads an appearance from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding an appearance.</param>
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();

                ThumbnailFileID = io.ReadUInt32();
                ThumbnailTypeID = io.ReadUInt32();

                var numBindings = io.ReadUInt32();
                Bindings = new AppearanceBinding[numBindings];

                for (var i = 0; i < numBindings; i++)
                {
                    Bindings[i] = new AppearanceBinding 
                    {
                        FileID = io.ReadUInt32(),
                        TypeID = io.ReadUInt32()
                    };
                }
            }
        }

        public void Write(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream))
            {
                io.WriteUInt32(1);
                io.WriteUInt32(ThumbnailFileID);
                io.WriteUInt32(ThumbnailTypeID);

                io.WriteUInt32((uint)Bindings.Length);
                foreach (var binding in Bindings)
                {
                    io.WriteUInt32(binding.FileID);
                    io.WriteUInt32(binding.TypeID);
                }
            }
        }
    }

    /// <summary>
    /// TypeID and FileID for a binding pointed to by an appearance.
    /// </summary>
    public class AppearanceBinding
    {
        public uint TypeID;
        public uint FileID;
        public Binding RealBinding;
    }
}
