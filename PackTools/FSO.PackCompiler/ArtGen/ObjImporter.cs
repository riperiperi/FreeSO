using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FSO.PackCompiler.ArtGen
{
    /// <summary>
    /// Loads flat-shaded OBJ + MTL meshes into ArtGen's face list. Kenney/Quaternius CC0
    /// kits ship solid Kd material colours and no texture maps — Kd * 255 is the per-face
    /// colour (see CATALOG-SOURCING.md correction).
    /// </summary>
    public static class ObjImporter
    {
        public class Params
        {
            public string MeshPath;
            public double Height = 1.0;
            public bool Symmetric;
        }

        public static Mesh Load(Params p)
        {
            if (string.IsNullOrEmpty(p.MeshPath))
                throw new ArgumentException("mesh path is required");
            if (!File.Exists(p.MeshPath))
                throw new FileNotFoundException("mesh file not found", p.MeshPath);
            if (p.Height <= 0)
                throw new ArgumentException("height must be > 0");

            var materialColors = new Dictionary<string, (byte, byte, byte)>();
            var dir = Path.GetDirectoryName(p.MeshPath) ?? ".";
            var verts = new List<Vec3>();
            var facesByMaterial = new List<(List<int> indices, string material)>();
            var currentMaterial = "default";
            var currentFace = new List<int>();

            foreach (var rawLine in File.ReadAllLines(p.MeshPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "mtllib":
                        LoadMtl(Path.Combine(dir, string.Join(" ", parts.Skip(1))), materialColors);
                        break;
                    case "v":
                        verts.Add(new Vec3(
                            ParseDouble(parts, 1),
                            ParseDouble(parts, 2),
                            ParseDouble(parts, 3)));
                        break;
                    case "usemtl":
                        if (currentFace.Count >= 3)
                            facesByMaterial.Add((currentFace, currentMaterial));
                        currentFace = new List<int>();
                        currentMaterial = string.Join(" ", parts.Skip(1));
                        if (!materialColors.ContainsKey(currentMaterial))
                            materialColors[currentMaterial] = DefaultColor;
                        break;
                    case "f":
                        if (currentFace.Count >= 3)
                            facesByMaterial.Add((currentFace, currentMaterial));
                        currentFace = parts.Skip(1).Select(ParseFaceIndex).ToList();
                        break;
                }
            }
            if (currentFace.Count >= 3)
                facesByMaterial.Add((currentFace, currentMaterial));

            if (verts.Count == 0 || facesByMaterial.Count == 0)
                throw new InvalidDataException("OBJ contains no usable geometry");

            Normalize(verts, p.Height);

            var mesh = new Mesh();
            foreach (var (indices, material) in facesByMaterial)
            {
                var color = materialColors.TryGetValue(material, out var c) ? c : DefaultColor;
                var faceVerts = indices.Select(i => verts[i - 1]).ToArray();
                EmitFace(mesh, faceVerts, color);
            }

            if (mesh.Faces.Count == 0)
                throw new InvalidDataException("OBJ produced no faces after triangulation");

            return mesh;
        }

        static readonly (byte, byte, byte) DefaultColor = (180, 180, 180);

        static void LoadMtl(string mtlPath, Dictionary<string, (byte, byte, byte)> into)
        {
            if (!File.Exists(mtlPath)) return;

            string current = null;
            foreach (var rawLine in File.ReadAllLines(mtlPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0] == "newmtl")
                {
                    current = string.Join(" ", parts.Skip(1));
                    if (!into.ContainsKey(current))
                        into[current] = DefaultColor;
                }
                else if (parts[0] == "Kd" && current != null && parts.Length >= 4)
                {
                    into[current] = (
                        ToByte(parts[1]),
                        ToByte(parts[2]),
                        ToByte(parts[3]));
                }
            }
        }

        static byte ToByte(string s)
        {
            var v = double.Parse(s, CultureInfo.InvariantCulture);
            if (v <= 1.0) v *= 255.0;
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(v)));
        }

        static double ParseDouble(string[] parts, int index)
        {
            if (index >= parts.Length) return 0;
            return double.Parse(parts[index], CultureInfo.InvariantCulture);
        }

        static int ParseFaceIndex(string token)
        {
            var slash = token.IndexOf('/');
            var num = slash >= 0 ? token.Substring(0, slash) : token;
            return int.Parse(num, CultureInfo.InvariantCulture);
        }

        static void Normalize(List<Vec3> verts, double targetHeight)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            foreach (var v in verts)
            {
                if (v.X < minX) minX = v.X; if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y; if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z; if (v.Z > maxZ) maxZ = v.Z;
            }

            var sizeY = maxY - minY;
            if (sizeY < 1e-9) throw new InvalidDataException("mesh has zero height");
            var scale = targetHeight / sizeY;
            var cx = (minX + maxX) / 2;
            var cz = (minZ + maxZ) / 2;

            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                verts[i] = new Vec3(
                    (v.X - cx) * scale,
                    (v.Y - minY) * scale,
                    (v.Z - cz) * scale);
            }
        }

        static void EmitFace(Mesh mesh, Vec3[] verts, (byte, byte, byte) color)
        {
            if (verts.Length == 3)
            {
                mesh.AddFace(verts, color);
                return;
            }
            if (verts.Length == 4)
            {
                mesh.AddQuad(verts[0], verts[1], verts[2], verts[3], color);
                return;
            }
            // n-gon: fan triangulate from v0, pair consecutive triangles into quads where possible.
            var tris = new List<Vec3[]>();
            for (int i = 1; i < verts.Length - 1; i++)
                tris.Add(new[] { verts[0], verts[i], verts[i + 1] });
            for (int i = 0; i < tris.Count; i += 2)
            {
                if (i + 1 < tris.Count)
                {
                    var a = tris[i]; var b = tris[i + 1];
                    mesh.AddQuad(a[0], a[1], b[2], a[2], color);
                }
                else
                    mesh.AddFace(tris[i], color);
            }
        }
    }
}
