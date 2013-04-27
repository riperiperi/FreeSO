using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace LibVitaBoy
{
    public class Skelenton
    {
        public int Version;
        public string Name;

        public int BoneCount;
        public Bone[] Bones;

        public Bone RootBone;



        public void Read(byte[] data)
        {
            using(var reader = new VBFile(new MemoryStream(data))){
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


        private Bone ReadBone(VBFile reader)
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

            if (bone.Name == "ROOT")
            {
                var y = true;
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
