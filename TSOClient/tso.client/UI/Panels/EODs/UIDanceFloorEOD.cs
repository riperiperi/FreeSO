using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIDanceFloorEOD : UIEOD
    {
        public UIImage UIBackground;

        public UIButton Dance001Button { get; set; }
        public UIButton Dance002Button { get; set; }
        public UIButton Dance003Button { get; set; }
        public UIButton Dance004Button { get; set; }
        public UIButton Dance005Button { get; set; }
        public UIButton Dance006Button { get; set; }
        public UIButton Dance007Button { get; set; }
        public UIButton Dance008Button { get; set; }
        public UIButton Dance009Button { get; set; }
        public UIButton Dance010Button { get; set; }
        public UIButton Dance011Button { get; set; }
        public UIButton Dance012Button { get; set; }
        public UIButton Dance013Button { get; set; }
        public UIButton Dance014Button { get; set; }
        public UIButton Dance015Button { get; set; }
        public UIButton Dance016Button { get; set; }
        public UIButton Dance017Button { get; set; }
        public UIButton Dance018Button { get; set; }
        public UIButton Dance019Button { get; set; }
        public UIButton Dance020Button { get; set; }
        public UIButton Dance021Button { get; set; }
        public UIButton Dance022Button { get; set; }
        public UIButton Dance023Button { get; set; }
        public UIButton Dance024Button { get; set; }

        public UIButton DanceToMusicButton { get; set; }

        public UIDanceFloorEOD(UIEODController controller) : base(controller)
        {
            var script = this.RenderScript("danceflooreod.uis");
            
            UIBackground = script.Create<UIImage>("UIBackground");
            AddAt(0, UIBackground);

            Dance001Button.OnButtonClick += (UIElement e) => { Dance(1); };
            Dance002Button.OnButtonClick += (UIElement e) => { Dance(2); };
            Dance003Button.OnButtonClick += (UIElement e) => { Dance(3); };
            Dance004Button.OnButtonClick += (UIElement e) => { Dance(4); };
            Dance005Button.OnButtonClick += (UIElement e) => { Dance(5); };
            Dance006Button.OnButtonClick += (UIElement e) => { Dance(6); };
            Dance007Button.OnButtonClick += (UIElement e) => { Dance(7); };
            Dance008Button.OnButtonClick += (UIElement e) => { Dance(8); };
            Dance009Button.OnButtonClick += (UIElement e) => { Dance(9); };
            Dance010Button.OnButtonClick += (UIElement e) => { Dance(10); };
            Dance011Button.OnButtonClick += (UIElement e) => { Dance(11); };
            Dance012Button.OnButtonClick += (UIElement e) => { Dance(12); };
            Dance013Button.OnButtonClick += (UIElement e) => { Dance(13); };
            Dance014Button.OnButtonClick += (UIElement e) => { Dance(14); };
            Dance015Button.OnButtonClick += (UIElement e) => { Dance(15); };
            Dance016Button.OnButtonClick += (UIElement e) => { Dance(16); };
            Dance017Button.OnButtonClick += (UIElement e) => { Dance(17); };
            Dance018Button.OnButtonClick += (UIElement e) => { Dance(18); };
            Dance019Button.OnButtonClick += (UIElement e) => { Dance(19); };
            Dance020Button.OnButtonClick += (UIElement e) => { Dance(20); };
            Dance021Button.OnButtonClick += (UIElement e) => { Dance(21); };
            Dance022Button.OnButtonClick += (UIElement e) => { Dance(22); };
            Dance023Button.OnButtonClick += (UIElement e) => { Dance(23); };
            Dance024Button.OnButtonClick += (UIElement e) => { Dance(24); };
            DanceToMusicButton.OnButtonClick += (UIElement e) => { Dance(0); };

            PlaintextHandlers["dance_show"] = P_Show;
        }

        public void Dance(int num)
        {
            num += 1;
            Send("press_button", num.ToString());
        }

        public void P_Show(string evt, string txt)
        {
            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.None,
                Tips = EODTextTips.None
            });
        }
        public override void OnClose()
        {
            CloseInteraction();
            base.OnClose();
        }
    }
}
