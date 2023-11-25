using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FSO.IDE.Managers
{
    public class BHAVEditManager
    {
        public Dictionary<BHAV, BHAVEditor> Editors = new Dictionary<BHAV, BHAVEditor>();
        public Dictionary<VMEntity, BHAVEditor> Tracers = new Dictionary<VMEntity, BHAVEditor>();

        public BHAVEditor OpenEditor(BHAV bhav, GameObject srcobj)
        {
            if (bhav == null) return null;
            BHAVEditor window;
            if (Editors.ContainsKey(bhav))
            {
                window = Editors[bhav];
                var form = (Form)window;
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                window.Activate();
                return window;
            }

            window = new BHAVEditor(bhav, new EditorComponent.EditorScope(srcobj, bhav));
            window.Show();
            window.Activate();
            Editors.Add(bhav, window);
            return window;
        }

        public void RemoveEditor(BHAV bhav)
        {
            Editors.Remove(bhav);
        }

        public BHAVEditor OpenTracer(VM vm, VMEntity entity)
        {
            BHAVEditor window;
            if (Tracers.ContainsKey(entity))
            {
                window = Tracers[entity];
                window.UpdateDebugger();
                var form = (Form)window;
                if (form.WindowState == FormWindowState.Minimized) form.WindowState = FormWindowState.Normal;
                window.Activate();
                return window;
            }

            window = new BHAVEditor(vm, entity);
            window.Show();
            window.Activate();
            window.UpdateDebugger();
            Tracers.Add(entity, window);
            return window;
        }

        public void RemoveTracer(VMEntity entity)
        {
            Tracers.Remove(entity);
        }

        public void CloseAllTracers()
        {
            var tracers = Tracers.ToList();
            foreach (var tracer in tracers)
            {
                tracer.Value.Close();
            }
        }
    }
}
