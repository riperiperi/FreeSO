using FSO.Vitaboy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
