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
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Panels;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Rendering.City;
using Microsoft.Xna.Framework;
using TSOClient.Code.Utils;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework.io;
using TSO.Common.rendering.framework;

namespace TSOClient.Code.UI.Screens
{
    public class CoreGameScreen : TSOClient.Code.UI.Framework.GameScreen
    {
        public UIUCP ucp;
        public UIGizmo gizmo;
        public UIInbox Inbox;
        public UIMessageController MessageUI;
        public UIGameTitle Title;
        private Terrain CityRenderer;

        public CoreGameScreen()
        {
            /** City Scene **/
            ListenForMouse(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new UIMouseEvent(MouseHandler));

            CityRenderer = new Terrain(GameFacade.Game.GraphicsDevice); //The Terrain class implements the ThreeDAbstract interface so that it can be treated as a scene but manage its own drawing and updates.

            String city = "Queen Margret's";
            if (PlayerAccount.CurrentlyActiveSim != null)
                city = PlayerAccount.CurrentlyActiveSim.ResidingCity.Name;

            CityRenderer.m_GraphicsDevice = GameFacade.GraphicsDevice;

            CityRenderer.Initialize(city, new CityDataRetriever());
            CityRenderer.RegenData = true;
            
            CityRenderer.LoadContent(GameFacade.GraphicsDevice);

            /**
            * Music
            */
            var tracks = new string[]{
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsobuild1.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsobuild3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap2_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\map\\tsomap4_v1.mp3"
            };
            PlayBackgroundMusic(
                tracks
            );

            ucp = new UIUCP();
            ucp.Y = ScreenHeight - 210;
            ucp.CityRenderer = CityRenderer;
            ucp.UpdateZoomButton();
            this.Add(ucp);

            gizmo = new UIGizmo();
            gizmo.X = ScreenWidth - 500;
            gizmo.Y = ScreenHeight - 300;
            this.Add(gizmo);

            Title = new UIGameTitle();
            Title.SetTitle(city);
            this.Add(Title);

            OpenInbox();

            MessageUI = new UIMessageController();
            this.Add(MessageUI);

            MessageUI.PassMessage("Whats His Face", "you suck");
            MessageUI.PassMessage("Whats His Face", "no rly");
            MessageUI.PassMessage("Whats His Face", "jk im just testing message recieving please love me"); 

            MessageUI.PassMessage("Yer maw", "dont let whats his face get to you"); 
            MessageUI.PassMessage("Yer maw", "i will always love you");

            MessageUI.PassEmail("M.O.M.I", "Ban Notice", "You have been banned for playing too well. \r\n\r\nWe don't know why you still have access to the game, but it's probably related to you playing the game pretty well. \r\n\r\nPlease stop immediately.\r\n\r\n - M.O.M.I. (this is just a test message btw, you're not actually banned)");

            GameFacade.Scenes.Add((_3DAbstract)CityRenderer);
        }

        public void CloseInbox()
        {
            this.Remove(Inbox);
            Inbox = null;
        }

        public void OpenInbox()
        {
            if (Inbox == null)
            {
                Inbox = new UIInbox();
                this.Add(Inbox);
                Inbox.X = GlobalSettings.Default.GraphicsWidth / 2 - 332;
                Inbox.Y = GlobalSettings.Default.GraphicsHeight / 2 - 184;
            }
            //todo, on already visible move to front
        }

        private void MouseHandler(UIMouseEventType type, UpdateState state)
        {
            //todo: change handler to game engine when in simulation mode.

            CityRenderer.UIMouseEvent(type.ToString()); //all the city renderer needs are events telling it if the mouse is over it or not.
            //if the mouse is over it, the city renderer will handle the rest.
        }
    }
}
