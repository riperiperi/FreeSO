/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Utils;
using FSO.Common;
using FSO.Server.Protocol.Voltron.Packets;

namespace FSO.Client.UI.Framework
{
    public class UIScreen : UIContainer
    {
        public UIScreen() : base()
        {
            ScaleX = ScaleY = FSOEnvironment.DPIScaleFactor;
        }

        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {

        }

        public static UIScreen Current
        {
            get
            {
                return GameFacade.Screens.CurrentUIScreen;
            }
        }

        public static UIAlert GlobalShowAnnouncement(AnnouncementMsgPDU msg)
        {
            UIAlert alert = null;
            alert = GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("195", "30") + GameFacade.CurrentCityName,
                Message = GameFacade.Strings.GetString("195", "28") + msg.SenderID.Substring(2) + "\r\n"
                + GameFacade.Strings.GetString("195", "29") + msg.Subject + "\r\n"
                + msg.Message,
                Buttons = UIAlertButton.Ok((btn) => RemoveDialog(alert)),
                Alignment = TextAlignment.Left
            }, true);
            return alert;
        }

        public static UIAlert GlobalShowAlert(UIAlertOptions options, bool modal)
        {
            var alert = new UIAlert(options);
            GlobalShowDialog(alert, modal);
            alert.CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
            return alert;
        }

        /// <summary>
        /// Adds a popup dialog
        /// </summary>
        /// <param name="dialog"></param>
        public static void GlobalShowDialog(UIElement dialog, bool modal)
        {
            GlobalShowDialog(new DialogReference
            {
                Dialog = dialog,
                Modal = modal
            });
        }

        public static void GlobalShowDialog(DialogReference dialog)
        {
            GameFacade.Screens.AddDialog(dialog);

            if (dialog.Dialog is UIDialog)
            {
                ((UIDialog)dialog.Dialog).CenterAround(UIScreen.Current, -(int)UIScreen.Current.X * 2, -(int)UIScreen.Current.Y * 2);
            }
        }

        /// <summary>
        /// Adds a popup dialog
        /// </summary>
        /// <param name="dialog"></param>
        public static void ShowDialog(UIElement dialog, bool modal)
        {
            GameFacade.Screens.AddDialog(new DialogReference
            {
                Dialog = dialog,
                Modal = modal
            });

            if (dialog is UIDialog)
            {
                ((UIDialog)dialog).CenterAround(UIScreen.Current, -(int)UIScreen.Current.X*2, -(int)UIScreen.Current.Y * 2);
            }
        }

        public virtual void DeviceReset(GraphicsDevice Device) { }

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
