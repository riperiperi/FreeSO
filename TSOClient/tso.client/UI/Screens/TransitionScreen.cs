/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using FSO.Client.GameContent;
using FSO.Server.Protocol.CitySelector;
using FSO.Client.Controllers;
using FSO.Client.Regulators;

namespace FSO.Client.UI.Screens
{
    public class TransitionScreen : GameScreen
    {
        private UISetupBackground m_Background;
        private UILoginProgress m_LoginProgress;

        /// <summary>
        /// Creates a new CityTransitionScreen.
        /// </summary>
        /// <param name="SelectedCity">The city being transitioned to.</param>
        /// <param name="CharacterCreated">If transitioning from CreateASim, this should be true.
        /// A CharacterCreateCity packet will be sent to the CityServer. Otherwise, this should be false.
        /// A CityToken packet will be sent to the CityServer.</param>
        public TransitionScreen()
        {
            /** Background image **/
            GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Hourglass);
            m_Background = new UISetupBackground();

            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            m_Background.BackgroundCtnr.Add(lbl);
            this.Add(m_Background);

            m_LoginProgress = new UILoginProgress();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
            m_LoginProgress.Opacity = 0.9f;
            this.Add(m_LoginProgress);
        }

        public override void GameResized()
        {
            base.GameResized();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
        }

        public bool ShowProgress
        {
            get
            {
                return m_LoginProgress.Visible;
            }
            set
            {
                m_LoginProgress.Visible = value;
            }
        }
        
        public void SetProgress(float progress, int stringIndex)
        {
            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stringIndex).ToString());
            m_LoginProgress.Progress = progress;
        }
    }
}
