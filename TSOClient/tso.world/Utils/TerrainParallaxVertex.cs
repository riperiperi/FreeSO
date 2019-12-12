using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FSO.LotView.Utils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainParallaxVertex : IVertexType
    {
        public Vector3 Position;
        public Vector4 Color;
        public Vector3 GrassInfo;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector3 Bitangent;

        private static Matrix TangentRot = Matrix.CreateRotationZ((float)(Math.PI / -2f));

        private static Vector3 TangentialRot(Vector3 src, Matrix rot)
        {
            var v2 = Vector3.Transform(Vector3.Up, rot);

            src = v2 - (Vector3.Dot(src, v2) * src);
            src.Normalize();
            //var TangentRot = Matrix.CreateRotationZ((float)(Math.PI / -2f)) * Matrix.CreateRotationY((float)(Math.PI / 4f));
            return src;//Vector3.Transform(src, TangentRot);
        }

        private static Vector3 TangentialRot(Vector3 src)
        {
            return TangentialRot(src, TangentRot);
        }

        public TerrainParallaxVertex(Vector3 position, Vector4 color, Vector2 grassPos, Single live, Vector3 normal, Matrix mat, bool flip)
            : this(position, color, grassPos, live, normal, TangentialRot(normal, mat), flip)
        {
        }


        public TerrainParallaxVertex(Vector3 position, Vector4 color, Vector2 grassPos, Single live, Vector3 normal)
            : this(position, color, grassPos, live, normal, TangentialRot(normal), false)
        {
        }

        public TerrainParallaxVertex(Vector3 position, Vector4 color, Vector2 grassPos, Single live, Vector3 normal, Vector3 tangent, bool flip)
        {
            this.Normal = normal;
            this.Position = position;
            this.Color = color;
            this.GrassInfo = new Vector3(live, grassPos.X, grassPos.Y);
            this.Tangent = tangent;
            if (flip) this.Bitangent = Vector3.Cross(normal, tangent);//, normal);
            else this.Bitangent = Vector3.Cross(tangent, normal);//, normal);
            this.Bitangent.Normalize();
        }

        public static int SizeInBytes = sizeof(float) * 19;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
             new VertexElement(sizeof(float) * 7, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
             new VertexElement(sizeof(float) * 10, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
             new VertexElement(sizeof(float) * 13, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 2),
             new VertexElement(sizeof(float) * 16, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 3)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
