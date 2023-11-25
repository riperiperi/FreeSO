using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Files.Utils;
using FSO.Common.Rendering.Framework;
using FSO.Vitaboy.Model;

namespace FSO.Vitaboy
{
    /// <summary>
    /// 3D Mesh.
    /// </summary>
    public class Mesh : I3DGeometry
    {
        /** 3D Data **/
        public VitaboyVertex[] VertexBuffer;
        
        public int[] BlendVertBoneIndices;
        public Vector3[] BlendVerts;
        public Vector3[] BlendNormals;

        public int[] IndexBuffer;
        public int NumPrimitives;
        public BoneBinding[] BoneBindings;
        public BlendData[] BlendData;

        private bool GPUMode;
        private DynamicVertexBuffer GPUVertexBuffer;
        private IndexBuffer GPUIndexBuffer;
        private bool Prepared = false;

        public string SkinName;
        public string TextureName;

        public Mesh()
        {
        }

        /// <summary>
        /// Clones this mesh.
        /// </summary>
        /// <returns>A Mesh instance with the same data as this one.</returns>
        public Mesh Clone()
        {
            var result = new Mesh()
            {
                BlendData = BlendData,
                BoneBindings = BoneBindings,
                NumPrimitives = NumPrimitives,
                IndexBuffer = IndexBuffer,
                VertexBuffer = VertexBuffer,
                BlendVerts = BlendVerts,
                BlendVertBoneIndices = (int[])BlendVertBoneIndices.Clone()
            };
            return result;
        }

        /// <summary>
        /// Transforms the verticies making up this mesh into
        /// the designated bone positions.
        /// </summary>
        /// <param name="bone">The bone to start with. Should always be the ROOT bone.</param>
        /// 

        public void Prepare(Bone bone)
        //TODO: assumes that skeleton will be same configuration for all bindings of this mesh. 
        //If any meshes are used by pets and avatars(???) or we implement children this will need 
        //to be changed to support binds to multiple SKEL bases.
        {
            if (Prepared) return;
            var bindings = BoneBindings.Where(x => x.BoneName.Equals(bone.Name, StringComparison.InvariantCultureIgnoreCase));
            if (bindings.Count(x => x.RealVertexCount == 0) > 1) { }
            foreach (var binding in bindings)
            {
                for (var i = 0; i < binding.RealVertexCount; i++)
                {
                    var vertexIndex = binding.FirstRealVertex + i;
                    VertexBuffer[vertexIndex].Parameters.X = bone.Index;
                }

                for (var i = 0; i < binding.BlendVertexCount; i++)
                {
                    var blendVertexIndex = binding.FirstBlendVertex + i;
                    BlendVertBoneIndices[blendVertexIndex] = bone.Index;
                }
            }

            foreach (var child in bone.Children)
            {
                Prepare(child);
            }

            if (bone.Name.Equals("ROOT", StringComparison.InvariantCultureIgnoreCase))
            {
                for (int i = 0; i < BlendData.Length; i++)
                {
                    var data = BlendData[i];
                    var vert = BlendVertBoneIndices[i];

                    VertexBuffer[data.OtherVertex].Parameters.Y = BlendVertBoneIndices[i];
                    VertexBuffer[data.OtherVertex].Parameters.Z = data.Weight;
                    VertexBuffer[data.OtherVertex].BvPosition = BlendVerts[i];
                    VertexBuffer[data.OtherVertex].BvNormal = BlendNormals[i];
                }

                InvalidateMesh();
                Prepared = true;
            }
        }

        public void StoreOnGPU(GraphicsDevice device)
        {
            GPUMode = true;
            GPUVertexBuffer = new DynamicVertexBuffer(device, typeof(VitaboyVertex), VertexBuffer.Length, BufferUsage.None);
            GPUVertexBuffer.SetData(VertexBuffer);
            
            GPUIndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, IndexBuffer.Length, BufferUsage.None);
            GPUIndexBuffer.SetData(IndexBuffer);
        }

        public void InvalidateMesh()
        {
            if (GPUMode)
            {
                GPUVertexBuffer.SetData(VertexBuffer);
            }
        }

        #region I3DGeometry Members

