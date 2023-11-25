using FSO.Files.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Files.RC
{
    /// <summary>
    /// Neighbourhood model. Defines the 3D positions of all houses, and provides a model.
    /// There is a grass layer rendered with the grass shader, and various other layers drawn with textures.
    /// </summary>
    public class NBHm
    {
        public static int CURRENT_VERSION = 1;
        public int Version = CURRENT_VERSION;
        public Dictionary<short, NBHmHouse> Houses = new Dictionary<short, NBHmHouse>();
        
        public NBHm(OBJ obj)
        {
            foreach (var group in obj.FacesByObjgroup)
            {
                if (group.Key[0] == 'p')
                {
                    //lot group
                    var split = group.Key.Split('_');
                    if (split.Length > 1 && split[1] == "floor")
                    {
                        Vector2 minLocation = new Vector2(999999999, 999999999);
                        var candidates = new List<Vector3>();
                        foreach (var inds in group.Value)
                        {
                            var vtx = obj.Vertices[inds[0]-1];
                            if (vtx.X < minLocation.X) { minLocation.X = vtx.X; candidates.Clear(); }
                            if (vtx.Z < minLocation.Y) { minLocation.Y = vtx.Z; candidates.Clear(); }
                            if ((int)vtx.Z == (int)minLocation.Y && (int)vtx.X == (int)minLocation.X) candidates.Add(vtx);
                        }

                        var minLocation3 = new Vector3(minLocation.X, candidates.Min(x => x.Y), minLocation.Y);

                        //add this house
                        var house = new NBHmHouse()
                        {
                            HouseNum = short.Parse(split[0].Substring(1)),
                            Position = minLocation3
                        };
                        Houses[house.HouseNum] = house;
                    }
                }
            }
        }

        public void Save(Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteCString("NBHm", 4);
                io.WriteInt32(CURRENT_VERSION);
                io.WriteInt32(Houses.Count);
                foreach (var house in Houses)
                {
                    io.WriteInt16(house.Key);
                    io.WriteFloat(house.Value.Position.X);
                    io.WriteFloat(house.Value.Position.Y);
                    io.WriteFloat(house.Value.Position.Z);
                }
                io.WriteByte(0); //has model. right now there is no model.
            }
        }

        public void Load(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                var magic = io.ReadCString(4);
                if (magic != "NBHm") throw new Exception("Not a valid neighbourhood model!");
                Version = io.ReadInt32();
                var houseC = io.ReadInt32();
                for (int i = 0; i < houseC; i++)
                {
                    var house = new NBHmHouse()
                    {
                        HouseNum = io.ReadInt16(),
                        Position = new Vector3()
                        {
                            X = io.ReadFloat(),
                            Y = io.ReadFloat(),
                            Z = io.ReadFloat()
                        }
                    };
                    Houses[house.HouseNum] = house;
                }
                io.ReadByte(); //has model. right now there is no model.
            }
        }
    }

    public class NBHmHouse
    {
        public short HouseNum;
        public Vector3 Position;
    }
}
