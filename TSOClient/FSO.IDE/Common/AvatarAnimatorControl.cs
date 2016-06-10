using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FSO.Client.UI.Framework;
using FSO.Client;

namespace FSO.IDE.Common
{
    public partial class AvatarAnimatorControl : FSOUIControl
    {
        private UIAvatarAnimator Renderer;
        public void ShowAnim(string anim)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                Renderer = new UIAvatarAnimator();
                mainCont.Add(Renderer);
                GameFacade.Screens.AddExternal(mainCont);

                SetUI(mainCont);
            }
            Renderer.SetAnimation(anim);
        }
    }
}
