using System.Numerics;

namespace CASOutfitImporter.Formats
{
    // Cross-platform replica of FSO.Vitaboy.Mesh's data model (no MonoGame dep).
    internal sealed class MeshModel
    {
        public string SkinName;     // e.g. "xskin-b076fafit_roundedflares-PELVIS-BODY"
        public string TextureName;  // typically "x" in TS1 .skn files

        public string[] BoneNames;          // length = boneCount
        public int[] IndexBuffer;           // 3*faceCount
        public int FaceCount;

        public BoneBinding[] BoneBindings;

        public Vector2[] UVs;               // per real-vertex (UV.X, UV.Y)
        public Vector3[] Positions;         // per real-vertex
        public Vector3[] Normals;           // per real-vertex

        public BlendDatum[] BlendData;      // length = blendVertexCount
        public Vector3[] BlendPositions;    // length = blendVertexCount
        public Vector3[] BlendNormals;      // length = blendVertexCount

        // The bone of the binding (taken from the .skn filename "xskin-name-BONE-GROUP")
        // is stored on the resulting Binding for the engine to attach the mesh.
        public string PrimaryBone;
    }

    internal struct BoneBinding
    {
        public int BoneIndex;
        public int FirstRealVertex;
        public int RealVertexCount;
        public int FirstBlendVertex;
        public int BlendVertexCount;
    }

    internal struct BlendDatum
    {
        public float Weight;
        public int OtherVertex;
    }
}