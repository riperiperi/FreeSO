/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.LotView.Utils
{
    /// <summary>
    /// Represents a vertex making up a 2D sprite in the game.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct _2DSpriteVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 WorldPosition;
        public Single ObjectID;
        public Vector2 Room;

        /// <summary>
        /// Creates a new _2DSpriteVertex instance.
        /// </summary>
        /// <param name="position">Position of vertex.</param>
        /// <param name="textureCoords">Texture coordinates of vertex.</param>
        /// <param name="worldPosition">Vertex' position in world.</param>
        /// <param name="objID">ID of object/sprite that this vertex belongs to.</param>
        public _2DSpriteVertex(Vector3 position, Vector2 textureCoords, Vector3 worldPosition, Single objID, ushort room)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoords;
            this.WorldPosition = worldPosition;
            this.ObjectID = objID/65535.0f;
            this.Room = new Vector2((room % 256) / 256f, (room / 256) / 256f);
        }

        public static int SizeInBytes = sizeof(float) * 11;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement( 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
             new VertexElement( sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1 ),
             new VertexElement( sizeof(float) * 8, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2 ),
             new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
