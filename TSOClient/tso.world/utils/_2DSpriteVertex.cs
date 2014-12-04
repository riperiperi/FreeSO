/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world.utils
{
    /// <summary>
    /// Represents a vertex making up a 2D sprite in the game.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct _2DSpriteVertex
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 WorldPosition;
        public Single ObjectID;

        /// <summary>
        /// Creates a new _2DSpriteVertex instance.
        /// </summary>
        /// <param name="position">Position of vertex.</param>
        /// <param name="textureCoords">Texture coordinates of vertex.</param>
        /// <param name="worldPosition">Vertex' position in world.</param>
        /// <param name="objID">ID of object/sprite that this vertex belongs to.</param>
        public _2DSpriteVertex(Vector3 position, Vector2 textureCoords, Vector3 worldPosition, Single objID)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoords;
            this.WorldPosition = worldPosition;
            this.ObjectID = objID;
        }

        public static int SizeInBytes = sizeof(float) * 9;

        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
             new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( 0, sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1 ),
             new VertexElement( 0, sizeof(float) * 8, VertexElementFormat.Single, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 2 )
        };

    }
}
