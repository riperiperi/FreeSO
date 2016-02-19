using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.ResourceBrowser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.IDE.Managers
{
    public class IffEditManager
    {
        public Dictionary<GameIffResource, IffResWindow> ResourceWindow = new Dictionary<GameIffResource, IffResWindow>();

        public IffResWindow OpenResourceWindow(GameObject obj)
        {
            if (ResourceWindow.ContainsKey(obj.Resource))
            {
                var resWindow = ResourceWindow[obj.Resource];
                var form = (Form)resWindow;
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                resWindow.Activate();
                resWindow.SetTargetObject(obj);
                return resWindow;
            }
            //straight up spawn an object window
            var window = new ObjectWindow(obj.Resource, obj);
            window.Show();
            window.Activate();
            ResourceWindow.Add(obj.Resource, window);
            return window;
        }

        public IffResWindow OpenResourceWindow(GameIffResource res, GameObject target)
        {
            if (ResourceWindow.ContainsKey(res))
            {
                var resWindow = ResourceWindow[res];
                var form = (Form)resWindow;
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                resWindow.Activate();
                resWindow.SetTargetObject(target);
                return resWindow;
            }
            //detect if object, spawn iff res if not.
            //WARNING: if OBJD missing or present in files it should not be, bad things will happen!

            IffResWindow window;
            var objs = res.List<OBJD>();
            if (objs != null && objs.Count > 0)
            {
                window = new ObjectWindow(res, (target == null) ? Content.Content.Get().WorldObjects.Get(objs[0].GUID) : target);
            }
            else
            {
                window = new IffResourceViewer(res.MainIff.Filename, res, target);
            }

            ResourceWindow.Add(res, window);
            window.Show();
            window.Activate();
            return window;
        }

        public void CloseResourceWindow(GameIffResource res)
        {
            ResourceWindow.Remove(res);
        }
    }

    public interface IffResWindow
    {
        void SetTargetObject(GameObject obj);
        void Activate();
        void Close();
        void Show();
    }
}
