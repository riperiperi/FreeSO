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
using FSO.Files.Utils;
using FSO.Vitaboy.Model;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Skeletons specify the network of bones that can be moved by an animation to bend 
    /// the applied meshes of a rendered character. Skeletons also provide non-animated 
    /// default translation and rotation values for each bone.
    /// </summary>
    public class Skeleton 
    {
        public string Name;
        public Bone[] Bones;
        public Bone RootBone;

        /// <summary>
        /// Gets a bone from this Skeleton instance.
        /// </summary>
        /// <param name="name">The name of a bone.</param>
        /// <returns>A Bone instance corresponding to the supplied name.</returns>
        public Bone GetBone(string name)
        {
            return Bones.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Clones this skeleton.
        /// </summary>
        /// <returns>A Skeleton instance with the same data as this one.</returns>
        public Skeleton Clone()
        {
            var result = new Skeleton();
            result.Name = this.Name;
            result.Bones = new Bone[Bones.Length];

            for (int i = 0; i < Bones.Length; i++){
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

        /// <summary>
        /// Reads a skeleton from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a skeleton.</param>
        public void Read(Stream stream, bool bcf)
        {
            using (var io = IoBuffer.FromStream(stream, bcf ? ByteOrder.LITTLE_ENDIAN : ByteOrder.BIG_ENDIAN))
            {
                if (!bcf)
                {
                    var version = io.ReadUInt32();
                }
                Name = io.ReadPascalString();

                var boneCount = io.ReadInt16();

                Bones = new Bone[boneCount];
                for (var i = 0; i < boneCount; i++)
                {
                    Bone bone = ReadBone(io, bcf);
                    if (bone == null)
                    {
                        i--;
                        continue;
                    }
                    bone.Index = i;
                    Bones[i] = bone;
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

        /// <summary>
        /// Reads a bone from a IOBuffer.
        /// </summary>
        /// <param name="reader">An IOBuffer instance used to read from a stream holding a skeleton.</param>
        /// <returns>A Bone instance.</returns>
        private Bone ReadBone(IoBuffer reader, bool bcf)
        {
            var bone = new Bone();
            if (!bcf) bone.Unknown = reader.ReadInt32();
            bone.Name = reader.ReadPascalString();
            bone.ParentName = reader.ReadPascalString();
            bone.HasProps = bcf || reader.ReadByte() > 0;
            if (bcf && bone.Name == "") return null;
            if (bone.HasProps)
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
            bone.Rotation = new Quaternion(
                reader.ReadFloat(),
                -reader.ReadFloat(),
                -reader.ReadFloat(),
                -reader.ReadFloat()
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
            var rotationMatrix = Matrix.CreateFromQuaternion(bone.Rotation);

            var myWorld = (rotationMatrix * translateMatrix)*world;
            bone.AbsolutePosition = Vector3.Transform(Vector3.Zero, myWorld);
            bone.AbsoluteMatrix = myWorld;

            foreach (var child in bone.Children)
            {
                ComputeBonePositions(child, myWorld);
            }
        }
    }
}
