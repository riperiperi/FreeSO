using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DGRP3DVert : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 Normal;
        public static readonly VertexDeclaration VertexDeclaration;
        public DGRP3DVert(Vector3 position, Vector3 normal, Vector2 textureCoordinate)
        {
            this.Position = position;
            this.Normal = normal;
            this.TextureCoordinate = textureCoordinate;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }
        public override int GetHashCode()
        {
            // TODO: FIc gethashcode
            return 0;
        }

        public override string ToString()
        {
            return "{{Position:" + this.Position + " Normal:" + this.Normal + " TextureCoordinate:" + this.TextureCoordinate + "}}";
        }

        public static bool operator ==(DGRP3DVert left, DGRP3DVert right)
        {
            return (((left.Position == right.Position) && (left.Normal == right.Normal)) && (left.TextureCoordinate == right.TextureCoordinate));
        }

        public static bool operator !=(DGRP3DVert left, DGRP3DVert right)
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
            return (this == ((DGRP3DVert)obj));
        }

        static DGRP3DVert()
        {
            VertexElement[] elements = new VertexElement[] { new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0), new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0), new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1) };
            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

        public static void GenerateNormals(bool invert, List<DGRP3DVert> verts, IList<int> indices)
        {
            for (int i = 0; i < indices.Count; i += 3)
            {
                var v1 = verts[indices[i + 1]].Position - verts[indices[i]].Position;
                var v2 = verts[indices[i + 2]].Position - verts[indices[i + 1]].Position;
                var cross = invert ? Vector3.Cross(v2, v1) : Vector3.Cross(v1, v2);
                for (int j = 0; j < 3; j++)
                {
                    var id = indices[i + j];
                    var v = verts[id];
                    v.Normal += cross;
                    verts[id] = v;
                }
            }

            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                v.Normal.Normalize();
                verts[i] = v;
            }
        }

        public static List<int> StripToTri(List<int> ind)
        {
            var result = new List<int>();
            for (int i = 0; i < ind.Count; i += 2)
            {
                if (i>0)
                {
                    result.Add(ind[i - 2]);
                    result.Add(ind[i - 1]);
                    result.Add(ind[i]);

                    result.Add(ind[i - 1]);
                    result.Add(ind[i + 1]);
                    result.Add(ind[i]);
                }
            }
            return result;
        }
    }
}
