using FSO.Client.UI.Framework.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.EODs.Archetypes
{
    public abstract class UIBasicEOD : UIEOD
    {
        private string UIScriptPath;

        protected string EODName;
        protected UIScript Script;

        public UIBasicEOD(UIEODController controller, string name, string uiScript) : base(controller)
        {
            EODName = name;
            UIScriptPath = uiScript;

            InitUI();
            InitEOD();
        }

        protected virtual void InitUI()
        {
            Script = RenderScript(UIScriptPath);
        }

        protected virtual void InitEOD()
        {
            PlaintextHandlers[EODName + "_show"] = Show;
        }

        protected abstract EODLiveModeOpt GetEODOptions();

        protected virtual void Show(string evt, string txt)
        {
            EODController.ShowEODMode(GetEODOptions());
        }

        public override void OnClose()
        {
            Send("close", "");
            base.OnClose();
        }
    }
}
