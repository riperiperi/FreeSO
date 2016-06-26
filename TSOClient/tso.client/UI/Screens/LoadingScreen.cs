/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using System.Timers;
using FSO.HIT;
using FSO.Client.GameContent;
using FSO.Client.Controllers;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Audio;
using FSO.Client.UI.Model;
using System.IO;
using FSO.Files;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Panels;

namespace FSO.Client.UI.Screens
{
    public class LoadingScreen : GameScreen
    {
        private UISetupBackground Background;
        private UILabel ProgressLabel1;
        private UILabel ProgressLabel2;

        private Timer CheckProgressTimer;

        public LoadingScreen()
        {
            HITVM.Get().PlaySoundEvent(UIMusic.LoadLoop);

            Background = new UISetupBackground();

            //TODO: Letter spacing is a bit wrong on this label
            var lbl = new UILabel();
            lbl.Caption = GameFacade.Strings.GetString("154", "5");
            lbl.X = 0;
            lbl.Size = new Microsoft.Xna.Framework.Vector2(800, 100);
            lbl.Y = 508;

            var style = lbl.CaptionStyle.Clone();
            style.Size = 17;
            lbl.CaptionStyle = style;
            Background.BackgroundCtnr.Add(lbl);
            this.Add(Background);

            ProgressLabel1 = new UILabel
            {
                X = 0,
                Y = 550,
                Size = new Microsoft.Xna.Framework.Vector2(800, 100),
                CaptionStyle = style
            };

            ProgressLabel2 = new UILabel
            {
                X = 0,
                Y = 550,
                Size = new Microsoft.Xna.Framework.Vector2(800, 100),
                CaptionStyle = style
            };

            Background.BackgroundCtnr.Add(ProgressLabel1);
            Background.BackgroundCtnr.Add(ProgressLabel2);

            PreloadLabels = new string[]{
                GameFacade.Strings.GetString("155", "6"),
                GameFacade.Strings.GetString("155", "7"),
                GameFacade.Strings.GetString("155", "8"),
                GameFacade.Strings.GetString("155", "9")
            };

            CurrentPreloadLabel = 0;
            AnimateLabel("", PreloadLabels[0]);

            CheckProgressTimer = new Timer();
            CheckProgressTimer.Interval = 5;
            CheckProgressTimer.Elapsed += new ElapsedEventHandler(CheckProgressTimer_Elapsed);
            CheckProgressTimer.Start();

            //GameFacade.Screens.Tween.To(rect, 10.0f, new Dictionary<string, float>() {
            //    {"X", 500.0f}
            //}, TweenQuad.EaseInOut);
        }

        void CheckProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckPreloadLabel();
        }

        private string[] PreloadLabels;
        private int CurrentPreloadLabel = 0;
        private bool InTween = false;

        private void CheckPreloadLabel()
        {
            if (Controller == null) { return; }

            /** Have we preloaded the correct percent? **/
            var percentDone = ((LoadingScreenController)Controller).Loader.Progress;
            var percentUntilNextLabel = ((float)(CurrentPreloadLabel + 1)) / ((float)PreloadLabels.Length);

            if (percentDone >= percentUntilNextLabel)
            {
                if (!InTween)
                {
                    if (CurrentPreloadLabel + 1 < PreloadLabels.Length)
                    {
                        CurrentPreloadLabel++;
                        AnimateLabel(PreloadLabels[CurrentPreloadLabel - 1], PreloadLabels[CurrentPreloadLabel]);
                    }
                    else
                    {
                        /** No more labels to show! Preload must be complete :) **/
                        CheckProgressTimer.Stop();

                        GameFacade.Controller.ShowLogin();
                    }
                }
            }
        }

        private void AnimateLabel(string previousLabel, string newLabel)
        {
            InTween = true;

            ProgressLabel1.X = 0;
            ProgressLabel1.Caption = previousLabel;

            ProgressLabel2.X = 800;
            ProgressLabel2.Caption = newLabel;

            var tween = GameFacade.Screens.Tween.To(ProgressLabel1, 1.0f, new Dictionary<string, float>() 
            {
                {"X", -800.0f}
            });
            tween.OnComplete += new TweenEvent(tween_OnComplete);

            GameFacade.Screens.Tween.To(ProgressLabel2, 1.0f, new Dictionary<string, float>() 
            {
                {"X", 0.0f}
            });
        }

        private void tween_OnComplete(UITweenInstance tween, float progress)
        {
            InTween = false;
            CheckPreloadLabel();
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
        }
    }
}
