using FSO.Client;
using FSO.Client.UI.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.IDE.EditorComponent.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.IDE.EditorComponent
{
    public class BHAVViewControl : FSOUIControl
    {
        public UIBHAVEditor Editor;
        public BHAVContainer Cont
        {
            get
            {
                return Editor.BHAVView;
            }
        }

        public BHAVViewControl() : base()
        {
            
        }

        public void InitBHAV(BHAV bhav, EditorScope scope)
        {
            var mainCont = new UIExternalContainer(1024, 768);
            Editor = new UIBHAVEditor(bhav, scope);
            mainCont.Add(Editor);
            GameFacade.Screens.AddExternal(mainCont);

            SetUI(mainCont);
        }
    }
}
