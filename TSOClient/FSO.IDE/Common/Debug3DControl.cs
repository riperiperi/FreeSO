using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using FSO.Files.RC;
using FSO.IDE.Common.Debug;

namespace FSO.IDE.Common
{
    public partial class Debug3DControl : FSOUIControl
    {
        private UI3DDGRP Renderer;
        public DGRP3DMesh Mesh
        {
            get
            {
                return Renderer.TargetComp3D.Mesh;
            }
        }

        public void ShowObject(uint GUID)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(128, 128);
                mainCont.UseZ = true;
                Renderer = new UI3DDGRP(GUID);
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
