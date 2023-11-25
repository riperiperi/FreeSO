using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Vitaboy.Model
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ShadowVertex : IVertexType
    {
        public Vector3 Position;
        public Single Bone;

        public ShadowVertex(Vector3 position, Single bone)
        {
            this.Position = position;
            this.Bone = bone;
        }

        public static int SizeInBytes = sizeof(float) * 4;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
