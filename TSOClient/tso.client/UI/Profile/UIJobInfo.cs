using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Files;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Profile
{
    public class UIJobInfo : UIAlert
    {
        private UIProgressBar ProgressBar;
        private new UIButton OKButton;

        public UIJobInfo(String jobInformation) : base(new UIAlertOptions()
        {
            Title = "Job Details",
            Width = 400,
            Height = 400,
            Message = jobInformation,
            Buttons = new UIAlertButton[]
            {
                new UIAlertButton(UIAlertButtonType.OK, (btn) => { }),
            },
            AllowBB = true
        })
        {
            OKButton = ButtonMap[UIAlertButtonType.OK];
            OKButton.OnButtonClick += (btn) => Destory();

            ProgressBar = new UIProgressBar()
            {
                X = 20,
                Y = 66,
                Value = 75
            };
            ProgressBar.SetSize(360, 27);
            ProgressBar.Caption = "Job Performance";
            this.Add(ProgressBar);
        }

        public override void Removed()
        {
            base.Removed();
            Icon.Texture?.Dispose();
        }

        public void Show()
        {
            GlobalShowDialog(this, false);
            this.CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
        }

        public void Destory()
        {
            UIScreen.RemoveDialog(this);
        }

        public static void GlobalShowDialog(UIElement dialog, bool modal)
        {
            GlobalShowDialog(new DialogReference
            {
                Dialog = dialog,
                Modal = modal
            });
        }

        public static void GlobalShowDialog(DialogReference dialog)
        {
            GameFacade.Screens.AddDialog(dialog);

            if (dialog.Dialog is UIDialog)
            {
                ((UIDialog)dialog.Dialog).CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
            }
        }
    }
}
