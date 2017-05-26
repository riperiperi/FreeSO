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
using FSO.Common.Utils;
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

        public void ReadBCF(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                Name = io.ReadPascalString();
                var type = io.ReadInt32();
                var zero = io.ReadInt32();

                var numBindings = io.ReadUInt32();
                Bindings = new AppearanceBinding[numBindings];

                for (var i = 0; i < numBindings; i++)
                {
                    //bindings are included verbatim here.
                    var bnd = new Binding();
                    bnd.Bone = io.ReadPascalString();
                    bnd.MeshName = io.ReadPascalString();
                    io.ReadInt32();
                    io.ReadInt32();

                    Bindings[i] = new AppearanceBinding
                    {
                        RealBinding = bnd
                    };
                }
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
