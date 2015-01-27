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
using Microsoft.Xna.Framework.Graphics;
using TSO.Content;
using Microsoft.Xna.Framework;
using System.IO;

namespace tso.world.components
{
    public class TerrainComponent : WorldComponent
    {
        private Texture2D Texture;
        private Rectangle Size;

        private int GeomLength;
        private float[] GrassState; //0 = green, 1 = brown. to start with, should be randomly distriuted in range 0-0.5.
        private int NumPrimitives;
        private int GrassPrimitives;
        private IndexBuffer IndexBuffer;
        private VertexBuffer VertexBuffer;
        private VertexBuffer GrassVertexBuffer;

        private BasicEffect Effect;
        public bool DrawGrid = false;

        public TerrainComponent(Rectangle size){
            this.Size = size;
            GenerateGrassStates();
        }

        public override float PreferredDrawOrder
        {
            get { return 0.0f; }
        }

        /// <summary>
        /// Setup component to run on graphics device
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Initialize(GraphicsDevice device, WorldState world)
        {
            base.Initialize(device, world);

            //var texturePath = Content.Get().GetPath("gamedata/terrain/newformat/gr.tga");
            //this.Texture = Texture2D.FromStream(device, new FileStream(texturePath, FileMode.Open));

            Effect = new BasicEffect(device);
            Effect.VertexColorEnabled = true;

            /** Convert rectangle to world units **/
            var quads = Size.Width;

            var quadWidth = WorldSpace.GetWorldFromTile((float)Size.Width / (float)quads);
            var quadHeight = WorldSpace.GetWorldFromTile((float)Size.Height / (float)quads);
            var numQuads = quads * quads;
            var numGrass = 1;//1200*quads*quads;

            var repeatX = Size.Width / 2.5f;
            var repeatY = Size.Height / 2.5f;

            var quadUVWidth = repeatX / Size.Width;
            var quadUVHeight = repeatY / Size.Height;

            VertexPositionColor[] Geom = new VertexPositionColor[numQuads * 4];
            int[] Indexes = new int[numQuads * 6];
            NumPrimitives = (numQuads * 2);

            int geomOffset = 0;
            int indexOffset = 0;

            var offsetX = WorldSpace.GetWorldFromTile(Size.X);
            var offsetY = WorldSpace.GetWorldFromTile(Size.Y);

            Color LightGreen = new Color(80, 116, 59);
            Color LightBrown = new Color(157, 117, 65);
            Color DarkGreen = new Color(8, 52, 8);
            Color DarkBrown = new Color(81, 60, 18);

            for (var y = 0; y < quads; y++)
            {
                for (var x = 0; x < quads; x++){
                    var tl = new Vector3(offsetX + (x * quadWidth), 0.0f, offsetY + (y * quadHeight));
                    var tr = new Vector3(tl.X + quadWidth, 0.0f, tl.Z);
                    var bl = new Vector3(tl.X, 0.0f, tl.Z + quadHeight);
                    var br = new Vector3(tl.X + quadWidth, 0.0f, tl.Z + quadHeight);

                    Indexes[indexOffset++] = geomOffset;
                    Indexes[indexOffset++] = (geomOffset + 1);
                    Indexes[indexOffset++] = (geomOffset+2);

                    Indexes[indexOffset++] = (geomOffset + 2);
                    Indexes[indexOffset++] = (geomOffset + 3);
                    Indexes[indexOffset++] = geomOffset;

                    Color tlCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * quads + x]);
                    Color trCol = Color.Lerp(LightGreen, LightBrown, GrassState[y * quads + ((x + 1) % quads)]);
                    Color blCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % quads) * quads + x]);
                    Color brCol = Color.Lerp(LightGreen, LightBrown, GrassState[((y + 1) % quads) * quads + ((x + 1) % quads)]);

                    Geom[geomOffset++] = new VertexPositionColor(tl, tlCol);
                    Geom[geomOffset++] = new VertexPositionColor(tr, trCol);
                    Geom[geomOffset++] = new VertexPositionColor(br, brCol);
                    Geom[geomOffset++] = new VertexPositionColor(bl, blCol);
                }
            }

            var rand = new Random();


            VertexPositionColor[] GrassGeom = new VertexPositionColor[numGrass*2];
            GrassPrimitives = numGrass;

            geomOffset = 0;
            indexOffset = 0;

            for (var i = 0; i < numGrass; i++)
            { //generate a lot of grass blades!
                float xPos = (float)rand.NextDouble() * quads;
                float yPos = (float)rand.NextDouble() * quads;
                int x = (int)xPos%quads;
                int y = (int)yPos%quads;

                float bladeCol = (float)rand.NextDouble() * 0.4f + 0.1f;

                Color green = Color.Lerp(LightGreen, DarkGreen, bladeCol);
                Color brown = Color.Lerp(LightBrown, DarkBrown, bladeCol);

                Color FinalCol = Color.Lerp(green, brown, GrassState[y * quads + x]);
                /**
                 * Insane interpolation!! do not use if you value your cpu's life
                 * 
                Color trCol = Color.Lerp(green, brown, GrassState[y * quads + ((x + 1) % quads)]);
                Color blCol = Color.Lerp(green, brown, GrassState[((y + 1) % quads) * quads + x]);
                Color brCol = Color.Lerp(green, brown, GrassState[((y + 1) % quads) * quads + ((x + 1) % quads)]);

                Color FinalCol = Color.Lerp(Color.Lerp(tlCol, trCol, xPos % 1.0f), Color.Lerp(blCol, brCol, xPos % 1.0f), yPos % 1.0f);
                 **/
                float bladeHeight = 0.2f*(float)rand.NextDouble();

                GrassGeom[geomOffset++] = new VertexPositionColor(new Vector3(xPos * quadWidth + offsetX, 0.0f, yPos * quadHeight + offsetY), FinalCol);
                GrassGeom[geomOffset++] = new VertexPositionColor(new Vector3(xPos * quadWidth + offsetX, bladeHeight, yPos * quadHeight + offsetY), FinalCol);
            }

            VertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), Geom.Length, BufferUsage.None);
            VertexBuffer.SetData(Geom);

            IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, sizeof(int) * Indexes.Length, BufferUsage.None);
            IndexBuffer.SetData(Indexes);
            GeomLength = Geom.Length;

            GrassVertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), GrassGeom.Length, BufferUsage.None);
            GrassVertexBuffer.SetData(GrassGeom);
        }

        public void GenerateGrassStates() //generates a set of grass states for a lot.
        {
            //right now only works for square lots, but that's all tso has!
            var random = new Random();
            int width = Size.Width;
            float[] result = new float[width * width];
            int initial = width/4; //divide by more for less noisyness!
            float factor = 0.5f/((int)Math.Log(initial, 2));
            while (initial > 0)
            {
                var squared = initial * initial;
                var noise = new float[squared];
                for (int i = 0; i < squared; i++) noise[i] = (float)random.NextDouble()*factor;

                int offset = 0;
                for (int x = 0; x < width; x++)
                {
                    double xInt = (x / (double)(width-1)) * (initial-1);
                    for (int y = 0; y < width; y++)
                    {
                        double yInt = (y / (double)(width - 1)) * (initial - 1);
                        float tl = noise[(int)(Math.Floor(yInt)*initial+Math.Floor(xInt))];
                        float tr = noise[(int)(Math.Floor(yInt) * initial + Math.Ceiling(xInt))];
                        float bl = noise[(int)(Math.Ceiling(yInt) * initial + Math.Floor(xInt))];
                        float br = noise[(int)(Math.Ceiling(yInt) * initial + Math.Ceiling(xInt))];
                        float p = (float)(xInt%1.0);
                        float q = (float)(yInt%1.0);
                        result[offset++] += (tl * (1 - p) + tr * (p)) * (1 - q) + (bl * (1 - p) + br * (p)) * q; //don't you love 2 dimensional linear interpolation?? ;)
                    }
                }
                initial /= 2;
            }
            GrassState = result;
        }

        /// <summary>
        /// Render the terrain
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Draw(GraphicsDevice device, WorldState world){
            world._3D.ApplyCamera(Effect, this);
            //device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            //device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

            device.SetVertexBuffer(VertexBuffer);
            device.Indices = IndexBuffer;

            //device.RasterizerState.CullMode = CullMode.None;
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, GeomLength, 0, NumPrimitives);
            }

            device.SetVertexBuffer(GrassVertexBuffer);
            //device.Indices = GrassIndexBuffer;

            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawPrimitives(PrimitiveType.LineList, 0, GrassPrimitives);
                //device.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, GrassPrimitives*2, 0, GrassPrimitives);
            };

        }
    }
}
