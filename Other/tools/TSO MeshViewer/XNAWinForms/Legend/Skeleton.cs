/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using System.Windows.Forms;
using LogThis;

namespace Dressup
{
    public class Bone
    {
        private Skeleton m_Skel;

        public int ID; //This is assigned when reading a skeleton, to make it easier to look up bones.
        public string BoneName;
        public string ParentName;
        public byte HasPropertyList;

        public PropertyList PList = new PropertyList();

        //Little Endian!
        public float[] Translations;
        public float[] Versor;

        //Back to Big Endian (Maxis...!)
        public int CanTranslate;
        public int CanRotate;
        public int CanUseBlending;
        public float CanWiggle;
        public float WiggleAmount;

        public Bone Parent;
        public int NumChildren = 0;
        public int[] Children;

        public float[,] m_BlendedVertices;

        public BasicEffect BoneEffect;
        private Matrix m_AbsoluteTransform;

        private Vector3 m_Scale = Vector3.One;

        public Bone(Skeleton Skel)
        {
            m_Skel = Skel;
        }

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
                ComputeAbsoluteTransform(m_Skel.WorldMatrix);
                
                return m_AbsoluteTransform;
            }
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
                return /*Quaternion.CreateFromAxisAngle(Vector3.UnitX, Versor[0]) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitY, Versor[1]) *
                    Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Versor[2]);*/
                    new Quaternion(Versor[0], Versor[1], Versor[2], Versor[3]);
            }
        }

        /// <summary>
        /// Compute the absolute transformation for this bone.
        /// </summary>
        /// <param name="WorldMatrix">The worldmatrix, so that the bone can be positioned correctly.</param>
        public void ComputeAbsoluteTransform(Matrix WorldMatrix)
        {
            if (Parent != null)
            {
                //NOTE: Seems like Parent.AbsoluteTransform should be at the END of this equation, NOT at the
                //      beginning. Same goes for below...
                m_AbsoluteTransform = Matrix.CreateFromQuaternion(GlobalRotation) * Matrix.CreateTranslation(GlobalTranslation) * Parent.AbsoluteTransform;
            }
            //This bone didn't have a parent, which means it is probably the root bone.
            else
                m_AbsoluteTransform =  Matrix.CreateFromQuaternion(GlobalRotation) * Matrix.CreateTranslation(GlobalTranslation * WorldMatrix.Translation);
        }
    }

    public class Skeleton
    {
        private uint m_Version;
        private string m_Name;

        private ushort m_BoneCount;
        private Bone[] m_Bones;

        private Matrix m_WorldMatrix;

        /// <summary>
        /// The bones in this skeleton.
        /// </summary>
        public Bone[] Bones
        {
            get { return m_Bones; }
        }

        public Matrix WorldMatrix
        {
            get { return m_WorldMatrix; }
        }

        public Skeleton(GraphicsDevice Device, string Filepath, ref Matrix WorldMatrix)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Filepath, FileMode.Open));
            m_WorldMatrix = WorldMatrix;

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Name = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

            m_BoneCount = Endian.SwapUInt16(Reader.ReadUInt16());
            m_Bones = new Bone[m_BoneCount];

            for (int i = 0; i < m_BoneCount; i++)
            {
                Endian.SwapUInt32(Reader.ReadUInt32()); //1 in hexadecimal... typical useless Maxis value...

                Bone Bne = new Bone(this);

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

                Bne.Versor = new float[4];
                //These values are given in degrees...
                Bne.Versor[0] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[1] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[2] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[3] = MathHelper.ToRadians(Reader.ReadSingle());

                Bne.CanTranslate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanRotate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanUseBlending = Endian.SwapInt32(Reader.ReadInt32());
                //Little endian.
                Bne.CanWiggle = Reader.ReadSingle();
                Bne.WiggleAmount = Reader.ReadSingle();

                Bne.BoneEffect = new BasicEffect(Device, null);

                Bne.Children = new int[m_BoneCount - i - 1];

                int Parent = FindBone(Bne.ParentName, i);
                if (Parent != -1)
                {
                    m_Bones[Parent].Children[m_Bones[Parent].NumChildren] = Bne.ID;
                    m_Bones[Parent].NumChildren += 1;
                    Bne.Parent = m_Bones[Parent];
                    Bne.ComputeAbsoluteTransform(m_WorldMatrix);
                }

                m_Bones[i] = Bne;
            }

            Reader.Close();
        }

        public Skeleton(GraphicsDevice Device, byte[] Filedata, Matrix WorldMatrix)
        {
            MemoryStream MemStream = new MemoryStream(Filedata);
            BinaryReader Reader = new BinaryReader(MemStream);
            m_WorldMatrix = WorldMatrix;

            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Name = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

            m_BoneCount = Endian.SwapUInt16(Reader.ReadUInt16());
            m_Bones = new Bone[m_BoneCount];

            for (int i = 0; i < m_BoneCount; i++)
            {
                Endian.SwapUInt32(Reader.ReadUInt32()); //1 in hexadecimal... typical useless Maxis value...

                Bone Bne = new Bone(this);

                Bne.ID = i;

                Bne.BoneName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));
                Log.LogThis("BoneName: " + Bne.BoneName, eloglevel.info);
                Bne.ParentName = Encoding.ASCII.GetString(Reader.ReadBytes(Reader.ReadByte()));

                Bne.HasPropertyList = Reader.ReadByte();

                if (Bne.HasPropertyList == 1)
                    Bne.PList = ReadPropList(Reader);

                //Little Endian
                Bne.Translations = new float[3];
                Bne.Translations[0] = Reader.ReadSingle();
                Bne.Translations[1] = Reader.ReadSingle();
                Bne.Translations[2] = Reader.ReadSingle();

                Bne.Versor = new float[4];
                //These values are given in degrees...
                Bne.Versor[0] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[1] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[2] = MathHelper.ToRadians(Reader.ReadSingle());
                Bne.Versor[3] = MathHelper.ToRadians(Reader.ReadSingle());

                Bne.CanTranslate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanRotate = Endian.SwapInt32(Reader.ReadInt32());
                Bne.CanUseBlending = Endian.SwapInt32(Reader.ReadInt32());
                //Little endian.
                Bne.CanWiggle = Reader.ReadSingle();
                Bne.WiggleAmount = Reader.ReadSingle();

                Bne.BoneEffect = new BasicEffect(Device, null);

                Bne.Children = new int[m_BoneCount - i - 1];

                int Parent = FindBone(Bne.ParentName, i);
                if (Parent != -1)
                {
                    m_Bones[Parent].Children[m_Bones[Parent].NumChildren] = Bne.ID;
                    m_Bones[Parent].NumChildren += 1;
                    Bne.Parent = m_Bones[Parent];
                    Bne.ComputeAbsoluteTransform(m_WorldMatrix);
                }

                m_Bones[i] = Bne;

                /*Log.LogThis("Bone: " + Bne.BoneName, eloglevel.info);
                if (Parent != -1)
                {
                    Log.LogThis("Parent: " + Bne.Parent.BoneName, eloglevel.info);

                    for (int j = 0; j < Bne.Parent.NumChildren; j++)
                        Log.LogThis("Child: " + Bne.Parent.Children[j].BoneName, eloglevel.info);
                }
                else
                    Log.LogThis("Parent: NULL", eloglevel.info);*/
            }

            Reader.Close();
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
        public int FindBone(string BoneName, int Index)
        {
            for (int i = 0; i < Index; i++)
            {
                if (BoneName == m_Bones[i].BoneName)
                    return i;
            }

            return -1;
        }
    }
}
