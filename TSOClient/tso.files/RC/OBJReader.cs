using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FSO.Files.RC
{
    /// <summary>
    /// Simple reader for .OBJ files. Only made to import from blender.
    /// </summary>
    public class OBJ
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector2> TextureCoords = new List<Vector2>();
        public List<Vector3> Normals = new List<Vector3>();
        public Dictionary<string, List<int[]>> FacesByObjgroup = new Dictionary<string, List<int[]>>();

        public OBJ(Stream obj)
        {
            using (var reader = new StreamReader(obj))
                Read(reader);
        }

        public void Read(StreamReader read)
        {
            string objGroup = "_default";
            string line = "";
            List<int[]> indices = new List<int[]>();
            FacesByObjgroup[objGroup] = indices;
            while ((line = read.ReadLine()) != null)
            {
                line = line.TrimStart();
                var comInd = line.IndexOf("#");
                if (comInd != -1) line = line.Substring(0, comInd);
                var split = line.Split(' ');
                if (split.Length == 0) continue;
                switch (split[0])
                {
                    case "o": //set object group
                        objGroup = split[1];
                        if (!FacesByObjgroup.TryGetValue(objGroup, out indices))
                        {
                            indices = new List<int[]>();
                            FacesByObjgroup[objGroup] = indices;
                        }
                        break;
                    case "v":
                        Vertices.Add(new Vector3(float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture), float.Parse(split[3], CultureInfo.InvariantCulture)));
                        break;
                    case "vt":
                        TextureCoords.Add(new Vector2(float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture)));
                        break;
                    case "vn":
                        Normals.Add(new Vector3(float.Parse(split[1], CultureInfo.InvariantCulture), float.Parse(split[2], CultureInfo.InvariantCulture), float.Parse(split[3], CultureInfo.InvariantCulture)));
                        break;
                    case "f":
                        for (int i = 0; i < 3; i++)
                        {
                            var split2 = split[i + 1].Split('/');
                            if (split2.Length == 2)
                                indices.Add(new int[] { int.Parse(split2[0]), int.Parse(split2[1]) });
                            else if (split2.Length == 3)
                                indices.Add(new int[] { int.Parse(split2[0]), int.Parse(split2[1]), int.Parse(split2[2]) });
                        }
                        break;
                }
            }
        }
    }
}