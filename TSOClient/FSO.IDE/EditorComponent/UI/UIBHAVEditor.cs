using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.EditorComponent.UI
{
    public class UIBHAVEditor : UIContainer
    {
        public BHAVContainer BHAVView;

        public UIBHAVEditor(BHAV target, EditorScope scope)
        {
            BHAVView = new BHAVContainer(target, scope);
            this.Add(BHAVView);
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            var res = EditorResource.Get();
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(), new Vector2(4, batch.Height), Color.Black * 0.2f);
            DrawLocalTexture(batch, res.WhiteTex, null, new Vector2(4, 0), new Vector2(batch.Width, 4), Color.Black * 0.2f);
        }
    }
}
