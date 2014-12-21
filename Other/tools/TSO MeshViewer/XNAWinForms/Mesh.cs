/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is TSO Dressup.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ddfzcsm.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace Dressup
{
    public class Mesh
    {
        private int Version;
        private int BoneCount;
        
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

        public VertexPositionNormalTexture[] VertexTexNormalPositions
        {
            get { return m_VertexNTexPositions; }
        }

        public Mesh()
        {
        }

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
        public void ProcessMesh(Skeleton Skel, bool IsHeadMesh)
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

                if (IsHeadMesh)
                {
                    //Transform the head vertices' position by the absolute transform
                    //for the headbone (which is always bone 17) to render the head in place.
                    NormVerticies[i].Position = Vector3.Transform(Vertex[i].Vertex.Coord,
                        Skel.Bones[16].AbsoluteMatrix);

                    //Transform the head normals' position by the absolute transform
                    //for the headbone (which is always bone 17) to render the head in place.
                    NormVerticies[i].Normal = Vector3.Transform(Vertex[i].Vertex.NormalCoord,
                        Skel.Bones[16].AbsoluteMatrix);
                }
            }

            for (int i = 0; i < RealVertexCount; i++)
            {
                NormVerticies[i].TextureCoordinate.X = TransformedVertices[i].Vertex.TextureCoord.X;
                NormVerticies[i].TextureCoordinate.Y = TransformedVertices[i].Vertex.TextureCoord.Y;
            }

            m_VertexNTexPositions = NormVerticies;
        }

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
        /// Advances the frame of an animation for a skeleton used on this mesh.
        /// </summary>
        /// <param name="Skel">A skeleton used to render this mesh.</param>
        /// <param name="Animation">The animation to advance.</param>
        /// <param name="AnimationTime">The playback time for an animation (how long has it been playing for?)</param>
        /// <param name="TimeDelta">The timedelta of the rendering loop.</param>
        public void AdvanceFrame(ref Skeleton Skel, Anim Animation, ref float AnimationTime, float TimeDelta)
        {
            float Duration = (float)Animation.Motions[0].NumFrames / 30;
            AnimationTime += TimeDelta;
            AnimationTime = AnimationTime % Duration; //Loop the animation

            for (int i = 0; i < Animation.Motions.Count; i++)
            {
                int BoneIndex = Skel.FindBone(Animation.Motions[i].BoneName, i);

                if (BoneIndex == -1)
                    continue;

                int Frame = (int)(AnimationTime * 30);
                float FractionShown = AnimationTime * 30 - Frame;
                int NextFrame = (Frame + 1 != Animation.Motions[0].NumFrames) ? Frame + 1 : 0;

                if (Animation.Motions[i].HasTranslation == 1)
                {
                    Vector3 Translation = new Vector3(Animation.Motions[i].Translations[Frame, 0],
                        Animation.Motions[i].Translations[Frame, 1], Animation.Motions[i].Translations[Frame, 2]);
                    Vector3 NextTranslation = new Vector3(Animation.Motions[i].Translations[NextFrame, 0],
                        Animation.Motions[i].Translations[NextFrame, 1], Animation.Motions[i].Translations[NextFrame, 2]);

                    Vector3 UpdatedTranslation = new Vector3();
                    UpdatedTranslation.X = (1 - FractionShown) * Translation.X + FractionShown * NextTranslation.X;
                    UpdatedTranslation.Y = (1 - FractionShown) * Translation.Y + FractionShown * NextTranslation.Y;
                    UpdatedTranslation.Z = (1 - FractionShown) * Translation.Z + FractionShown * NextTranslation.Z;

                    Skel.Bones[BoneIndex].Translation = UpdatedTranslation;
                }

                if (Animation.Motions[i].HasRotation == 1)
                {
                    Quaternion Rotation = new Quaternion(Animation.Motions[i].Rotations[Frame, 0],
                        Animation.Motions[i].Rotations[Frame, 1], Animation.Motions[i].Rotations[Frame, 2],
                        Animation.Motions[i].Rotations[Frame, 3]);
                    Quaternion NextRotation = new Quaternion(Animation.Motions[i].Rotations[NextFrame, 0],
                        Animation.Motions[i].Rotations[NextFrame, 1], Animation.Motions[i].Rotations[NextFrame, 2],
                        Animation.Motions[i].Rotations[NextFrame, 3]);

                    //Use Slerp to interpolate
                    float W1, W2 = 1.0f;
                    float CosTheta = DotProduct(Rotation, NextRotation);

                    if (CosTheta < 0)
                    {
                        CosTheta *= -1;
                        W2 *= -1;
                    }

                    float Theta = (float)Math.Acos(CosTheta);
                    float SinTheta = (float)Math.Sin(Theta);

                    if (SinTheta > 0.001f)
                    {
                        W1 = (float)Math.Sin((1.0f - FractionShown) * Theta) / SinTheta;
                        W2 *= (float)Math.Sin(FractionShown * Theta) / SinTheta;
                    }
                    else
                    {
                        W1 = 1.0f - FractionShown;
                        W2 = FractionShown;
                    }

                    Quaternion UpdatedRotation = new Quaternion();
                    UpdatedRotation.X = W1 * Rotation.X + W2 * NextRotation.X;
                    UpdatedRotation.Y = W1 * Rotation.Y + W2 * NextRotation.Y;
                    UpdatedRotation.Z = W1 * Rotation.Z + W2 * NextRotation.Z;
                    UpdatedRotation.W = W1 * Rotation.W + W2 * NextRotation.W;

                    Skel.Bones[BoneIndex].Rotation.X = UpdatedRotation.X;
                    Skel.Bones[BoneIndex].Rotation.Y = UpdatedRotation.Y;
                    Skel.Bones[BoneIndex].Rotation.Z = UpdatedRotation.Z;
                    Skel.Bones[BoneIndex].Rotation.W = UpdatedRotation.W;
                }
            }
        }

        private float DotProduct(Quaternion Rotation1, Quaternion Rotation2)
        {
            return Rotation1.X * Rotation2.X + Rotation1.Y * Rotation2.Y + Rotation1.Z * Rotation2.Z +
                Rotation1.W * Rotation2.W;
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
