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
        public void InjectIDEInto(UIScreen screen, VM vm, BHAV targetBhav, GameObject targetObj)
        {
            EditorResource.Get().Init(GameFacade.GraphicsDevice);
            EditorScope.Behaviour = new Files.Formats.IFF.IffFile(Content.Content.Get().GetPath("objectdata/globals/behavior.iff"));
            EditorScope.Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");

            new Thread(() =>
            {
                var editor = new BHAVEditor(targetBhav, new EditorScope(targetObj, targetBhav));
                Application.Run(editor);
            }).Start();
        }
    }
}
