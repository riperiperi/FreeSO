/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Screens;
using TSOClient.Network;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Code
{
    /// <summary>
    /// Handles the game flow between various game modes, e.g. login => city view
    /// </summary>
    public class GameController
    {
        public void DebugShowTypeFaceScreen()
        {
            var screen = new DebugTypeFaceScreen();

            /** Remove preload screen **/
            GameFacade.Screens.AddScreen(screen);
        }

        /// <summary>
        /// Start the preloading process
        /// </summary>
        public void StartLoading()
        {
            var screen = new MaxisLogo();

            GameFacade.Screens.AddScreen(screen);
            ContentManager.InitLoading();
        }

        /// <summary>
        /// Show the login screen
        /// </summary>
        public void ShowLogin()
        {
            var screen = new LoginScreen();

            /** Remove preload screen **/
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        /// <summary>
        /// Go to the person selection page
        /// </summary>
        public void ShowPersonSelection()
        {
            var screen = new PersonSelection();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        public void ShowPersonCreation(CityInfo selectedCity)
        {
            var screen = new PersonSelectionEdit();
            screen.SelectedCity = selectedCity;
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        public void ShowCityTransition(CityInfo selectedCity, bool CharacterCreated)
        {
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(new CityTransitionScreen(selectedCity, CharacterCreated));
        }

        public void ShowCity()
        {
            var screen = new CoreGameScreen();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        public void ShowCredits()
        {
            var screen = new Credits();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
        }

        public void ShowLotDebug()
        {
            var screen = new CoreGameScreen(); //new LotDebugScreen();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
            //screen.InitTestLot();
            //screen.ZoomLevel = 1;
        }

        public void StartDebugTools()
        {
            if (GameFacade.DebugWindow != null)
            {
                if (GameFacade.DebugWindow.Visible)
                {
                    GameFacade.DebugWindow.Hide();
                }
                else
                {
                    GameFacade.DebugWindow.Show();
                }
                return;
            }

            var debugWindow = new TSOClient.Code.Debug.TSOClientTools();
            GameFacade.DebugWindow = debugWindow;

            /** Position the debug window **/

            debugWindow.Show();

            //debugWindow.PositionAroundGame(GameFacade.Game.Window);
        }
    }
}
