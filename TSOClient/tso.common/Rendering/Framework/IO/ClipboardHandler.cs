using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Rendering.Framework.IO
{
    public class ClipboardHandler
    {
        public static ClipboardHandler Default = new ClipboardHandler();

        public virtual string Get() { return ""; }
        public virtual void Set(string text) { }
    }
}
