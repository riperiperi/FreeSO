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
using FSO.IDE.Common.Debug;
using FSO.Files.RC;

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
                lock (FSOUI)
                {
                    Renderer.SetGUID(GUID);
                }
            }
        }

        public void ChangeWorld(int rotation, int zoom)
        {
            lock (FSOUI)
            {
                Renderer.ChangeWorld(rotation, zoom);
            }
        }

        public void ChangeGraphic(int gfx)
        {
            lock (FSOUI)
            {
                Renderer.ChangeGraphic(gfx);
            }
        }

        public void ForceUpdate()
        {
            lock (FSOUI)
            {
                Renderer.ForceUpdate();
            }
        }

        public void SetDynamic(int i)
        {
            lock (FSOUI)
            {
                Renderer.SetDynamic(i);
            }
        }
    }
}
