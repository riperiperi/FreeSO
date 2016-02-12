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
    public struct TerrainVertex : IVertexType
    {
        public Vector3 Position;
        public Vector4 Color;
        public Vector3 GrassInfo;

        public TerrainVertex(Vector3 position, Vector4 color, Vector2 grassPos, Single live)
        {
            this.Position = position;
            this.Color = color;
            this.GrassInfo = new Vector3(live, grassPos.X, grassPos.Y);
        }

        public static int SizeInBytes = sizeof(float) * 11;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
             new VertexElement(sizeof(float) * 7, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
