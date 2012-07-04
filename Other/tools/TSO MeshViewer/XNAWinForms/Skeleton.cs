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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LogThis;

namespace XNAWinForms
{
    public class Bone
    {
        public int ID; //This is assigned when reading a skeleton, to make it easier to look up bones.
        public string BoneName;
        public string ParentName;
        public byte HasPropertyList;

        public PropertyList PList = new PropertyList();

        //Little Endian!
        public float[] Translations;
        public float[] Quaternions;

        //Back to Big Endian (Maxis...!)
        public int CanTranslate;
        public int CanRotate;
        public int CanUseBlending;
        public float CanWiggle;
        public float WiggleAmount;

        public Bone Parent;
        public int NumChildren = 0;
        public Bone[] Children;

        public float[,] m_BlendedVertices;

        public BasicEffect BoneEffect;
        private Matrix m_AbsoluteTransform;

        private Vector3 m_Scale = Vector3.One;

        public float[,] BlendedVertices
        {
            get { return m_BlendedVertices; }
            set { m_BlendedVertices = value; }
        }

        /// <summary>
        /// The absolute transform for this bone.
        /// </summary>
        public Matrix AbsoluteTransform
        {
            get
            {
                ComputeAbsoluteTransform();
                
                return m_AbsoluteTransform;
            }

            set { m_AbsoluteTransform = value; }
        }

        /// <summary>
        /// The global translation of this bone.
        /// </summary>
        public Vector3 GlobalTranslation
        {
            get
            {
                return new Vector3(Translations[0], Translations[1], Translations[2]);
            }

            set
            {
                Translations[0] = value.X;
                Translations[1] = value.Y;
                Translations[2] = value.Z;
            }
        }

        /// <summary>
        /// The global rotation of this bone.
        /// </summary>
        public Quaternion GlobalRotation
        {
            get
            {
                return new Quaternion(Quaternions[0], Quaternions[1], Quaternions[2], Quaternions[3]);
            }
        }

        /// <summary>
        /// Compute the absolute transformation for this bone.
        /// </summary>
        public void ComputeAbsoluteTransform()
        {
            //Should the Z-axis be reversed??
            Matrix GlobalRot = Matrix.CreateFromQuaternion(GlobalRotation);
            Vector3 Back = GlobalRot.Backward;
            GlobalRot.Backward = Vector3.Negate(Back);

            if (Parent != null) 
            {
                m_AbsoluteTransform = Parent.AbsoluteTransform * Matrix.CreateFromQuaternion(GlobalRotation) * Matrix.CreateTranslation(GlobalTranslation);
            }
            //This bone didn't have a parent, which means it is probably the root bone.
            else
            {
                m_AbsoluteTransform = Matrix.CreateFromQuaternion(GlobalRotation) * Matrix.CreateTranslation(GlobalTranslation);
            }
        }
    }

    public class Skeleton
    {
        private uint m_Version;
        private string m_Name;

        private ushort m_BoneCount;
        private Bone[] m_Bones;

        /// <summary>
        /// The bones in this skeleton.
        /// </summary>
        public Bone[] Bones
        {
            get { return m_Bones; }
        }

        /// <summary>
        /// Gets all the rotations for all the bones in this skeleton.
        /// </summary>
        /// <returns>An array of quaternions containing rotations.</returns>
        public Vector3[] GetTranslations()
        {
            Vector3[] Translations = new Vector3[m_BoneCount];

            for (int i = 0; i < m_Bones.Length; i++)
                Translations[i] = Bones[i].GlobalTranslation;

            return Translations;
        }

