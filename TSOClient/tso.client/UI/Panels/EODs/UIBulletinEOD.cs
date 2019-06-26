using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIBulletinEOD : UIEOD
    {
        public int Mode; //read/read-specific/write
        public UIBulletinDialog Dialog;
        public UILabel SeeDialog;

        public UIBulletinEOD(UIEODController controller) : base(controller)
        {
            PlaintextHandlers["bulletin_show"] = P_Show;
        }

        public void P_Show(string evt, string text)
        {
            int mode;
            int.TryParse(text, out mode);
            if (mode < 1 || mode > 2) mode = 1;
            var split = text.Split('\n');
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Normal,
                Length = EODLength.None,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            });

            var style = TextStyle.DefaultLabel.Clone();
            style.Shadow = true;
            Add(SeeDialog = new UILabel() {
                Position = new Microsoft.Xna.Framework.Vector2(0, 32),
                Size = new Microsoft.Xna.Framework.Vector2(472, 112),
                Alignment = TextAlignment.Center | TextAlignment.Middle,
                CaptionStyle = style,
                Caption = GameFacade.Strings.GetString("f120", "36")
            });

            if (UIBulletinDialog.Present)
            {
                GameThread.NextUpdate(x => OnClose());
            }
            else
            {
                Dialog = new UIBulletinDialog(LotController?.vm?.TSOState?.NhoodID ?? 0);
                Dialog.OnModeChange += (dmode) =>
                {
                    Send("bulletin_mode", dmode.ToString());
                };
                Dialog.CloseButton.OnButtonClick += (btn) => OnClose();
                if (mode == 2)
                {
                    Dialog.SelectedPost(null);
                } else
                {
                    Send("bulletin_mode", "0");
                }
                UIScreen.GlobalShowDialog(Dialog, false);
                //Send("bulletin_mode", ((mode == 1) ? 0 : 2).ToString());
            }
            
        }

        public override void OnClose()
        {
            Send("close", "");
            if (Dialog != null) UIScreen.RemoveDialog(Dialog);
            Dialog = null;
            base.OnClose();
        }
    }
}
