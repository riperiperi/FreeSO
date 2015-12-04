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

            new Thread(() =>
            {
                var editor = new ObjectBrowser();
                editor.Test(vm);
                Application.Run(editor);
            }).Start();
        }

        public void InjectIDEInto(UIScreen screen, VM vm, BHAV targetBhav, GameObject targetObj)
        {
            EditorResource.Get().Init(GameFacade.GraphicsDevice);
            EditorScope.Behaviour = new Files.Formats.IFF.IffFile(Content.Content.Get().GetPath("objectdata/globals/behavior.iff"));
            EditorScope.Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");

            new Thread(() =>
            {
                var editor = new BHAVEditor(targetBhav, new EditorScope(targetObj, targetBhav), this);
                Application.Run(editor);
            }).Start();
        }

        public void IDEBreakpointHit(VM vm, VMEntity targetEnt)
        {
            EditorResource.Get().Init(GameFacade.GraphicsDevice);
            EditorScope.Behaviour = new Files.Formats.IFF.IffFile(Content.Content.Get().GetPath("objectdata/globals/behavior.iff"));
            EditorScope.Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");

            lock (EntToDebugger)
            {
                if (EntToDebugger.ContainsKey(targetEnt))
                {
                    var editor = EntToDebugger[targetEnt];
                    editor.UpdateDebugger();
                }
                else
                {
                    new Thread(() =>
                    {
                        var editor = new BHAVEditor(vm, targetEnt, this);
                        lock (EntToDebugger) EntToDebugger.Add(targetEnt, editor);
                        Application.Run(editor);
                    }).Start();
                }
            }
        }

        public void UnregisterDebugger(VMEntity targetEnt)
        {
            lock (EntToDebugger)
            {
                if (EntToDebugger.ContainsKey(targetEnt)) EntToDebugger.Remove(targetEnt);
            }
        }
    }
}