        public Skeleton(GraphicsDevice Device, string Filepath)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Filepath, FileMode.Open));

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Name = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

            m_BoneCount = Endian.SwapUInt16(Reader.ReadUInt16());
            m_Bones = new Bone[m_BoneCount];

            for (int i = 0; i < m_BoneCount; i++)
            {
                Endian.SwapUInt32(Reader.ReadUInt32()); //1 in hexadecimal... typical useless Maxis value...

                Bone Bne = new Bone();

                Bne.ID = i;

                Bne.BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                Bne.ParentName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

                Bne.HasPropertyList = Reader.ReadByte();

                if (Bne.HasPropertyList == 1)
                    Bne.PList = ReadPropList(Reader);

                //Little Endian
                Bne.Translations = new float[3];
                Bne.Translations[0] = Reader.ReadSingle();
                Bne.Translations[1] = Reader.ReadSingle();
                Bne.Translations[2] = Reader.ReadSingle();

                Bne.Quaternions = new float[4];
                Bne.Quaternions[0] = Reader.ReadSingle();
                Bne.Quaternions[1] = Reader.ReadSingle();
                Bne.Quaternions[2] = Reader.ReadSingle();
                Bne.Quaternions[3] = Reader.ReadSingle();

                Bne.CanTranslate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanRotate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanUseBlending = Endian.SwapInt32(Reader.ReadInt32());
                //Little endian.
                Bne.CanWiggle = Reader.ReadSingle();
                Bne.WiggleAmount = Reader.ReadSingle();

                Bne.BoneEffect = new BasicEffect(Device, null);

                Bne.Children = new Bone[m_BoneCount - i - 1];

                m_Bones[i] = Bne;
            }

            for (int i = 0; i < m_Bones.Length; i++)
            {
                for (int j = 0; j < m_Bones.Length; j++)
                {
                    if (m_Bones[i].ParentName == m_Bones[j].BoneName)
                        m_Bones[i].Parent = m_Bones[j];
                }
            }

            foreach (Bone Bne in m_Bones)
            {
                Bne.ComputeAbsoluteTransform();
            }

            Reader.Close();
        }

        public Skeleton(GraphicsDevice Device, byte[] Filedata)
        {
            MemoryStream MemStream = new MemoryStream(Filedata);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Name = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

            m_BoneCount = Endian.SwapUInt16(Reader.ReadUInt16());
            m_Bones = new Bone[m_BoneCount];

            for (int i = 0; i < m_BoneCount; i++)
            {
                Endian.SwapUInt32(Reader.ReadUInt32()); //1 in hexadecimal... typical useless Maxis value...

                Bone Bne = new Bone();

                Bne.ID = i;

                Bne.BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                Bne.ParentName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

                Bne.HasPropertyList = Reader.ReadByte();

                if (Bne.HasPropertyList == 1)
                    Bne.PList = ReadPropList(Reader);

                //Little Endian
                Bne.Translations = new float[3];
                Bne.Translations[0] = Reader.ReadSingle();
                Bne.Translations[1] = Reader.ReadSingle();
                Bne.Translations[2] = Reader.ReadSingle();

                Bne.Quaternions = new float[4];
                Bne.Quaternions[0] = Reader.ReadSingle();
                Bne.Quaternions[1] = Reader.ReadSingle();
                Bne.Quaternions[2] = Reader.ReadSingle();
                Bne.Quaternions[3] = Reader.ReadSingle();

                Bne.CanTranslate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanRotate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanUseBlending = Endian.SwapInt32(Reader.ReadInt32());
                //Little endian.
                Bne.CanWiggle = Reader.ReadSingle();
                Bne.WiggleAmount = Reader.ReadSingle();

                Bne.BoneEffect = new BasicEffect(Device, null);

                Bne.Children = new Bone[m_BoneCount - i - 1];

                m_Bones[i] = Bne;
            }

            for(int i = 0; i < m_Bones.Length; i++)
            {
                for(int j = 0; j < m_Bones.Length; j++)
                {
                    if (m_Bones[i].ParentName == m_Bones[j].BoneName)
                        m_Bones[i].Parent = m_Bones[j];
                }
            }

            foreach (Bone Bne in m_Bones)
            {
                Bne.ComputeAbsoluteTransform();
            }

            Reader.Close();
        }

        /// <summary>
        /// Assigns children to all the bones in a skeleton.
        /// </summary>
        /// <param name="Skel">The skeleton to whose bones to assign children to.</param>
        public void AssignChildren(ref Skeleton Skel)
        {
            for(int i = 0; i < Skel.Bones.Length; i++)
            {
                for (int j = 0; j < m_Bones.Length; j++)
                {
                    if (m_Bones[j].ParentName == Skel.Bones[i].BoneName)
                    {
                        //Skel.Bones[i] is the parent of m_Bones[j]
                        Skel.Bones[i].Children[Skel.Bones[i].NumChildren] = m_Bones[j];
                        Skel.Bones[i].NumChildren++;
                    }
                }
            }
        }

        /// <summary>
        /// Reads a list of properties from the *.skel file.
        /// </summary>
        /// <param name="Reader">The BinaryReader instance used to read the *.skel file.</param>
        /// <returns>A PropertyList instance filled with properties.</returns>
        private PropertyList ReadPropList(BinaryReader Reader)
        {
            PropertyList PList = new PropertyList();
            PList.PropsCount = Endian.SwapUInt32(Reader.ReadUInt32());

            for (int j = 0; j < PList.PropsCount; j++)
            {
                Property Prop = new Property();

                uint PairsCount = Endian.SwapUInt32(Reader.ReadUInt32());

                for (int k = 0; k < PairsCount; k++)
                {
                    Prop.Key = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                    Prop.Value = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                }

                PList.PList.Add(Prop);
            }

            return PList;
        }

        /// <summary>
        /// Finds a bone in this skeleton with the specified name.
        /// </summary>
        /// <param name="BoneName">The name of the bone to find.</param>
        /// <returns>The index of the bone in this skeleton's list of bones, or -1 if the bone wasn't found.</returns>
        public int FindBone(string BoneName)
        {
            for (int i = 0; i < m_Bones.Length; i++)
            {
                if (BoneName == m_Bones[i].BoneName)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Replaces a bone in this skeleton's list of bones with a specific bone.
        /// </summary>
        /// <param name="Index">The index of the bone to replace/update.</param>
        /// <param name="Bne">The bone with which to replace the bone at the specified index.</param>
        public void UpdateBone(int Index, Bone Bne)
        {
            m_Bones[Index] = Bne;
        }
    }
}
