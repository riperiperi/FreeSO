/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO MeshViewer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNAWinForms
{
    /// <summary>
    /// A face of a mesh.
    /// </summary>
    public struct Face
    {
        public int AVertexIndex, BVertexIndex, CVertexIndex;
    }

    public struct BoneBinding
    {
        public int BoneIndex, FirstVertex, VertexCount, FirstBlendedVert, BlendedVertexCount;
    }

    public struct BlendData
    {
        public float WeightFixed;
        public int OtherVertexIndex;
    }

    /// <summary>
    /// Represents a renderable mesh.
    /// </summary>
    public class Mesh
    {
        private static int m_Version = 0;

        private int m_BoneCount = 0;
        private List<string> m_BoneNames = new List<string>();

        private int m_FaceCount = 0;
        private Face[] m_Faces;

        private int m_BndCount = 0;
        private List<BoneBinding> m_BoneBindings = new List<BoneBinding>();

        private int m_TotalVertexCount;
        private Single[,] m_TexVerticies;

        private int m_BlendCount = 0;
        private List<BlendData> m_BlendData = new List<BlendData>();

        private int m_RealVertexCount = 0;
        private Single[,] m_VertexData;

        private Vertex[] m_TransformedVertices;

        private VertexPositionNormalTexture[] m_VertexNTexPositions;

        //Is this a bodymesh?
        public bool IsBodyMesh = false;

        public int TexVertexCount
        {
            get { return m_RealVertexCount; }
        }

        /// <summary>
        /// The transformed vertices for a body mesh.
        /// </summary>
        public Vertex[] TransformedVertices
        {
            get { return m_TransformedVertices; }
        }

        /// <summary>
        /// Number of verticies in this mesh.
        /// </summary>
        public int VertexCount
        {
            get { return m_TotalVertexCount; }
        }

        /// <summary>
        /// The vertexdata that makes up this mesh.
        /// </summary>
        public Single[,] VertexData
        {
            get { return m_VertexData; }
        }

        public Single[,] TextureVertData
        {
            get { return m_TexVerticies; }
        }

        /// <summary>
        /// Number of faces in this mesh.
        /// </summary>
        public int FaceCount
        {
            get { return m_FaceCount; }
        }

        /// <summary>
        /// The faces of this mesh.
        /// </summary>
        public Face[] Faces
        {
            get { return m_Faces; }
        }

        /// <summary>
        /// The bonebindings associated with this mesh.
        /// </summary>
        public List<BoneBinding> BoneBindings
        {
            get { return m_BoneBindings; }
        }

        /// <summary>
        /// The number of blended verticies in this mesh.
        /// Only applicable for body-meshes.
        /// </summary>
        public int BlendCount
        {
            get { return m_BlendCount; }
        }

        public List<BlendData> Blends
        {
            get { return m_BlendData; }
        }

        /// <summary>
        /// All the vertices and normals for this mesh.
        /// Should only be accessed if this mesh is a head mesh.
        /// </summary>
        public VertexPositionNormalTexture[] VertexTexNormalPositions
        {
            get { return m_VertexNTexPositions; }
        }

        public Mesh(string Filepath, bool BodyMesh)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Filepath, FileMode.Open));

            IsBodyMesh = BodyMesh;

            m_Version = Endian.SwapInt32(Reader.ReadInt32());

            m_BoneCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BoneCount; i++)
            {
                byte StrLen = Reader.ReadByte();
                string BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(StrLen));
                m_BoneNames.Add(BoneName);
            }

            m_FaceCount = Endian.SwapInt32(Reader.ReadInt32());
            m_Faces = new Face[m_FaceCount];

            for (int i = 0; i < m_FaceCount; i++)
            {
                m_Faces[i].AVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].BVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].CVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
            }

            m_BndCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BndCount; i++)
            {
                BoneBinding Binding = new BoneBinding();
                Binding.BoneIndex = Endian.SwapInt32(Reader.ReadInt32());
                Binding.FirstVertex = Endian.SwapInt32(Reader.ReadInt32());
                Binding.VertexCount = Endian.SwapInt32(Reader.ReadInt32());
                Binding.FirstBlendedVert = Endian.SwapInt32(Reader.ReadInt32());
                Binding.BlendedVertexCount = Endian.SwapInt32(Reader.ReadInt32());

                m_BoneBindings.Add(Binding);
            }

            m_RealVertexCount = Endian.SwapInt32(Reader.ReadInt32());
            m_TexVerticies = new Single[m_RealVertexCount, 3];

            for (int i = 0; i < m_RealVertexCount; i++)
            {
                m_TexVerticies[i, 0] = i;
                m_TexVerticies[i, 1] = Reader.ReadSingle();
                m_TexVerticies[i, 2] = Reader.ReadSingle();
            }

            m_BlendCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BlendCount; i++)
            {
                BlendData Blend = new BlendData();
                Blend.WeightFixed = (float)(Endian.SwapInt32(Reader.ReadInt32())) / 0x8000;
                Blend.OtherVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_BlendData.Add(Blend);
            }

            m_TotalVertexCount = Endian.SwapInt32(Reader.ReadInt32());

            m_VertexData = new Single[m_TotalVertexCount, 6];
            m_TransformedVertices = new Vertex[m_TotalVertexCount];

            for (int i = 0; i < m_TotalVertexCount; i++)
            {
                m_VertexData[i, 0] = Reader.ReadSingle();
                m_VertexData[i, 1] = Reader.ReadSingle();
                m_VertexData[i, 2] = Reader.ReadSingle();
                //Normals
                m_VertexData[i, 3] = Reader.ReadSingle();
                m_VertexData[i, 4] = Reader.ReadSingle();
                m_VertexData[i, 5] = Reader.ReadSingle();

                if (i < m_RealVertexCount)
                {
                    if (m_TransformedVertices[i] == null)
                        m_TransformedVertices[i] = new Vertex();

                    //Fixed vertex
                    m_TransformedVertices[i].TextureCoord.X = m_TexVerticies[i, 1];
                    m_TransformedVertices[i].TextureCoord.Y = m_TexVerticies[i, 2];
                }
                else
                {
                    if (m_TransformedVertices[i] == null)
                        m_TransformedVertices[i] = new Vertex();

                    //Blended vertex
                    m_TransformedVertices[i].Blend.WeightFixed = m_BlendData[i - m_RealVertexCount].WeightFixed;
                    m_TransformedVertices[i].Blend.OtherVertexIndex = m_BlendData[i - m_RealVertexCount].OtherVertexIndex;
                }
            }
        }

        public Mesh(byte[] Filedata, bool BodyMesh)
        {
            MemoryStream MemStream = new MemoryStream(Filedata);
            BinaryReader Reader = new BinaryReader(MemStream);

            IsBodyMesh = BodyMesh;

            m_Version = Endian.SwapInt32(Reader.ReadInt32());

            m_BoneCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BoneCount; i++)
            {
                byte StrLen = Reader.ReadByte();
                string BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(StrLen));
                m_BoneNames.Add(BoneName);
            }

            m_FaceCount = Endian.SwapInt32(Reader.ReadInt32());
            m_Faces = new Face[m_FaceCount];

            for (int i = 0; i < m_FaceCount; i++)
            {
                m_Faces[i].AVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].BVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_Faces[i].CVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
            }

            m_BndCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BndCount; i++)
            {
                BoneBinding Binding = new BoneBinding();
                Binding.BoneIndex = Endian.SwapInt32(Reader.ReadInt32());
                Binding.FirstVertex = Endian.SwapInt32(Reader.ReadInt32());
                Binding.VertexCount = Endian.SwapInt32(Reader.ReadInt32());
                Binding.FirstBlendedVert = Endian.SwapInt32(Reader.ReadInt32());
                Binding.BlendedVertexCount = Endian.SwapInt32(Reader.ReadInt32());

                m_BoneBindings.Add(Binding);
            }

            m_RealVertexCount = Endian.SwapInt32(Reader.ReadInt32());
            m_TexVerticies = new Single[m_RealVertexCount, 3];

            for (int i = 0; i < m_RealVertexCount; i++)
            {
                m_TexVerticies[i, 0] = i;
                m_TexVerticies[i, 1] = Reader.ReadSingle();
                m_TexVerticies[i, 2] = Reader.ReadSingle();
            }

            m_BlendCount = Endian.SwapInt32(Reader.ReadInt32());

            for (int i = 0; i < m_BlendCount; i++)
            {
                BlendData Blend = new BlendData();
                Blend.WeightFixed = (float)(Endian.SwapInt32(Reader.ReadInt32())) / 0x8000;
                Blend.OtherVertexIndex = Endian.SwapInt32(Reader.ReadInt32());
                m_BlendData.Add(Blend);
            }

            m_TotalVertexCount = Endian.SwapInt32(Reader.ReadInt32());

            m_VertexData = new Single[m_TotalVertexCount, 6];
            m_TransformedVertices = new Vertex[m_TotalVertexCount];

            for (int i = 0; i < m_TotalVertexCount; i++)
            {
                m_VertexData[i, 0] = Reader.ReadSingle();
                m_VertexData[i, 1] = Reader.ReadSingle();
                m_VertexData[i, 2] = Reader.ReadSingle();
                //Normals
                m_VertexData[i, 3] = Reader.ReadSingle();
                m_VertexData[i, 4] = Reader.ReadSingle();
                m_VertexData[i, 5] = Reader.ReadSingle();

                if (i < m_RealVertexCount)
                {
                    if (m_TransformedVertices[i] == null)
                        m_TransformedVertices[i] = new Vertex();

                    //Fixed vertex
                    m_TransformedVertices[i].TextureCoord.X = m_TexVerticies[i, 1];
                    m_TransformedVertices[i].TextureCoord.Y = m_TexVerticies[i, 2];
                }
                else
                {
                    if (m_TransformedVertices[i] == null)
                        m_TransformedVertices[i] = new Vertex();

                    //Blended vertex
                    m_TransformedVertices[i].Blend.WeightFixed = m_BlendData[i - m_RealVertexCount].WeightFixed;
                    m_TransformedVertices[i].Blend.OtherVertexIndex = m_BlendData[i - m_RealVertexCount].OtherVertexIndex;
                }
            }
        }

        /// <summary>
        /// Transforms the vertices in a mesh to their location in 3D-space based on 
        /// the location of a bone.
        /// </summary>
        /// <param name="Bne">The bone to start with (should be a skeleton's ROOT bone).</param>
        /// <param name="Effect">The BasicEffect instance used for rendering.</param>
        public void TransformVertices2(Bone Bne, ref Matrix World)
        {
            int BoneIndex = 0;
            Matrix WorldMat = World * Bne.AbsoluteTransform;

            for (BoneIndex = 0; BoneIndex < m_BndCount; BoneIndex++)
            {
                if (Bne.BoneName == m_BoneNames[m_BoneBindings[BoneIndex].BoneIndex])
                    break;
            }

            if (BoneIndex < m_BndCount)
            {
                for (int i = 0; i < m_BoneBindings[BoneIndex].VertexCount; i++)
                {
                    int VertexIndex = m_BoneBindings[BoneIndex].FirstVertex + i;
                    Vector3 RelativeVertex = new Vector3(m_VertexData[VertexIndex, 0], 
                        m_VertexData[VertexIndex, 1], m_VertexData[VertexIndex, 2]);
                    Vector3 RelativeNormal = new Vector3(m_VertexData[VertexIndex, 3],
                        m_VertexData[VertexIndex, 4], m_VertexData[VertexIndex, 5]);

                    WorldMat *= Matrix.CreateTranslation(RelativeVertex);
                    
                    Vector3.Transform(RelativeVertex, WorldMat);
                    m_TransformedVertices[VertexIndex].Coord.X = WorldMat.M41;
                    m_TransformedVertices[VertexIndex].Coord.Y = WorldMat.M42;
                    m_TransformedVertices[VertexIndex].Coord.Z = WorldMat.M43;

                    WorldMat *= Matrix.CreateTranslation(new Vector3(-RelativeVertex.X, 
                        -RelativeVertex.Y, -RelativeVertex.Z));

                    WorldMat *= Matrix.CreateTranslation(RelativeNormal);

                    Vector3.TransformNormal(RelativeNormal, WorldMat);
                    m_TransformedVertices[VertexIndex].Normal.X = WorldMat.M41;
                    m_TransformedVertices[VertexIndex].Normal.Y = WorldMat.M42;
                    m_TransformedVertices[VertexIndex].Normal.Z = WorldMat.M43;

                    WorldMat *= Matrix.CreateTranslation(new Vector3(-RelativeNormal.X,
                        -RelativeNormal.Y, -RelativeNormal.Z));
                }

                for (int i = 0; i < m_BoneBindings[BoneIndex].BlendedVertexCount; i++)
                {
                    int VertexIndex = m_RealVertexCount + m_BoneBindings[BoneIndex].FirstBlendedVert + i;
                    Vector3 RelativeVertex = new Vector3(m_VertexData[VertexIndex, 0], 
                        m_VertexData[VertexIndex, 1], m_VertexData[VertexIndex, 2]);
                    Vector3 RelativeNormal = new Vector3(m_VertexData[VertexIndex, 3], 
                        m_VertexData[VertexIndex, 4], m_VertexData[VertexIndex, 5]);

                    WorldMat *= Matrix.CreateTranslation(RelativeVertex);

                    Vector3.Transform(RelativeVertex, WorldMat);
                    m_TransformedVertices[VertexIndex].Coord.X = WorldMat.M41;
                    m_TransformedVertices[VertexIndex].Coord.Y = WorldMat.M42;
                    m_TransformedVertices[VertexIndex].Coord.Z = WorldMat.M43;

                    WorldMat *= Matrix.CreateTranslation(new Vector3(-RelativeVertex.X,
                        -RelativeVertex.Y, -RelativeVertex.Z));

                    WorldMat *= Matrix.CreateTranslation(RelativeNormal);

                    Vector3.TransformNormal(RelativeNormal, WorldMat);
                    m_TransformedVertices[VertexIndex].Normal.X = WorldMat.M41;
                    m_TransformedVertices[VertexIndex].Normal.Y = WorldMat.M42;
                    m_TransformedVertices[VertexIndex].Normal.Z = WorldMat.M43;

                    WorldMat *= Matrix.CreateTranslation(new Vector3(-RelativeNormal.X,
                        -RelativeNormal.Y, -RelativeNormal.Z));
                }
            }

            if (Bne.NumChildren == 1)
                TransformVertices2(Bne.Children[0], ref World);
            else if (Bne.NumChildren > 1)
            {
                for (int i = 0; i < Bne.NumChildren; i++)
                    TransformVertices2(Bne.Children[i], ref World);
            }
        }

        /// <summary>
        /// This is an important function. I have no idea what it does, but it helps
        /// render body meshes correctly. This function was ported from SimPose 8.
        /// </summary>
        /// <param name="TransformedVector1">The first transformed vector.</param>
        /// <param name="TransformedVector2">The second transformed vector.</param>
        /// <param name="Target">The target vector.</param>
        /// <param name="Weight">The weight (last quaternion in a bone's array of quaternions).</param>
        public void Blend(Vector3 TransformedVector1, 
            Vector3 TransformedVector2, ref VertexPositionNormalTexture Target, float Weight)
        {
            float FloatingPointDelta = 0.000001f;
            float Weight1 = 1.0f - Weight;

            Target.Position.X = Weight * TransformedVector1.X + Weight1 * Target.Position.X;
            Target.Position.Y = Weight * TransformedVector1.Y + Weight1 * Target.Position.Y;
            Target.Position.Z = Weight * TransformedVector1.Z + Weight1 * Target.Position.Z;

            //Normalize the target's normal.
            double Length = Math.Sqrt((Target.Normal.X * Target.Normal.X) +
                                      (Target.Normal.Y * Target.Normal.Y) +
                                      (Target.Normal.Z * Target.Normal.Z));
            double Factor = (Math.Abs(Length) > FloatingPointDelta) ? Length : 1.0f;
            Target.Normal.X /= (float)Factor;
            Target.Normal.Y /= (float)Factor;
            Target.Normal.Z /= (float)Factor;
        }

        /// <summary>
        /// Blends the vertices in this mesh. Should be called directly after TransformVertices2().
        /// </summary>
        public void BlendVertices2()
        {
            for (int i = 0; i < m_BlendCount; i++)
            {
                Vertex BlendVertex = m_TransformedVertices[m_RealVertexCount + i];
                float Weight = BlendVertex.Blend.WeightFixed;
                Vertex RealVertex = m_TransformedVertices[BlendVertex.Blend.OtherVertexIndex];

                RealVertex.Coord.X = Weight * BlendVertex.Coord.X + (1 - Weight) * RealVertex.Coord.X;
                RealVertex.Coord.Y = Weight * BlendVertex.Coord.Y + (1 - Weight) * RealVertex.Coord.Y;
                RealVertex.Coord.Z = Weight * BlendVertex.Coord.Z + (1 - Weight) * RealVertex.Coord.Z;
                RealVertex.Normal.X = Weight * BlendVertex.Normal.X + (1 - Weight) * RealVertex.Normal.X;
                RealVertex.Normal.Y = Weight * BlendVertex.Normal.Y + (1 - Weight) * RealVertex.Normal.Y;
                RealVertex.Normal.Z = Weight * BlendVertex.Normal.Z + (1 - Weight) * RealVertex.Normal.Z;

                m_TransformedVertices[BlendVertex.Blend.OtherVertexIndex] = RealVertex;
            }
        }

        /// <summary>
        /// Advances the frame of an animation for a skeleton used on this mesh.
        /// </summary>
        /// <param name="Skel">A skeleton used to render this mesh.</param>
        /// <param name="Animation">The animation to advance.</param>
        /// <param name="AnimationTime">The playback time for an animation (how long has it been playing for?)</param>
        /// <param name="TimeDelta">The timedelta of the rendering loop.</param>
        public void AdvanceFrame(Skeleton Skel, Anim Animation, ref float AnimationTime, float TimeDelta)
        {
            float Duration = (float)Animation.Motions[0].NumFrames / 30;
            AnimationTime += TimeDelta;
            AnimationTime = AnimationTime % Duration; //Loop the animation

            for (int i = 0; i < Animation.Motions.Count; i++)
            {
                int BoneIndex = Skel.FindBone(Animation.Motions[i].BoneName);

                if (BoneIndex == -1)
                    continue;

                Bone Bne = Skel.Bones[BoneIndex];

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

                    Bne.GlobalTranslation = UpdatedTranslation;
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
                }
            }
        }

        private float DotProduct(Quaternion Rotation1, Quaternion Rotation2)
        {
            return Rotation1.X * Rotation2.X + Rotation1.Y * Rotation2.Y + Rotation1.Z * Rotation2.Z + 
                Rotation1.W * Rotation2.W;
        }

        /// <summary>
        /// Processes the loaded mesh's data and populates an array of
        /// VertexPositionNormalTexture elements that can be looped to
        /// render the mesh.
        /// </summary>
        public void ProcessMesh()
        {
            VertexPositionNormalTexture[] NormVerticies = new VertexPositionNormalTexture[m_TotalVertexCount];

            for (int i = 0; i < m_TotalVertexCount; i++)
            {
                NormVerticies[i] = new VertexPositionNormalTexture();
                NormVerticies[i].Position.X = m_VertexData[i, 0];
                NormVerticies[i].Position.Y = m_VertexData[i, 1];
                NormVerticies[i].Position.Z = m_VertexData[i, 2];
                NormVerticies[i].Normal.X = m_VertexData[i, 3];
                NormVerticies[i].Normal.Y = m_VertexData[i, 4];
                NormVerticies[i].Normal.Z = m_VertexData[i, 5];


                //Not really sure why this is important, but I think it has something to do
                //with being able to see the texture.
                //NormVerticies[i].Normal.Normalize();
            }

            for (int i = 0; i < m_RealVertexCount; i++)
            {
                NormVerticies[i].TextureCoordinate.X = m_TexVerticies[i, 1];
                NormVerticies[i].TextureCoordinate.Y = m_TexVerticies[i, 2];
            }

            m_VertexNTexPositions = NormVerticies;
        }
    }
}
