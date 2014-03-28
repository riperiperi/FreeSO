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
using TSOClient.Code.UI.Controls;
using System.Timers;
using TSO.HIT;

namespace TSOClient.Code.UI.Screens
{
    public class LoadingScreen : GameScreen
    {
        private UIContainer BackgroundCtnr;
        private UIImage Background;
        private UILabel ProgressLabel1;
        private UILabel ProgressLabel2;


        private Timer CheckProgressTimer;

        public LoadingScreen()
        {
            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            BackgroundCtnr.Add(Background);

            //TODO: Letter spacing is a bit wrong on this label
            var lbl = new UILabel();
            lbl.Caption = GameFacade.Strings.GetString("154", "5");
            lbl.X = 0;
            lbl.Size = new Microsoft.Xna.Framework.Vector2(800, 100);
            lbl.Y = 508;

            var style = lbl.CaptionStyle.Clone();
            style.Size = 17;
            lbl.CaptionStyle = style;
            BackgroundCtnr.Add(lbl);
            this.Add(BackgroundCtnr);


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


            BackgroundCtnr.Add(ProgressLabel1);
            BackgroundCtnr.Add(ProgressLabel2);


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

            PlayBackgroundMusic(new string[]{GameFacade.GameFilePath("music\\stations\\latin\\latin3_7df26b84.mp3")});

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
            /** Have we preloaded the correct percent? **/
            var percentDone = ContentManager.PreloadProgress;
            var percentUntilNextLabel = ((float)(CurrentPreloadLabel + 1)) / ((float)PreloadLabels.Length);

            //System.Diagnostics.Debug.WriteLine(percentDone);

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
                        //init audio now
                        HITVM.Init();
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

            var tween = GameFacade.Screens.Tween.To(ProgressLabel1, 1.0f, new Dictionary<string, float>() {
                {"X", -800.0f}
            });
            tween.OnComplete += new TweenEvent(tween_OnComplete);

            GameFacade.Screens.Tween.To(ProgressLabel2, 1.0f, new Dictionary<string, float>() {
                {"X", 0.0f}
            });
        }


        void tween_OnComplete(UITweenInstance tween, float progress)
        {
            InTween = false;
            CheckPreloadLabel();
        }

    }
}
