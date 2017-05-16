using FSO.Common.Rendering.Framework.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Client.Utils
{
    public class WinFormsClipboard : ClipboardHandler
    {
        public override string Get()
        {
            var wait = new AutoResetEvent(false);
            string clipboardText = "";
            var clipThread = new Thread(x =>
            {
                clipboardText = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.Text);
                wait.Set();
            });
            clipThread.SetApartmentState(ApartmentState.STA);
            clipThread.Start();
            wait.WaitOne();
            return clipboardText;
        }

        public override void Set(string str)
        {
            var copyThread = new Thread(x =>
            {
                System.Windows.Forms.Clipboard.SetText((String.IsNullOrEmpty(str)) ? " " : str);
            });
            copyThread.SetApartmentState(ApartmentState.STA);
            copyThread.Start();
        }
    }
}
