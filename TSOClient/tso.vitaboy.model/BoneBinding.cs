namespace FSO.Vitaboy
{
    /// <summary>
    /// A bone binding associates vertices and blende vertices with bones.
    /// </summary>
    public class BoneBinding
    {
        public string BoneName;
        public int BoneIndex;
        public int FirstRealVertex;
        public int RealVertexCount;
        public int FirstBlendVertex;
        public int BlendVertexCount;
    }
}
