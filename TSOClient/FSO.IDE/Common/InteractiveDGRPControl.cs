using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.SimAntics;
using FSO.SimAntics.Entities;

namespace FSO.IDE.Common
{
    public partial class InteractiveDGRPControl : FSOUIControl
    {
        private UIInteractiveDGRP Renderer;

        public VM ExtVM
        {
            get
            {
                return Renderer.TempVM;
            }
        }

        public VMMultitileGroup ExtObj
        {
            get
            {
                return Renderer.TargetOBJ;
            }
        }

        public void ShowObject(uint GUID)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                mainCont.UseZ = true;
                Renderer = new UIInteractiveDGRP(GUID);
                mainCont.Add(Renderer);
                GameFacade.Screens.AddExternal(mainCont);

                SetUI(mainCont);
            }
            else
            {
                //reuse existing
                GameThread.InUpdate(() =>
                {
                    Renderer.SetGUID(GUID);
                });
            }
        }

        public void ChangeWorld(int rotation, int zoom)
        {
            GameThread.InUpdate(() =>
            {
                Renderer.ChangeWorld(rotation, zoom);
            });
        }

        public void ChangeGraphic(int gfx)
        {
            GameThread.InUpdate(() =>
            {
                Renderer.ChangeGraphic(gfx);
            });
        }

        public void ForceUpdate()
        {
            GameThread.InUpdate(() =>
            {
                Renderer.ForceUpdate();
            });
        }

        public void SetDynamic(int i)
        {
            GameThread.InUpdate(() =>
            {
                Renderer.SetDynamic(i);
            });
        }
    }
}
