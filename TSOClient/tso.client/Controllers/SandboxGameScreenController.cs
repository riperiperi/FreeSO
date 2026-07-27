using FSO.Client.UI.Screens;

namespace FSO.Client.Controllers
{
    internal class SandboxGameScreenController : IDisposable
    {
        public SandboxGameScreen Screen;

        public SandboxGameScreenController(SandboxGameScreen view)
        {
            view.Controller = this;
            this.Screen = view;
        }

        public void Dispose()
        {
            Screen.CleanupLastWorld();
            GameFacade.Scenes.Clear();
        }
    }
}
