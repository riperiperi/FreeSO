using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace CASOutfitImporter.Formats
{
    // Parser for TS1 xskin-*.skn (text-form mesh, identical layout to .bmf binary).
    // Mirrors FSO.Vitaboy.Mesh.Read with bmf=true via BCFReadString:
    //   line: skinName
    //   line: textureName  (typically "x")
    //   "<n>"  bone count
    //   n × line: bone name
    //   "<m>"  face count
    //   m × line: 3 ints
    //   "<b>"  bone-binding count
    //   b × line: 5 ints  (boneIdx firstReal realCount firstBlend blendCount)
    //   "<v>"  vertex count
    //   v × line: 2 floats (u v)
    //   "<bv>" blend-vertex count
    //   bv × line: 2 ints (weight otherVertex)   -- BCFReadString always writes weight first
    //   "<v>"  realVertexCount2  (== v)
    //   v × triplet of (xyz pos, xyz normal)  -- 6 floats per vertex
    //   bv × triplet of (xyz blendPos, xyz blendNormal)  -- 6 floats per blendvert
    //
    // Position X is read negated: vertex.x = -fileX.
    internal sealed class SknReader
    {
        private readonly string[] _tokens;
        private int _ti;

        private SknReader(string text)
        {
            _tokens = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(line => line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();
            _ti = 0;
        }

        public static MeshModel ReadFile(string path)
        {
            var raw = File.ReadAllText(path);
            var r = new SknReader(raw);
            return r.Parse();
        }

        private string Next()
        {
            if (_ti >= _tokens.Length) throw new EndOfStreamException("unexpected EOF in .skn");
            return _tokens[_ti++];
        }
        private int NextInt() => int.Parse(Next(), CultureInfo.InvariantCulture);
        private float NextFloat() => float.Parse(Next(), CultureInfo.InvariantCulture);

        private MeshModel Parse()
        {
            var m = new MeshModel();
            m.SkinName = Next();
            m.TextureName = Next();

            int boneCount = NextInt();
            m.BoneNames = new string[boneCount];
            for (int i = 0; i < boneCount; i++) m.BoneNames[i] = Next();

            int faceCount = NextInt();
            m.FaceCount = faceCount;
            m.IndexBuffer = new int[faceCount * 3];
            for (int i = 0; i < faceCount; i++)
            {
                m.IndexBuffer[i * 3 + 0] = NextInt();
                m.IndexBuffer[i * 3 + 1] = NextInt();
                m.IndexBuffer[i * 3 + 2] = NextInt();
            }

            int bindingCount = NextInt();
            m.BoneBindings = new BoneBinding[bindingCount];
            for (int i = 0; i < bindingCount; i++)
            {
                m.BoneBindings[i] = new BoneBinding
                {
                    BoneIndex = NextInt(),
                    FirstRealVertex = NextInt(),
                    RealVertexCount = NextInt(),
                    FirstBlendVertex = NextInt(),
                    BlendVertexCount = NextInt()
                };
            }

            int vertexCount = NextInt();
            m.UVs = new Vector2[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float u = NextFloat();
                float v = NextFloat();
                m.UVs[i] = new Vector2(u, v);
            }

            int blendCount = NextInt();
            m.BlendData = new BlendDatum[blendCount];
            for (int i = 0; i < blendCount; i++)
            {
                // BCF text path (Mesh.Read with `binary=false` branch in Write):
                // otherVertex first, then weight*0x8000. The binary BMF/.mesh path
                // is the opposite order — see Mesh.Write/Read in tso.vitaboy.model.
                int other = NextInt();
                int wRaw = NextInt();
                m.BlendData[i] = new BlendDatum
                {
                    OtherVertex = other,
                    Weight = wRaw / 32768f
                };
            }

            // realVertexCount2 is the TOTAL of real + blend verts that follow; the
            // FSO Mesh.Read implementation simply reads it and ignores it, iterating
            // realVertexCount real positions then blendVertexCount blend positions.
            int vertexCount2 = NextInt();
            if (vertexCount2 != vertexCount && vertexCount2 != vertexCount + blendCount)
                throw new InvalidDataException(
                    $".skn vertex counts unexpected: real={vertexCount} blend={blendCount} total={vertexCount2}");

            m.Positions = new Vector3[vertexCount];
            m.Normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float px = -NextFloat();
                float py = NextFloat();
                float pz = NextFloat();
                m.Positions[i] = new Vector3(px, py, pz);

                float nx = -NextFloat();
                float ny = NextFloat();
                float nz = NextFloat();
                var n = new Vector3(nx, ny, nz);
                if (n == Vector3.Zero) n = new Vector3(0, 1, 0);
                m.Normals[i] = n;
            }

            m.BlendPositions = new Vector3[blendCount];
            m.BlendNormals = new Vector3[blendCount];
            for (int i = 0; i < blendCount; i++)
            {
                float px = -NextFloat();
                float py = NextFloat();
                float pz = NextFloat();
                m.BlendPositions[i] = new Vector3(px, py, pz);

                float nx = -NextFloat();
                float ny = NextFloat();
                float nz = NextFloat();
                m.BlendNormals[i] = new Vector3(nx, ny, nz);
            }

            // PrimaryBone from filename pattern "xskin-<name>-<BONE>-<GROUP>" if available.
            // The .skn line 1 (skinName) carries the same string; parse from there.
            m.PrimaryBone = ExtractPrimaryBone(m.SkinName);

            return m;
        }

        private static string ExtractPrimaryBone(string skinName)
        {
            // "xskin-b076fafit_roundedflares-PELVIS-BODY" -> "PELVIS"
            var parts = skinName.Split('-');
            if (parts.Length >= 4) return parts[parts.Length - 2];
            return null;
        }
    }
}