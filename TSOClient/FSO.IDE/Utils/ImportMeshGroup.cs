using FSO.Vitaboy;
using System;

namespace FSO.IDE.Utils
{
    public class ImportMeshGroup
    {
        public string Name;
        public Mesh Mesh;
        public ArraySegment<byte>? TextureData;
    }

    public class ImportMaterialGroup
    {
        public string Name;
        public ArraySegment<byte>? TextureData;
    }
}
