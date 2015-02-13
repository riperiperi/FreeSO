/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

//DON'T FUCKING CHANGE THIS - some fucking rocket scientist decided to redefine System.IO.FileMode with the EXACT
//SAME NAMESPACE inside the Monogame lib...
extern alias MonoGame;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using MG = MonoGame::Microsoft.Xna.Framework;
using MGfx = MonoGame::Microsoft.Xna.Framework.Graphics;

namespace TSO_CityServer.Terrain
{
	/// <summary>
	/// Represents a MeshVertex that makes up a face.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct MeshVertex : MGfx.IVertexType
	{
		public MG.Vector3 Coord;
		/** UV Mapping **/
		public MG.Vector2 TextureCoord;
		public MG.Vector2 Texture2Coord;
		public MG.Vector2 Texture3Coord;
		public MG.Vector2 UVBCoord;
		public MG.Vector2 RoadCoord;
		public MG.Vector2 RoadCCoord;

		public static int SizeInBytes = sizeof(float) * 15;

		public static readonly MGfx.VertexDeclaration VertexElements = new MGfx.VertexDeclaration(

			new MGfx.VertexElement(0, MGfx.VertexElementFormat.Vector3,
				MGfx.VertexElementUsage.Position, 0),
			new MGfx.VertexElement(sizeof(float) * 3, MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 0),
			new MGfx.VertexElement(sizeof(float) * (3 + 2), MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 1),
			new MGfx.VertexElement(sizeof(float) * (3 + 4), MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 2),
			new MGfx.VertexElement(sizeof(float) * (3 + 6), MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 3),
			new MGfx.VertexElement(sizeof(float) * (3 + 8), MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 4),
			new MGfx.VertexElement(sizeof(float) * (3 + 10), MGfx.VertexElementFormat.Vector2,
				MGfx.VertexElementUsage.TextureCoordinate, 5)
		);

		MGfx.VertexDeclaration MGfx.IVertexType.VertexDeclaration
		{
			get { return VertexElements; }
		}
	}
}