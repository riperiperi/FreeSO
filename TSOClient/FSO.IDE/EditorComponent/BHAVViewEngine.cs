using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class BHAVViewEngine
    {

        public GraphicsDevice GD;
        public SpriteBatch Batch;
        public ContentManager Content;
        public BHAVTree TestTree;

        public SpriteFont FontTest;
        public BHAVViewEngine(IGraphicsDeviceService graphics, IServiceProvider services)
        {
            Content = new ContentManager(services, "Content/");
            GD = graphics.GraphicsDevice;
            Batch = new SpriteBatch(GD);
        }

        public void Initialize()
        {
            FontTest = Content.Load<SpriteFont>("Fonts/ProjectDollhouse_12px");

            var aquarium = FSO.Content.Content.Get().WorldObjects.Get(0x98E0F8BD);
            var watchExcited = aquarium.Resource.Get<BHAV>(4118);
            TestTree = new BHAVTree(watchExcited);
            TestTree.InitResource(GD);
        }

        public void Update(GameTime gameTime)
        {

        }

        public void Draw(GameTime gameTime)
        {
            GD.Clear(Color.White);
            Batch.Begin();
            TestTree.Draw(Batch, FontTest);
            Batch.End();
        }
    }
}
