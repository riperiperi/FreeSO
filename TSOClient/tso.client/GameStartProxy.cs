using FSO.LotView;
using FSO.UI;
using System;

namespace FSO.Client
{
    /// <summary>
    /// To avoid dynamically linking monogame from Program.cs (where we have to choose the correct version for the OS),
    /// we use this mediator class.
    /// </summary>
    public class GameStartProxy : IGameStartProxy
    {
        public static Action<Func<bool>, IntPtr> BindClosingHandler;

        public void Start(bool useDX)
        {
            GameFacade.DirectX = useDX;
			World.DirectX = useDX;
            TSOGame game = new TSOGame();

            BindClosingHandler?.Invoke(HandleClosing, game.Window.Handle);

            game.Run();
            game.Dispose();
        }

        public bool HandleClosing()
        {
            return FSOFacade.Controller?.CloseAttempt() ?? true;
        }

		public void SetPath(string path)
		{
			GlobalSettings.Default.StartupPath = path;
            GlobalSettings.Default.Windowed = false;
		}


	}
}
