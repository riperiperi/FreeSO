namespace FSO.LotView.LMap
{
    internal readonly struct GradMesh
    {
        public readonly GradVertex[] Vertices;
        public readonly int VertexCount;

        public readonly int[] Indices;
        public readonly int IndexCount;

        public GradMesh(GradVertex[] vertices, int vertexCount, int[] indices, int indexCount)
        {
            Vertices = vertices;
            VertexCount = vertexCount;

            Indices = indices;
            IndexCount = indexCount;
        }
    }
}
