using System;
using System.Threading;

namespace FSO.IDE.Utils
{
    public static class FormsUtils
    {
        public static void StaExecute(Action action)
        {
            // ༼ つ ◕_◕ ༽つ IMPEACH STAThread ༼ つ ◕_◕ ༽つ
            var wait = new AutoResetEvent(false);
            var thread = new Thread(() => {
                action();
                wait.Set();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            wait.WaitOne();
            return;
        }
    }
}
