using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace LibVitaBoy
{
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

        //public MeshVertex[] VertexData;
        //public MeshVertex[] TransformedVertexData;

        public MeshVertexData[] Vertex;
        public MeshVertexData[] TransformedVertex;


        public Mesh()
        {
        }




        public void GenerateData()
        {

        }



















        public void Read(byte[] data)
        {
            using (var reader = new VBFile(new MemoryStream(data)))
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
                //VertexData = new MeshVertex[RealVertexCount];

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
                //VertexData = new MeshVertex[TotalVertexCount];
                //TransformedVertexData = new MeshVertex[TotalVertexCount];

                Vertex = new MeshVertexData[TotalVertexCount];
                TransformedVertex = new MeshVertexData[TotalVertexCount];

                for (var i = 0; i < TotalVertexCount; i++){
                    var vertexData = new MeshVertex {
                        Coord = new Vector3(
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



                    //VertexData[i] = vertexData;
                    //TransformedVertexData[i] = tVertexData;

                    Vertex[i] = vertex;
                    TransformedVertex[i] = tVertex;
                }



            }
        }
    }

    public class BlendData
    {
        public float Weight;
        public int OtherVertex;
    }

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

    public class MeshVertexData
    {
        public MeshVertex Vertex;

        public uint BoneIndex;
        public BlendData BlendData;
    }


    public class BoneBinding
    {
        public int BoneIndex;
        public int FirstRealVertex;
        public int RealVertexCount;
        public int FirstBlendVertex;
        public int BlendVertexCount;
    }

    public class Face
    {
        public int VertexA;
        public int VertexB;
        public int VertexC;
    }
}
