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
using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.Rendering.City
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CityVertex
    {
        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate;
        public Vector4 TextureWeight1;
        public Vector4 TextureWeight2;

        public static int SizeInBytes = (sizeof(float) * (3 + 2 + 4 + 4)) + 4;
        public static VertexElement[] VertexElements = new VertexElement[]
        {
             new VertexElement( 0, 0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0 ),
             new VertexElement( 0, sizeof(float) * 3, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0 ),
             new VertexElement( 0, (sizeof(float) * 3) + 4, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0 ),
             new VertexElement( 0, (sizeof(float) * (3 + 2)) + 4, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1 ),
             new VertexElement( 0, (sizeof(float) * (3 + 2 + 4)) + 4, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 2 )
        };

        public CityVertex(Vector3 position, Vector2 textureCoords, Color color, TerrainType terrain)
        {
            this.Position = position;
            this.Color = color;
            this.TextureCoordinate = textureCoords;
            this.TextureWeight1 = new Vector4(terrain == TerrainType.Grass ? 1 : 0,
                                              terrain == TerrainType.Snow ? 1 : 0,
                                              terrain == TerrainType.Sand ? 1 : 0,
                                              terrain == TerrainType.Rock ? 1 : 0);

            this.TextureWeight2 = new Vector4(terrain == TerrainType.Water ? 1 : 0, 0, 0, 0);
        }
    }
}
