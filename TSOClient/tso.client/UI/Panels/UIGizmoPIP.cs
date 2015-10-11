using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIGizmoPIP : UIContainer
    {
        public UISim SimBox { get; internal set; }
        private UIButton Button;

        public UIGizmoPIP()
        {
            Button = new UIButton();
            Add(Button);

            Button.OnButtonClick += Button_OnButtonClick;
        }

        private void Button_OnButtonClick(UIElement button)
        {
            //Show my sim page
            ((CoreGameScreenController)Parent.Parent.Controller).ShowMyPersonPage();
        }

        public void Initialize()
        {
            var buttonTexture = Button.Texture;

            SimBox = new UISim();
            SimBox.Position = new Microsoft.Xna.Framework.Vector2((buttonTexture.Width / 4) / 2.0f, buttonTexture.Height - 20.0f);
            SimBox.Avatar.BodyOutfitId = 2611340115981;
            SimBox.Avatar.HeadOutfitId = 5076651343885;
            SimBox.Avatar.Scale = new Microsoft.Xna.Framework.Vector3(0.37f);
            SimBox.Size = new Microsoft.Xna.Framework.Vector2(Button.Texture.Width / 4, Button.Texture.Height);
            SimBox.AutoRotate = true;
            this.Add(SimBox);

        }

        [UIAttribute("buttonImage")]
        public Texture2D ButtonImage
        {
            set
            {
                Button.Texture = value;
            }
        }

        [UIAttribute("maskImage")]
        public Texture2D MaskImage
        {
            set
            {
            }
        }
    }
}
