/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
rhy3756547. All Rights Reserved.

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
    public struct VitaboyVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public Vector3 BvPosition; //blend vert
        public Vector3 Parameters;

        public VitaboyVertex(Vector3 position, Vector2 textureCoords, Vector3 bvPosition, Vector3 parameters)
        {
            this.Position = position;
            this.TextureCoordinate = textureCoords;
            this.BvPosition = bvPosition;
            this.Parameters = parameters;
        }

        public static int SizeInBytes = sizeof(float) * 11;

        public static VertexDeclaration VertexElements = new VertexDeclaration
        (
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
             new VertexElement(sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
             new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 2)
        );

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexElements; }
        }
    }
}
