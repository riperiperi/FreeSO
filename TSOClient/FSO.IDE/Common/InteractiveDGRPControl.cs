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
    public partial class InteractiveDGRPControl : FSOUIControl
    {
        private UIInteractiveDGRP Renderer;
        public void ShowObject(uint GUID)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                Renderer = new UIInteractiveDGRP(GUID);
                mainCont.Add(Renderer);
                GameFacade.Screens.AddExternal(mainCont);

                SetUI(mainCont);
            }
            else
            {
                //reuse existing
                lock (FSOUI)
                {
                    Renderer.SetGUID(GUID);
                }
            }
        }
    }
}
