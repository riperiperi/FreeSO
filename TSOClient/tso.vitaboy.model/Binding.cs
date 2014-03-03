using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Common.utils;
using TSO.Files.utils;

namespace TSO.Vitaboy
{
    public class Binding
    {
        public string Bone;
        public uint MeshGroupID;
        public uint MeshFileID;
        public uint MeshTypeID;

        public uint TextureGroupID;
        public uint TextureFileID;
        public uint TextureTypeID;

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream)){
                var version = io.ReadUInt32();
                if (version != 1){
                    throw new Exception("Unknown binding version");
                }

                Bone = io.ReadPascalString();
                var meshType = io.ReadUInt32();
                if (meshType == 8){
                    this.MeshGroupID = io.ReadUInt32();
                    this.MeshFileID = io.ReadUInt32();
                    this.MeshTypeID = io.ReadUInt32();
                }

                var textureType = io.ReadUInt32();
                if(textureType == 8){
                    this.TextureGroupID = io.ReadUInt32();
                    this.TextureFileID = io.ReadUInt32();
                    this.TextureTypeID = io.ReadUInt32();
                }
            }
        }
    }
}
