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

        private int[] BlendVertBoneIndices;
        private Vector3[] BlendVerts;

        protected short[] IndexBuffer;
        protected int NumPrimitives;
        public BoneBinding[] BoneBindings;
        public BlendData[] BlendData;

        private bool GPUMode;
        private DynamicVertexBuffer GPUVertexBuffer;
        private IndexBuffer GPUIndexBuffer;
        private bool Prepared = false;

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
            var binding = BoneBindings.FirstOrDefault(x => x.BoneName.Equals(bone.Name, StringComparison.InvariantCultureIgnoreCase));
            if (binding != null)
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
                }

                InvalidateMesh();
                Prepared = true;
            }
        }

        /*public void Transform(Bone bone)
        {

            var binding = BoneBindings.FirstOrDefault(x => x.BoneName.Equals(bone.Name, StringComparison.InvariantCultureIgnoreCase));
            if (binding != null)
            {
                for (var i = 0; i < binding.RealVertexCount; i++)
                {
                    var vertexIndex = binding.FirstRealVertex + i;
                    var blendVertexIndex = vertexIndex;//binding.FirstBlendVertex + i;

                    var realVertex = RealVertexBuffer[vertexIndex];
                    //var matrix = Matrix.CreateTranslation(realVertex.Position) * bone.AbsoluteMatrix;

                    //Position
                    var newPosition = Vector3.Transform(realVertex.Position, bone.AbsoluteMatrix);
                    BlendVertexBuffer[blendVertexIndex].Position = newPosition;

                    //Normals
                    var matrix = Matrix.CreateTranslation(
                        new Vector3(realVertex.Normal.X,
                                    realVertex.Normal.Y,
                                    realVertex.Normal.Z)) * bone.AbsoluteMatrix;
                }

                for (var i = 0; i < binding.BlendVertexCount; i++)
                {
                    var blendVertexIndex = binding.FirstBlendVertex + i;
                    var realVertex = UntransformedBlendVerts[blendVertexIndex];

                    //Position
                    var newPosition = Vector3.Transform(realVertex, bone.AbsoluteMatrix);
                    TransformedBlendVerts[blendVertexIndex] = newPosition;

                    //todo, alter normals too. would it be correct to linear interpolate that too? it seems like doing that might be kinda stupid

                }

            }

            foreach (var child in bone.Children)
            {
                Transform(child);
            }

            if (bone.Name.Equals("ROOT", StringComparison.InvariantCultureIgnoreCase))
            {
                for (int i = 0; i < BlendData.Length; i++)
                {
                    var data = BlendData[i];
                    var vert = TransformedBlendVerts[i];

                    BlendVertexBuffer[data.OtherVertex].Position = Vector3.Lerp(BlendVertexBuffer[data.OtherVertex].Position, vert, data.Weight);
                }

                InvalidateMesh();
            }
        }*/

        public void StoreOnGPU(GraphicsDevice device)
        {
            GPUMode = true;
            GPUVertexBuffer = new DynamicVertexBuffer(device, typeof(VitaboyVertex), VertexBuffer.Length, BufferUsage.None);
            GPUVertexBuffer.SetData(VertexBuffer);
            
            GPUIndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, IndexBuffer.Length, BufferUsage.None);
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
            //if (BoneMatrices != null) Avatar.Effect.Parameters["SkelBindings"].SetValue(BoneMatrices);
            if (GPUMode){
                gd.Indices = GPUIndexBuffer;
                gd.SetVertexBuffer(GPUVertexBuffer);
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, VertexBuffer.Length, 0, NumPrimitives);
            }else{
                gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, VertexBuffer, 0, VertexBuffer.Length, IndexBuffer, 0, NumPrimitives);
            }
        }

        #endregion

        /// <summary>
        /// Draws this mesh.
        /// </summary>
        /// <param name="gd">A GraphicsDevice instance used for drawing.</param>
        public void Draw(GraphicsDevice gd){
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, VertexBuffer, 0, VertexBuffer.Length, IndexBuffer, 0, NumPrimitives);
        }

        /// <summary>
        /// Reads a mesh from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a mesh.</param>
        public unsafe void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadInt32();
                var boneCount = io.ReadInt32();
                var boneNames = new string[boneCount];
                for (var i = 0; i < boneCount; i++){
                    boneNames[i] = io.ReadPascalString();
                }

                var faceCount = io.ReadInt32();
                NumPrimitives = faceCount;

                IndexBuffer = new short[faceCount * 3];
                int offset = 0;
                for (var i = 0; i < faceCount; i++){
                    IndexBuffer[offset++] = (short)io.ReadInt32();
                    IndexBuffer[offset++] = (short)io.ReadInt32();
                    IndexBuffer[offset++] = (short)io.ReadInt32();
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

                    BoneBindings[i].BoneName = boneNames[BoneBindings[i].BoneIndex];
                }


                var realVertexCount = io.ReadInt32();
                VertexBuffer = new VitaboyVertex[realVertexCount];

                for (var i = 0; i < realVertexCount; i++){
                    VertexBuffer[i].TextureCoordinate.X = io.ReadFloat();
                    VertexBuffer[i].TextureCoordinate.Y = io.ReadFloat();
                }

                /** Blend data **/
                var blendVertexCount = io.ReadInt32();
                BlendData = new BlendData[blendVertexCount];
                for (var i = 0; i < blendVertexCount; i++)
                {
                    BlendData[i] = new BlendData
                    {
                        Weight = (float)io.ReadInt32() / 0x8000,
                        OtherVertex = io.ReadInt32()
                    };
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
                }

                BlendVerts = new Vector3[blendVertexCount];

                for (int i = 0; i < blendVertexCount; i++)
                {
                    BlendVerts[i] = new Vector3(
                        -io.ReadFloat(),
                        io.ReadFloat(),
                        io.ReadFloat()
                    );

                    var normal = new Vector3(
                        -io.ReadFloat(),
                        io.ReadFloat(),
                        io.ReadFloat()
                    ); //todo: read this in somewhere and maybe use it.
                }

                BlendVertBoneIndices = new int[blendVertexCount];
            }
        }
    }
}