        public void DrawGeometry(GraphicsDevice gd){
            if (GPUMode){
                gd.Indices = GPUIndexBuffer;
                gd.SetVertexBuffer(GPUVertexBuffer);
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.Length, 0, NumPrimitives);
            }else{
                //legacy path, shouldn't get here
                gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, VertexBuffer, 0, VertexBuffer.Length, IndexBuffer, 0, NumPrimitives);
            }
        }

        #endregion

        /// <summary>
        /// Draws this mesh.
        /// </summary>
        /// <param name="gd">A GraphicsDevice instance used for drawing.</param>
        public void Draw(GraphicsDevice gd){
            if (!GPUMode) StoreOnGPU(gd);
            gd.Indices = GPUIndexBuffer;
            gd.SetVertexBuffer(GPUVertexBuffer);
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, NumPrimitives);
        }

        /// <summary>
        /// Reads a mesh from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a mesh.</param> <param name="bmf">If this stream contains a .bmf file (rather than TSO .mesh).</param>
        public void Read(BCFReadProxy io, bool bmf)
        {
            if (bmf)
            {
                SkinName = io.ReadPascalString();
                TextureName = io.ReadPascalString();
            }
            else
            {
                var version = io.ReadInt32();
            }
            var boneCount = io.ReadInt32();
            var boneNames = new string[boneCount];
            for (var i = 0; i < boneCount; i++)
            {
                boneNames[i] = io.ReadPascalString();
            }

            var faceCount = io.ReadInt32();
            NumPrimitives = faceCount;

            IndexBuffer = new int[faceCount * 3];
            int offset = 0;
            for (var i = 0; i < faceCount; i++)
            {
                IndexBuffer[offset++] = io.ReadInt32();
                IndexBuffer[offset++] = io.ReadInt32();
                IndexBuffer[offset++] = io.ReadInt32();
            }

            /** Bone bindings **/
            var bindingCount = io.ReadInt32();
            BoneBindings = new BoneBinding[bindingCount];
            for (var i = 0; i < bindingCount; i++)
            {
                BoneBindings[i] = new BoneBinding
                {
                    BoneIndex = io.ReadInt32(),
                    FirstRealVertex = io.ReadInt32(),
                    RealVertexCount = io.ReadInt32(),
                    FirstBlendVertex = io.ReadInt32(),
                    BlendVertexCount = io.ReadInt32()
                };

                BoneBindings[i].BoneName = boneNames[Math.Min(boneNames.Length - 1, BoneBindings[i].BoneIndex)];
            }


            var realVertexCount = io.ReadInt32();
            VertexBuffer = new VitaboyVertex[realVertexCount];

            for (var i = 0; i < realVertexCount; i++)
            {
                VertexBuffer[i].TextureCoordinate.X = io.ReadFloat();
                VertexBuffer[i].TextureCoordinate.Y = io.ReadFloat();
            }

            /** Blend data **/
            var blendVertexCount = io.ReadInt32();
            BlendData = new BlendData[blendVertexCount];
            for (var i = 0; i < blendVertexCount; i++)
            {
                if (io is IoBuffer)
                {
                    BlendData[i] = new BlendData
                    {
                        Weight = (float)io.ReadInt32() / 0x8000,
                        OtherVertex = io.ReadInt32()
                    };
                } else
                {
                    BlendData[i] = new BlendData
                    {
                        OtherVertex = io.ReadInt32(),
                        Weight = (float)io.ReadInt32() / 0x8000
                    };
                }

            }

            var realVertexCount2 = io.ReadInt32();

            for (int i = 0; i < realVertexCount; i++)
            {
                VertexBuffer[i].Position = new Microsoft.Xna.Framework.Vector3(
                    -io.ReadFloat(),
                    io.ReadFloat(),
                    io.ReadFloat()
                );

                VertexBuffer[i].Normal = new Microsoft.Xna.Framework.Vector3(
                    -io.ReadFloat(),
                    io.ReadFloat(),
                    io.ReadFloat()
                );
                if (VertexBuffer[i].Normal == Vector3.Zero) VertexBuffer[i].Normal = new Vector3(0, 1, 0);
            }

            BlendVerts = new Vector3[blendVertexCount];
            BlendNormals = new Vector3[blendVertexCount];

            for (int i = 0; i < blendVertexCount; i++)
            {
                BlendVerts[i] = new Vector3(
                    -io.ReadFloat(),
                    io.ReadFloat(),
                    io.ReadFloat()
                );

                BlendNormals[i] = new Vector3(
                    -io.ReadFloat(),
                    io.ReadFloat(),
                    io.ReadFloat()
                ); //todo: use it for lighting.
            }

            BlendVertBoneIndices = new int[blendVertexCount];
        }

        public void Write(BCFWriteProxy io, bool bmf)
        {
            if (bmf)
            {
                io.WritePascalString(SkinName);
                io.WritePascalString(TextureName);
            }
            else
            {
                io.WriteInt32(2); //version
            }

            var boneNames = new HashSet<string>(BoneBindings.Select(x => x.BoneName)).ToList();
            io.WriteInt32(boneNames.Count);
            foreach (var name in boneNames)
            {
                io.WritePascalString(name);
            }

            io.WriteInt32(NumPrimitives);

            foreach (var index in IndexBuffer)
            {
                io.WriteInt32(index);
            }

            io.WriteInt32(BoneBindings.Length);
            foreach (var binding in BoneBindings)
            {
                io.WriteInt32(boneNames.IndexOf(binding.BoneName));
                io.WriteInt32(binding.FirstRealVertex);
                io.WriteInt32(binding.RealVertexCount);
                io.WriteInt32(binding.FirstBlendVertex);
                io.WriteInt32(binding.BlendVertexCount);
            }

            io.WriteInt32(VertexBuffer.Length);
            io.SetGrouping(2);
            foreach (var vert in VertexBuffer)
            {
                io.WriteFloat(vert.TextureCoordinate.X);
                io.WriteFloat(vert.TextureCoordinate.Y);
            }
            io.SetGrouping(1);

            /** Blend data **/
            io.WriteInt32(BlendVerts.Length);
            foreach (var bv in BlendData)
            {
                var binary = true;
                if (binary)
                {
                    io.WriteInt32((int)(bv.Weight * 0x8000));
                    io.WriteInt32(bv.OtherVertex);
                }
                else
                {
                    io.WriteInt32(bv.OtherVertex);
                    io.WriteInt32((int)(bv.Weight * 0x8000));
                }
            }

            io.WriteInt32(VertexBuffer.Length); //realVertexCount2

            io.SetGrouping(3);
            foreach (var vert in VertexBuffer)
            {
                io.WriteFloat(-vert.Position.X);
                io.WriteFloat(vert.Position.Y);
                io.WriteFloat(vert.Position.Z);

                io.WriteFloat(-vert.Normal.X);
                io.WriteFloat(vert.Normal.Y);
                io.WriteFloat(vert.Normal.Z);
            }

            var i = 0;
            foreach (var bv in BlendVerts)
            {
                io.WriteFloat(-bv.X);
                io.WriteFloat(bv.Y);
                io.WriteFloat(bv.Z);
                
                var norm = BlendNormals[i++];
                io.WriteFloat(-norm.X);
                io.WriteFloat(norm.Y);
                io.WriteFloat(norm.Z);
            }

            io.SetGrouping(1);
        }
    }
}
