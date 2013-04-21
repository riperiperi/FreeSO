using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework;

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
