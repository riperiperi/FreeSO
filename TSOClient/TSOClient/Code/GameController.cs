/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
            var screen = new LoadingScreen();
            
            GameFacade.Screens.AddScreen(screen);
            ContentManager.InitLoading();
        }

        /// <summary>
        /// Show the login screen
        /// </summary>
        public void ShowLogin()
        {
            /*NetworkFacade.Cities = new List<TSOClient.Network.CityInfo>()
            {
                new TSOClient.Network.CityInfo(){
                    ID = 1,
                    Map = "1",
                    Messages = new List<TSOClient.Network.CityInfoMessageOfTheDay>(),
                    Name = "Alphaville",
                    Online = true,
                    Status = TSOClient.Network.CityInfoStatus.Ok,
                    UUID = Guid.NewGuid().ToString()
                }
            };
            NetworkFacade.Avatars = new List<AvatarInfo>()
            {
                new AvatarInfo{
                    CityId = 1,
                    Description = "A basic description",
                    Name = "Max",
                    UUID = Guid.NewGuid().ToString()
                }
            };
            ShowPersonSelection();
            if (true) { return; }
            */

            //var screen = new LoginScreen();
            //ShowPersonCreation(new TSOClient.Network.CityInfo("Foo", "Foo", 1, "", 1, "127.0.0.1", 1234));
            //var screen = new CoreGameScreen();
            /** Remove preload screen **/
            //GameFacade.Screens.RemoveCurrent();
            //GameFacade.Screens.AddScreen(screen);


            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(new LotScreenNew());
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

        public void ShowCity()
        {

            var screen = new CoreGameScreen();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(screen);
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

            System.Windows.Forms.Form gameWindowForm = 
                (System.Windows.Forms.Form)System.Windows.Forms.Form.FromHandle(GameFacade.Game.Window.Handle);
            debugWindow.Show();

            debugWindow.PositionAroundGame(gameWindowForm);
        }
    }
}
