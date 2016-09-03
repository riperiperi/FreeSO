using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Debug.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIDebugMenu : UIDialog
    {
        private UIImage Background;
        private UIButton ContentBrowserBtn;

        public UIDebugMenu() : base(UIDialogStyle.Tall, true)
        {
            SetSize(500, 300);
            Caption = "Debug Tools";

            Position = new Microsoft.Xna.Framework.Vector2(
                (GlobalSettings.Default.GraphicsWidth / 2.0f) - 250,
                (GlobalSettings.Default.GraphicsHeight / 2.0f) - 150
            );

            Add(new UIImage()
            {
                Texture = GetTexture(0x00000Cbfb00000001),
                Position = new Microsoft.Xna.Framework.Vector2(40, 95)
            });

            ContentBrowserBtn = new UIButton();
            ContentBrowserBtn.Caption = "Browse Content";
            ContentBrowserBtn.Position = new Microsoft.Xna.Framework.Vector2(160, 50);
            ContentBrowserBtn.Width = 300;
            ContentBrowserBtn.OnButtonClick += x =>
            {
                //ShowTool(new ContentBrowser());
            };
            Add(ContentBrowserBtn);
        }
    }
}
