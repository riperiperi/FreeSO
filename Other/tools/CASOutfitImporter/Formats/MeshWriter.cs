using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CASOutfitImporter.Formats
{
    // Mirror of FSO.Vitaboy.Mesh.Write(io, bmf=false) with ByteOrder=BIG_ENDIAN.
    // Produces a .mesh file the FSO TSO mesh codec can load.
    internal static class MeshWriter
    {
        public static byte[] Write(MeshModel m)
        {
            using var ms = new MemoryStream();
            using (var w = new BeWriter(ms))
            {
                // Header: version=2 (TSO mesh, big-endian path).
                w.I32(2);

                // Bone names: deduplicated list of bone names from BoneBindings,
                // matching Mesh.Write which builds a HashSet<string>.ToList().
                var boneList = new List<string>();
                var boneSet = new HashSet<string>();
                foreach (var bb in m.BoneBindings)
                {
                    var name = m.BoneNames[bb.BoneIndex];
                    if (boneSet.Add(name)) boneList.Add(name);
                }
                w.I32(boneList.Count);
                foreach (var name in boneList) w.PascalString(name);

                // Face count + index buffer.
                w.I32(m.FaceCount);
                foreach (var idx in m.IndexBuffer) w.I32(idx);

                // Bone bindings (BoneIndex re-mapped to dedup list).
                w.I32(m.BoneBindings.Length);
                foreach (var bb in m.BoneBindings)
                {
                    var name = m.BoneNames[bb.BoneIndex];
                    int newIdx = boneList.IndexOf(name);
                    w.I32(newIdx);
                    w.I32(bb.FirstRealVertex);
                    w.I32(bb.RealVertexCount);
                    w.I32(bb.FirstBlendVertex);
                    w.I32(bb.BlendVertexCount);
                }

                // UVs
                w.I32(m.UVs.Length);
                foreach (var uv in m.UVs)
                {
                    w.F32(uv.X);
                    w.F32(uv.Y);
                }

                // Blend data (binary path: weight*0x8000 first, then otherVertex).
                w.I32(m.BlendData.Length);
                foreach (var bd in m.BlendData)
                {
                    w.I32((int)(bd.Weight * 0x8000));
                    w.I32(bd.OtherVertex);
                }

                // realVertexCount2
                w.I32(m.Positions.Length);

                // Position+normal interleaved (X negated on write to round-trip).
                for (int i = 0; i < m.Positions.Length; i++)
                {
                    var p = m.Positions[i];
                    var n = m.Normals[i];
                    w.F32(-p.X); w.F32(p.Y); w.F32(p.Z);
                    w.F32(-n.X); w.F32(n.Y); w.F32(n.Z);
                }

                // Blend positions+normals (same negate convention).
                for (int i = 0; i < m.BlendPositions.Length; i++)
                {
                    var bp = m.BlendPositions[i];
                    var bn = m.BlendNormals[i];
                    w.F32(-bp.X); w.F32(bp.Y); w.F32(bp.Z);
                    w.F32(-bn.X); w.F32(bn.Y); w.F32(bn.Z);
                }
            }
            return ms.ToArray();
        }
    }
}