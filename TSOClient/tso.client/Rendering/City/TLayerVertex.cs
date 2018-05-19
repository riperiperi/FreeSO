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
    public struct TLayerVertex : IVertexType
    {
        public Vector3 Position;
        /** UV Mapping **/
        public Vector2 TextureCoord; //in shader these are xy and zw in TC0 respectively.
        public Vector2 MaskTextureCoord;
        public Vector3 Normal;
        public float Transparency; //0 is opaque.

        public static int SizeInBytes = sizeof(float) * 11;

        public static readonly VertexDeclaration VertexElements = new VertexDeclaration(

            new VertexElement(0, VertexElementFormat.Vector3,
                VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector4,
                VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * (3 + 4), VertexElementFormat.Vector4,
                VertexElementUsage.TextureCoordinate, 1)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
