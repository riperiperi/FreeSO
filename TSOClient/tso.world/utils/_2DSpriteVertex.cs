using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world.utils
{
    [StructLayout(LayoutKind.Sequential)]
    public struct _2DSpriteVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 WorldPosition;

        public _2DSpriteVertex(Vector3 position, Vector2 textureCoords, Vector3 worldPosition){
            this.Position = position;
            this.TextureCoordinate = textureCoords;
            this.WorldPosition = worldPosition;
        }

        public static int SizeInBytes = sizeof(float) * 8;

        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
             new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( 0, sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1 )
        };

    }
}
