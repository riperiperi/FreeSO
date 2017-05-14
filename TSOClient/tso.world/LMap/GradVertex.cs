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
using System.Runtime.Serialization;

namespace FSO.LotView.LMap
{
    /// <summary>
    /// Represents one Vertex that makes up a gradient vertex colour.
    /// Supports multiple gradient types.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GradVertex : IVertexType
    {
        public Vector2 Coord;

        public Color Color1;
        public Color Color2;

        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public Vector4 Params; // x: mode. 0/1/2/3 solid/linear/cone/circle. y: col1 offset, for cone and circle
        public Vector4 EllipseDat;

        public static int SizeInBytes = 64;

        public static readonly VertexDeclaration VertexElements = new VertexDeclaration( 
        
            new VertexElement(0, VertexElementFormat.Vector2,
                VertexElementUsage.Position, 0),
            new VertexElement(8, VertexElementFormat.Color,
                VertexElementUsage.Color, 0),
            new VertexElement(12, VertexElementFormat.Color,
                VertexElementUsage.Color, 1),
            new VertexElement(16, VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(24, VertexElementFormat.Vector2, 
                VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(32, VertexElementFormat.Vector4, 
                VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(48, VertexElementFormat.Vector4,
                VertexElementUsage.TextureCoordinate, 3)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }

        public static GradVertex SolidVert(Vector2 position, Color color, EllipseDesc ellipse)
        {
            return new GradVertex()
            {
                Color1 = color,
                Coord = position,
                Params = new Vector4(0, 0, ellipse.pos.X, ellipse.pos.Y),
                EllipseDat = ellipse.dimensions
            };
        }

        public static GradVertex ConeVert(Vector2 position, Vector2 coneCtr, Vector2 coneOuter, Color color1, Color color2, float penumbra, EllipseDesc ellipse)
        {
            return new GradVertex()
            {
                Color1 = color1,
                Color2 = color2,
                Coord = position,
                StartPosition = coneCtr,
                EndPosition = coneOuter,
                Params = new Vector4(2, penumbra, ellipse.pos.X, ellipse.pos.Y),
                EllipseDat = ellipse.dimensions
            };
        }
    }

    public struct EllipseDesc
    {
        public Vector2 pos;
        public bool linear;
        public Vector4 dimensions
        {
            get
            {
                return _dimensions;
            }
            set
            {
                _dimensions = value;
            }
        }

        public EllipseDesc Prepare(float mod)
        {
            if (linear) this._dimensions.X = mod;
            return this;
        }

        private Vector4 _dimensions;
    }
}
