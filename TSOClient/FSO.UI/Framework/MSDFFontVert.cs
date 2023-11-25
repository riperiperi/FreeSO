using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace FSO.UI.Framework
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MSDFFontVert : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector2 Derivative;
        public static readonly VertexDeclaration VertexDeclaration;
        public MSDFFontVert(Vector3 position, Vector2 textureCoordinate, Vector2 derivative)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoordinate;
            this.Derivative = derivative;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public static bool operator ==(MSDFFontVert left, MSDFFontVert right)
        {
            return (((left.Position == right.Position) && (left.Derivative == right.Derivative)) && (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(MSDFFontVert left, MSDFFontVert right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != base.GetType())
            {
                return false;
            }
            return (this == ((MSDFFontVert)obj));
        }

        static MSDFFontVert()
        {
            VertexElement[] elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
            };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }
    }
}
