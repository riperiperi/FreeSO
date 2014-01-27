/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ddfzcsm.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib.ThreeD
{
    /// <summary>
    /// Represents a renderable mesh.
    /// </summary>
    public class Mesh
    {
        public int Version;
        public int BoneCount;
        
        public string[] BoneNames;

        public int FaceCount;
        public Face[] FaceData;

        public int BindingCount;
        public BoneBinding[] BoneBindings;

        public int RealVertexCount;
        public int BlendVertexCount;
        public int TotalVertexCount;

        public MeshVertexData[] Vertex;
        public MeshVertexData[] TransformedVertices;

        private VertexPositionNormalTexture[] m_VertexNTexPositions;

        /// <summary>
        /// An array of VertexPositionNormalTexture elements that can
        /// be looped to render this mesh. Will be null until
        /// ProcessMesh() has been called!
        /// </summary>
        public VertexPositionNormalTexture[] VertexTexNormalPositions
        {
            get { return m_VertexNTexPositions; }
            set { m_VertexNTexPositions = value; }
        }

        /// <summary>
        /// Creates a new Mesh instance.
        /// </summary>
        public Mesh()
        {
        }

        /// <summary>
        /// Transforms the verticies making up this mesh into
        /// the designated bone positions.
        /// </summary>
        /// <param name="bone">The bone to start with. Should always be the ROOT bone.</param>
        public void TransformVertices(Bone bone)
        {
            var boneBinding = BoneBindings.FirstOrDefault(x => BoneNames[x.BoneIndex] == bone.Name);

            if (boneBinding != null)
            {
                for (var i = 0; i < boneBinding.RealVertexCount; i++)
                {
                    int vertexIndex = boneBinding.FirstRealVertex + i;
                    MeshVertexData transformedVertex = TransformedVertices[vertexIndex];
                    MeshVertexData relativeVertex = Vertex[vertexIndex];

                    var translatedMatrix = Matrix.CreateTranslation(new Vector3(relativeVertex.Vertex.Coord.X, relativeVertex.Vertex.Coord.Y, relativeVertex.Vertex.Coord.Z)) * bone.AbsoluteMatrix;
                    transformedVertex.Vertex.Coord = Vector3.Transform(Vector3.Zero, translatedMatrix);

                    //Normals...
                    translatedMatrix = Matrix.CreateTranslation(new Vector3(relativeVertex.Vertex.NormalCoord.X, relativeVertex.Vertex.NormalCoord.Y, relativeVertex.Vertex.NormalCoord.Z)) * bone.AbsoluteMatrix;
                    transformedVertex.Vertex.NormalCoord = Vector3.Transform(Vector3.Zero, translatedMatrix);
                }
            }

            foreach (var child in bone.Children)
            {
                TransformVertices(child);
            }
        }

        /// <summary>
        /// Processes the loaded mesh's data and populates an array of
        /// VertexPositionNormalTexture elements that can be looped to
        /// render the mesh. Assumes that TransformVertices2() and 
        /// BlendVertices2() has been called for bodymeshes!
        /// </summary>
        public void ProcessMesh()
        {
            VertexPositionNormalTexture[] NormVerticies = new VertexPositionNormalTexture[TotalVertexCount];

            for (int i = 0; i < TotalVertexCount; i++)
            {
                NormVerticies[i] = new VertexPositionNormalTexture();
                NormVerticies[i].Position.X = TransformedVertices[i].Vertex.Coord.X;
                NormVerticies[i].Position.Y = TransformedVertices[i].Vertex.Coord.Y;
                NormVerticies[i].Position.Z = TransformedVertices[i].Vertex.Coord.Z;
                NormVerticies[i].Normal.X = TransformedVertices[i].Vertex.NormalCoord.X;
                NormVerticies[i].Normal.Y = TransformedVertices[i].Vertex.NormalCoord.Y;
                NormVerticies[i].Normal.Z = TransformedVertices[i].Vertex.NormalCoord.Z;
            }

            for (int i = 0; i < RealVertexCount; i++)
            {
                NormVerticies[i].TextureCoordinate.X = TransformedVertices[i].Vertex.TextureCoord.X;
                NormVerticies[i].TextureCoordinate.Y = TransformedVertices[i].Vertex.TextureCoord.Y;
            }

            m_VertexNTexPositions = NormVerticies;
        }

        /// <summary>
        /// Reads this mesh.
        /// </summary>
        /// <param name="data">The data for the mesh file to read from.</param>
        public void Read(byte[] data)
        {
            using (var reader = new VBReader(new MemoryStream(data)))
            {
                System.Diagnostics.Debug.WriteLine("========== Mesh ==========");

                Version = reader.ReadInt32();
                BoneCount = reader.ReadInt32();

                System.Diagnostics.Debug.WriteLine("Version: " + Version);
                System.Diagnostics.Debug.WriteLine("BoneCount: " + BoneCount);

                /** Read bone names |str_len|str_body| **/
                BoneNames = new string[BoneCount];
                for (var i = 0; i < BoneCount; i++){
                    BoneNames[i] = reader.ReadPascalString();

                    System.Diagnostics.Debug.WriteLine("| Bone " + (i + 1) + ": " + BoneNames[i]);
                }

                /** Faces **/
                FaceCount = reader.ReadInt32();
                System.Diagnostics.Debug.WriteLine("FaceCount: " + FaceCount);

                FaceData = new Face[FaceCount];
                for (var i = 0; i < FaceCount; i++){
                    FaceData[i] = new Face {
                        VertexA = reader.ReadInt32(),
                        VertexB = reader.ReadInt32(),
                        VertexC = reader.ReadInt32()
                    };
                }

                /** Bone bindings **/
                BindingCount = reader.ReadInt32();
                BoneBindings = new BoneBinding[BindingCount];
                for (var i = 0; i < BindingCount; i++){
                    BoneBindings[i] = new BoneBinding {
                        BoneIndex = reader.ReadInt32(),
                        FirstRealVertex = reader.ReadInt32(),
                        RealVertexCount = reader.ReadInt32(),
                        FirstBlendVertex = reader.ReadInt32(),
                        BlendVertexCount = reader.ReadInt32()
                    };
                }

                /** Texture vertex data **/
                RealVertexCount = reader.ReadInt32();

                var textureData = new Vector2[RealVertexCount];
                for (var i = 0; i < RealVertexCount; i++){
                    textureData[i] = new Vector2(
                        reader.ReadFloat(),
                        reader.ReadFloat()
                    );
                }

                /** Blend data **/
                BlendVertexCount = reader.ReadInt32();
                var blend = new BlendData[BlendVertexCount];
                for (var i = 0; i < BlendVertexCount; i++)
                {
                    blend[i] = new BlendData {
                        Weight = (float)reader.ReadInt32()/0x8000,
                        OtherVertex = reader.ReadInt32()
                    };
                }

                TotalVertexCount = reader.ReadInt32();

                Vertex = new MeshVertexData[TotalVertexCount];
                TransformedVertices = new MeshVertexData[TotalVertexCount];

                for (var i = 0; i < TotalVertexCount; i++)
                {
                    var vertexData = new MeshVertex
                    {
                        Coord = new Vector3
                        (
                            -reader.ReadFloat(),
                            reader.ReadFloat(),
                            reader.ReadFloat()
                        )
                    };
                    var tVertexData = new MeshVertex
                    {
                        Coord = vertexData.Coord,
                        NormalCoord = new Vector3(
                            -reader.ReadFloat(),
                            reader.ReadFloat(),
                            reader.ReadFloat()
                        )
                    };

                    var vertex = new MeshVertexData {
                        Vertex = vertexData
                    };

                    var tVertex = new MeshVertexData{
                        Vertex = tVertexData
                    };

                    if (i < RealVertexCount)
                    {
                        tVertex.Vertex.TextureCoord = textureData[i];
                    }
                    else
                    {
                        tVertex.BlendData = blend[i - RealVertexCount];
                    }

                    Vertex[i] = vertex;
                    TransformedVertices[i] = tVertex;
                }
            }
        }

        /// <summary>
        /// Draw this mesh geometry onto the graphics device
        /// </summary>
        public void Draw(GraphicsDevice gd)
        {
            gd.VertexDeclaration = new VertexDeclaration(gd, MeshVertex.VertexElements);

            foreach (var face in FaceData)
            {
                var vertexA = TransformedVertices[face.VertexA];
                var vertexB = TransformedVertices[face.VertexB];
                var vertexC = TransformedVertices[face.VertexC];

                var vertexList = new MeshVertex[3] { vertexA.Vertex, vertexB.Vertex, vertexC.Vertex };
                gd.DrawUserPrimitives(PrimitiveType.TriangleList, vertexList, 0, 1);
            }
        }

        /// <summary>
        /// Creates a brand new mesh object that is a complete copy of this one.
        /// We need this utility because the Skel system modifies meshs. This means we
        /// have to copy a sim mesh before we use it.
        /// </summary>
        /// <returns></returns>
        public Mesh Clone()
        {
            var newMesh = new Mesh
            {
                Version = this.Version,
                BoneCount = this.BoneCount,
                BoneNames = this.BoneNames,
                FaceCount = this.FaceCount,
                FaceData = this.FaceData,
                BindingCount = this.BindingCount,
                BoneBindings = this.BoneBindings,
                RealVertexCount = this.RealVertexCount,
                BlendVertexCount = this.BlendVertexCount,
                TotalVertexCount = this.TotalVertexCount
            };

            /** Because mesh vertex is a struct, copying the array should be enough to clone it **/
            newMesh.Vertex = this.Vertex.ToArray();
            newMesh.TransformedVertices = this.TransformedVertices.ToArray();
            newMesh.VertexTexNormalPositions = this.VertexTexNormalPositions.ToArray();
            
            return newMesh;
        }
    }

    public class BlendData
    {
        public float Weight;
        public int OtherVertex;
    }

    /// <summary>
    /// Represents a MeshVertex that makes up a face.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex
    {
        public Vector3 Coord;
        /** UV Mapping **/
        public Vector2 TextureCoord;
        public Vector3 NormalCoord;

        public static int SizeInBytes = sizeof(float) * 8;

        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
             new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( 0, sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0 )
        };
    }

    /// <summary>
    /// Stores a vertex and its accompanying blenddata,
    /// for simplicity's sake.
    /// </summary>
    public class MeshVertexData
    {
        public MeshVertex Vertex;

        public uint BoneIndex;
        public BlendData BlendData;
    }

    /// <summary>
    /// Represents a binding that binds a vertex to a bone.
    /// </summary>
    public class BoneBinding
    {
        public int BoneIndex;
        public int FirstRealVertex;
        public int RealVertexCount;
        public int FirstBlendVertex;
        public int BlendVertexCount;
    }

    /// <summary>
    /// Represents a face that makes up a renderable mesh.
    /// </summary>
    public class Face
    {
        public int VertexA;
        public int VertexB;
        public int VertexC;
    }
}
