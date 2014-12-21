/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace SimsLib.ThreeD
{
    /// <summary>
    /// Skeletons specify the network of bones that can be moved by an animation to bend the applied meshes of a 
    /// rendered character. Skeletons also provide non-animated default translation and rotation values for each bone, 
    /// for convenient editing in 3DS Max by the artists of Maxis, which are used only for Create-a-Sim (in both games) 
    /// and character pages.
    /// </summary>
    public class Skeleton
    {
        public string Name;
        public Bone[] Bones;
        public Bone RootBone;

        public Bone GetBone(string name)
        {
            return Bones.FirstOrDefault(x => x.Name == name);
        }

        public Skeleton Clone()
        {
            var result = new Skeleton();
            result.Name = this.Name;
            result.Bones = new Bone[Bones.Length];

            for (int i = 0; i < Bones.Length; i++)
            {
                result.Bones[i] = Bones[i].Clone();
            }

            /** Construct tree **/
            foreach (var bone in result.Bones)
            {
                bone.Children = result.Bones.Where(x => x.ParentName == bone.Name).ToArray();
            }
            result.RootBone = result.Bones.FirstOrDefault(x => x.ParentName == "NULL");
            result.ComputeBonePositions(result.RootBone, Matrix.Identity);
            return result;
        }

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream))
            {
                var version = io.ReadUInt32();
                Name = io.ReadPascalString();

                var boneCount = io.ReadInt16();

                Bones = new Bone[boneCount];
                for (var i = 0; i < boneCount; i++)
                {
                    Bones[i] = ReadBone(io);
                }

                /** Construct tree **/
                foreach (var bone in Bones)
                {
                    bone.Children = Bones.Where(x => x.ParentName == bone.Name).ToArray();
                }
                RootBone = Bones.FirstOrDefault(x => x.ParentName == "NULL");
                ComputeBonePositions(RootBone, Matrix.Identity);
            }
        }


        private Bone ReadBone(IoBuffer reader)
        {
            var bone = new Bone();
            bone.Unknown = reader.ReadInt32();
            bone.Name = reader.ReadPascalString();
            bone.ParentName = reader.ReadPascalString();
            bone.HasProps = reader.ReadByte();
            if (bone.HasProps != 0)
            {
                var propertyCount = reader.ReadInt32();
                var property = new PropertyListItem();

                for (var i = 0; i < propertyCount; i++)
                {
                    var pairCount = reader.ReadInt32();
                    for (var x = 0; x < pairCount; x++)
                    {
                        property.KeyPairs.Add(new KeyValuePair<string, string>(
                            reader.ReadPascalString(),
                            reader.ReadPascalString()
                        ));
                    }
                }
                bone.Properties.Add(property);
            }

            var xx = -reader.ReadFloat();
            bone.Translation = new Vector3(
                xx,
                reader.ReadFloat(),
                reader.ReadFloat()
            );
            bone.Rotation = new Vector4(
                reader.ReadFloat(),
                -reader.ReadFloat(),
                -reader.ReadFloat(),
                reader.ReadFloat()
            );
            bone.CanTranslate = reader.ReadInt32();
            bone.CanRotate = reader.ReadInt32();
            bone.CanBlend = reader.ReadInt32();
            bone.WiggleValue = reader.ReadFloat();
            bone.WigglePower = reader.ReadFloat();
            return bone;
        }

        /// <summary>
        /// Computes the absolute position for all the bones in this skeleton.
        /// </summary>
        /// <param name="bone">The bone to start with, should always be the ROOT bone.</param>
        /// <param name="world">A world matrix to use in the calculation.</param>
        public void ComputeBonePositions(Bone bone, Matrix world)
        {
            var translateMatrix = Matrix.CreateTranslation(bone.Translation);
            var rotationMatrix = FindQuaternionMatrix(bone.Rotation);

            var myWorld = (rotationMatrix * translateMatrix) * world;
            bone.AbsolutePosition = Vector3.Transform(Vector3.Zero, myWorld);
            bone.AbsoluteMatrix = myWorld;

            foreach (var child in bone.Children)
            {
                ComputeBonePositions(child, myWorld);
            }
        }

        private Matrix FindQuaternionMatrix(Vector4 Quaternion)
        {
            float x2 = Quaternion.X * Quaternion.X;
            float y2 = Quaternion.Y * Quaternion.Y;
            float z2 = Quaternion.Z * Quaternion.Z;
            float xy = Quaternion.X * Quaternion.Y;
            float xz = Quaternion.X * Quaternion.Z;
            float yz = Quaternion.Y * Quaternion.Z;
            float wx = Quaternion.W * Quaternion.X;
            float wy = Quaternion.W * Quaternion.Y;
            float wz = Quaternion.W * Quaternion.Z;

            var mtxIn = new Matrix();

            mtxIn.M11 = 1.0f - 2.0f * (y2 + z2);
            mtxIn.M12 = 2.0f * (xy - wz);
            mtxIn.M13 = 2.0f * (xz + wy);
            mtxIn.M14 = 0.0f;
            mtxIn.M21 = 2.0f * (xy + wz);
            mtxIn.M22 = 1.0f - 2.0f * (x2 + z2);
            mtxIn.M23 = 2.0f * (yz - wx);
            mtxIn.M24 = 0.0f;
            mtxIn.M31 = 2.0f * (xz - wy);
            mtxIn.M32 = 2.0f * (yz + wx);
            mtxIn.M33 = 1.0f - 2.0f * (x2 + y2);
            mtxIn.M34 = 0.0f;
            mtxIn.M41 = 0.0f;
            mtxIn.M42 = 0.0f;
            mtxIn.M43 = 0.0f;
            mtxIn.M44 = 1.0f;

            return mtxIn;
        }
    }
}