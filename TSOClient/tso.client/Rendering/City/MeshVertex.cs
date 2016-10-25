/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// Represents a MeshVertex that makes up a face.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshVertex : IVertexType
    {
        public Vector3 Coord;
        /** UV Mapping **/
        public Vector2 TextureCoord;
        public Vector2 Texture2Coord;
        public Vector2 Texture3Coord;
        public Vector2 UVBCoord;
        public Vector2 RoadCoord;
        public Vector2 RoadCCoord;
        public Vector3 Normal;

        public static int SizeInBytes = sizeof(float) * 18;

        public static readonly VertexDeclaration VertexElements = new VertexDeclaration( 
        
            new VertexElement(0, VertexElementFormat.Vector3,
                VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float)*3, VertexElementFormat.Vector2,
                VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float)*(3+2), VertexElementFormat.Vector2,
                VertexElementUsage.TextureCoordinate, 1),
            new VertexElement( sizeof(float)*(3+4), VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 2),
            new VertexElement( sizeof(float)*(3+6), VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 3),
            new VertexElement( sizeof(float)*(3+8), VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 4),
            new VertexElement( sizeof(float)*(3+10), VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 5),
            new VertexElement( sizeof(float)*(3+12), VertexElementFormat.Vector3,
                VertexElementUsage.Normal, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
