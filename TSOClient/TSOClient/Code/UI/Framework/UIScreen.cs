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
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    public class UIScreen : UIContainer
    {
        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
            if (backgroundTrack != -1)
            {
                GameFacade.SoundManager.StopMusictrack(backgroundTrack);
                backgroundTrack = -1;
            }
        }


        private int backgroundTrack = -1;
        public void PlayBackgroundMusic(string path)
        {
            backgroundTrack = GameFacade.SoundManager.PlayBackgroundMusic(
                path
            );
        }








        public static UIScreen Current
        {
            get
            {
                return GameFacade.Screens.CurrentUIScreen;
            }
        }


        public static UIAlert ShowAlert(UIAlertOptions options, bool modal)
        {
            var alert = new UIAlert(options);
            ShowDialog(alert, modal);
            alert.CenterAround(UIScreen.Current);
            return alert;
        }


        /// <summary>
        /// Adds a popup dialog
        /// </summary>
        /// <param name="dialog"></param>
        public static void ShowDialog(UIElement dialog, bool modal)
        {
            GameFacade.Screens.AddDialog(new DialogReference {
                Dialog = dialog,
                Modal = modal
            });

            if (dialog is UIDialog)
            {
                ((UIDialog)dialog).CenterAround(UIScreen.Current);
            }
        }

        public virtual void DeviceReset(GraphicsDevice Device) {}

        /// <summary>
        /// Removes a previously shown dialog
        /// </summary>
        /// <param name="dialog"></param>
        public static void RemoveDialog(UIElement dialog)
        {
            GameFacade.Screens.RemoveDialog(dialog);
        }



        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, ScreenWidth, ScreenHeight);
        }


        public int ScreenWidth
        {
            get
            {
                return GlobalSettings.Default.GraphicsWidth;
            }
        }

        public int ScreenHeight
        {
            get
            {
                return GlobalSettings.Default.GraphicsHeight;
            }
        }
    }
}
