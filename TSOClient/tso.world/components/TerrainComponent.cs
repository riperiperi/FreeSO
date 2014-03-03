using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content;
using Microsoft.Xna.Framework;

namespace tso.world.components
{
    public class TerrainComponent : WorldComponent
    {
        private Texture2D Texture;
        private Rectangle Size;

        private VertexPositionTexture[] Geom;
        private short[] Indexes;
        private short NumPrimitives;
        private IndexBuffer IndexBuffer;
        private VertexBuffer VertexBuffer;

        private BasicEffect Effect;
        public bool DrawGrid = false;

        public TerrainComponent(Rectangle size){
            this.Size = size;
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

            var texturePath = Content.Get().GetPath("gamedata/terrain/newformat/gr.tga");
            this.Texture = Texture2D.FromFile(device, texturePath);

            Effect = new BasicEffect(device, null);
            Effect.TextureEnabled = true;
            Effect.Texture = Texture;

            /** Convert rectangle to world units **/
            var quads = Size.Width;

            var quadWidth = WorldSpace.GetWorldFromTile((float)Size.Width / (float)quads);
            var quadHeight = WorldSpace.GetWorldFromTile((float)Size.Height / (float)quads);
            var numQuads = quads * quads;

            var repeatX = Size.Width / 2.5f;
            var repeatY = Size.Height / 2.5f;

            if (DrawGrid)
            {
                repeatX = ((Size.Width * quadWidth) / quadWidth);
                repeatY = ((Size.Height * quadHeight) / quadHeight);
                Effect.Texture = WorldContent.GridTexture;
            }

            var quadUVWidth = repeatX / Size.Width;
            var quadUVHeight = repeatY / Size.Height;

            Geom = new VertexPositionTexture[numQuads * 4];
            Indexes = new short[numQuads * 6];
            NumPrimitives = (short)(numQuads * 2);

            short geomOffset = 0;
            short indexOffset = 0;

            var offsetX = WorldSpace.GetWorldFromTile(Size.X);
            var offsetY = WorldSpace.GetWorldFromTile(Size.Y);

            for (var y = 0; y < quads; y++)
            {
                for (var x = 0; x < quads; x++){
                    var tl = new Vector3(offsetX + (x * quadWidth), 0.0f, offsetY + (y * quadHeight));
                    var tr = new Vector3(tl.X + quadWidth, 0.0f, tl.Z);
                    var bl = new Vector3(tl.X, 0.0f, tl.Z + quadHeight);
                    var br = new Vector3(tl.X + quadWidth, 0.0f, tl.Z + quadHeight);

                    var texTL = new Vector2(quadUVWidth * x, quadUVHeight * y);
                    var texTR = new Vector2(texTL.X + quadUVWidth, texTL.Y);
                    var texBR = new Vector2(texTL.X + quadUVWidth, texTL.Y + quadUVHeight);
                    var texBL = new Vector2(texTL.X, texTL.Y + quadUVHeight);

                    Indexes[indexOffset++] = geomOffset;
                    Indexes[indexOffset++] = (short)(geomOffset + 1);
                    Indexes[indexOffset++] = (short)(geomOffset+2);

                    Indexes[indexOffset++] = (short)(geomOffset + 2);
                    Indexes[indexOffset++] = (short)(geomOffset + 3);
                    Indexes[indexOffset++] = geomOffset;

                    Geom[geomOffset++] = new VertexPositionTexture(tl, texTL);
                    Geom[geomOffset++] = new VertexPositionTexture(tr, texTR);
                    Geom[geomOffset++] = new VertexPositionTexture(br, texBR);
                    Geom[geomOffset++] = new VertexPositionTexture(bl, texBL);
                }
            }

            VertexBuffer = new VertexBuffer(device, VertexPositionTexture.SizeInBytes * Geom.Length, BufferUsage.None);
            VertexBuffer.SetData(Geom);

            IndexBuffer = new IndexBuffer(device, sizeof(short) * Indexes.Length, BufferUsage.None, IndexElementSize.SixteenBits);
            IndexBuffer.SetData(Indexes);


            //Geom = new VertexPositionTexture[4];

            //var tl = WorldSpace.GetWorldFromTile(new Vector2(Size.X, Size.Y));
            //var tr = WorldSpace.GetWorldFromTile(new Vector2(Size.Width, Size.Y));
            //var bl = WorldSpace.GetWorldFromTile(new Vector2(Size.X, Size.Height));
            //var br = WorldSpace.GetWorldFromTile(new Vector2(Size.Width, Size.Height));

            //Geom[0] = new VertexPositionTexture(tl, new Vector2(0, 0));
            //Geom[1] = new VertexPositionTexture(tr, new Vector2(repeatX, 0));
            //Geom[2] = new VertexPositionTexture(br, new Vector2(repeatX, repeatY));
            //Geom[3] = new VertexPositionTexture(bl, new Vector2(0, repeatY));
        }

        /// <summary>
        /// Render the terrain
        /// </summary>
        /// <param name="device"></param>
        /// <param name="world"></param>
        public override void Draw(GraphicsDevice device, WorldState world){
            world._3D.ApplyCamera(Effect, this);
            device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
            device.VertexDeclaration = new VertexDeclaration(device, VertexPositionTexture.VertexElements);

            device.Vertices[0].SetSource(VertexBuffer, 0, VertexPositionTexture.SizeInBytes);
            device.Indices = IndexBuffer;

            device.RenderState.CullMode = CullMode.None;
            Effect.Begin();
            foreach (var pass in Effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, Geom.Length, 0, NumPrimitives);
                //device.DrawUserPrimitives<VertexPositionTexture>(PrimitiveType.TriangleFan, Geom, 0, 2);
                pass.End();
            }
            Effect.End();

        }
    }
}
