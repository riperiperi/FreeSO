using FSO.Client;
using FSO.Client.UI.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.IDE.Common
{
    public class ObjThumbnailControl : FSOUIControl {
        private UIThumbnailRenderer Renderer;

        public void ShowObject(uint GUID)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                Renderer = new UIThumbnailRenderer(GUID);
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
