using FSO.Client.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using FSO.SimAntics;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using FSO.IDE.EditorComponent;
using FSO.Client;
using FSO.Content;
using System.Windows.Forms;
using System.Threading;

namespace FSO.IDE
{
    public class IDETester : IDEInjector
    {
        public Dictionary<VMEntity, BHAVEditor> EntToDebugger = new Dictionary<VMEntity, BHAVEditor>();

        public void StartIDE(VM vm)
        {
            EditorResource.Get().Init(GameFacade.GraphicsDevice);
            EditorScope.Behaviour = new Files.Formats.IFF.IffFile(Content.Content.Get().GetPath("objectdata/globals/behavior.iff"));
            EditorScope.Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");

            var t = new Thread(() =>
            {
                var editor = new MainWindow();
                editor.Test(vm);
                Application.Run(editor);
            });

            //t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        public void IDEOpenBHAV(BHAV targetBhav, GameObject targetObj)
        {
            new Thread(() =>
            {
                if (MainWindow.Instance == null) return;
                MainWindow.Instance.Invoke(new MainWindowDelegate(() =>
                {
                    MainWindow.Instance.BHAVManager.OpenEditor(targetBhav, targetObj);
                }), null);
            }).Start();
        }

        public void IDEBreakpointHit(VM vm, VMEntity targetEnt)
        {
            new Thread(() =>
            {
                if (MainWindow.Instance == null) return;
                MainWindow.Instance.Invoke(new MainWindowDelegate(() =>
                {
                    MainWindow.Instance.BHAVManager.OpenTracer(vm, targetEnt);
                }), null);
            }).Start();
        }

        private delegate void MainWindowDelegate();
    }
}
