using FSO.Client.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class CoreGameScreenController : IDisposable
    {
        private CoreGameScreen Screen;

        public CoreGameScreenController(CoreGameScreen screen)
        {
            this.Screen = screen;
        }

        public void Dispose()
        {

        }
    }
}
