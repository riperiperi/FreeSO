using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.Commands;
using FSO.IDE.EditorComponent.UI;
using FSO.SimAntics;
using FSO.SimAntics.Engine;

namespace FSO.IDE.EditorComponent
{
    public class BHAVViewControl : FSOUIControl
    {
        public UIBHAVEditor Editor;
        public BHAVContainer Cont
        {
            get
            {
                return (Editor==null)?null:Editor.BHAVView;
            }
        }

        public BHAVViewControl() : base()
        {
            
        }

        public void InitBHAV(BHAV bhav, EditorScope scope, VMEntity debugEnt, VMStackFrame debugFrame, BHAVPrimSelect callback)
        {
            if (FSOUI == null)
            {
                var mainCont = new UIExternalContainer(1024, 768);
                Editor = new UIBHAVEditor(bhav, scope, debugEnt);
                mainCont.Add(Editor);
                GameFacade.Screens.AddExternal(mainCont);

                SetUI(mainCont);
                Editor.BHAVView.OnSelectedChanged += callback;
            } else
            {
                //reuse existing
                lock (FSOUI)
                {
                    Editor.QueueCommand(new ChangeBHAVCommand(bhav, scope, debugFrame, callback));
                }
            }
        }
    }
}
