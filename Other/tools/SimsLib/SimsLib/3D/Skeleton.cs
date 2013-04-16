/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is TSO Dressup.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ddfzcsm.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SimsLib.ThreeD
{
    public class Skeleton
    {
        public int Version;
        public string Name;

        public int BoneCount;
        public Bone[] Bones;

        public Bone RootBone;

        public void Read(byte[] data)
        {
            using(var reader = new VBReader(new MemoryStream(data))){
                Version = reader.ReadInt32();
                Name = reader.ReadPascalString();
                BoneCount = reader.ReadInt16();

                System.Diagnostics.Debug.WriteLine("========== Skeleton ==========");
                System.Diagnostics.Debug.WriteLine("Version: " + Version);
                System.Diagnostics.Debug.WriteLine("Name: " + Name);
                System.Diagnostics.Debug.WriteLine("BoneCount: " + BoneCount);

                Bones = new Bone[BoneCount];
                for (var i = 0; i < BoneCount; i++){
                    System.Diagnostics.Debug.WriteLine("\n [Bone " + i + "]");
                    Bones[i] = ReadBone(reader);
                }


                /** Construct tree **/
                foreach (var bone in Bones){
                    bone.Children = Bones.Where(x => x.ParentName == bone.Name).ToArray();
                }

                RootBone = Bones.FirstOrDefault(x => x.ParentName == "NULL");
            }
        }

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

        private Bone ReadBone(VBReader reader)
        {
            var bone = new Bone();
            bone.Unknown = reader.ReadInt32();
            bone.Name = reader.ReadPascalString();
            bone.ParentName = reader.ReadPascalString();

            System.Diagnostics.Debug.WriteLine("Name: " + bone.Name);
            System.Diagnostics.Debug.WriteLine("ParentName: " + bone.ParentName);

            bone.HasProps = reader.ReadByte();
            if (bone.HasProps != 0)
            {
                var propertyCount = reader.ReadInt32();
                var property = new PropertyListItem();
                
                for (var i = 0; i < propertyCount; i++){
                    var pairCount = reader.ReadInt32();
                    for (var x = 0; x < pairCount; x++){
                        property.KeyPairs.Add(new KeyValuePair<string, string>(
                            reader.ReadPascalString(),
                            reader.ReadPascalString()
                        ));
                    }
                }
                bone.Properties.Add(property);
            }

            /*if (bone.Name == "ROOT")
            {
                var y = true;
            }*/
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
    }


    public class PropertyListItem
    {
        public List<KeyValuePair<string, string>> KeyPairs = new List<KeyValuePair<string, string>>();
    }

    public class Bone
    {
        public int Unknown;
        public string Name;
        public string ParentName;

        public byte HasProps;
        public List<PropertyListItem> Properties = new List<PropertyListItem>();

        public Vector3 Translation;
        public Vector4 Rotation;

        public int CanTranslate;
        public int CanRotate;
        public int CanBlend;

        public float WiggleValue;
        public float WigglePower;

        public Bone[] Children;


        //Dummy & debug
        public Vector3 AbsolutePosition;
        public Matrix AbsoluteMatrix;

    }
}
