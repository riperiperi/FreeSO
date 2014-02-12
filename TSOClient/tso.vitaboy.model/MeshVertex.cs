using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tso.vitaboy
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex
    {
        public Vector3 Position;
        /** UV Mapping **/
        public Vector2 UV;
        public Vector3 Normal;

        public static int SizeInBytes = sizeof(float) * 8;

        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
             new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( 0, sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0 )
        };
    }
}
