using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Vitaboy
{
    /// <summary>
    /// Vertex that makes up a mesh.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex:IVertexType
    {
        public Vector3 Position;
        /** UV Mapping **/
        public Vector2 UV;
        public Vector3 Normal;

        public static int SizeInBytes = sizeof(float) * 8;

        public readonly static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement( 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
             new VertexElement( sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0 )
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
