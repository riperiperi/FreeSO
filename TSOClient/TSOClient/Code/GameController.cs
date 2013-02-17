using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Screens;

namespace TSOClient.Code
{
    /// <summary>
    /// Handles the game flow between various game modes, e.g. login => city view
    /// </summary>
    public class GameController
    {
        /// <summary>
        /// Start the preloading process
        /// </summary>
        public void StartLoading()
        {
            var screen = new LoginScreen();
            GameFacade.Screens.AddScreen(screen);

            ContentManager.InitLoading();
        }

    }
}
