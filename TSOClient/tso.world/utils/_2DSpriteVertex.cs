/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
    [StructLayout(LayoutKind.Sequential)]
    public struct _2DSpriteVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 WorldPosition;
        public Single ObjectID;

        public _2DSpriteVertex(Vector3 position, Vector2 textureCoords, Vector3 worldPosition, Single objID){
            this.Position = position;
            this.TextureCoordinate = textureCoords;
            this.WorldPosition = worldPosition;
            this.ObjectID = objID/65535.0f;
        }

        public static int SizeInBytes = sizeof(float) * 9;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement( 0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
             new VertexElement( sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1 ),
             new VertexElement( sizeof(float) * 8, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2 )
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
